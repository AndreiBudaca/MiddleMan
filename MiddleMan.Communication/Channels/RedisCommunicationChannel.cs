using System.Runtime.CompilerServices;
using MiddleMan.Communication.SyncMechanisms;
using MiddleMan.Core;
using MiddleMan.Data.InMemory;
using StackExchange.Redis;

namespace MiddleMan.Communication.Channels
{
  public class RedisCommunicationChannel : ICommunicationChannel
  {
    private readonly ISubscriber subscriber;
    private readonly IDatabase database;
    private readonly AsyncResourceMonitor<Guid> eventsMonitor = new();
    private readonly IInMemoryContext context;
    private readonly string connectionString;

    public RedisCommunicationChannel(string connectionString, IInMemoryContext context)
    {
      this.connectionString = connectionString;
      this.context = context;
      var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
      {
        EndPoints = { connectionString },
        AsyncTimeout = ServerCapabilities.GlobalTimeoutSeconds * 1000,
        SyncTimeout = ServerCapabilities.GlobalTimeoutSeconds * 1000,
      });

      subscriber = redis.GetSubscriber();
      database = redis.GetDatabase();
    }

    public Task PublishAsync<T>(string topic, T message)
    {
      var serializedMessage = SerializeMessage(message);
      return subscriber.PublishAsync(RedisChannel.Literal(topic), serializedMessage);
    }

    public Task PublishAsync(string topic, byte[] message)
    {
      return subscriber.PublishAsync(RedisChannel.Literal(topic), message);
    }

    public Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived)
    {
      return subscriber.SubscribeAsync(RedisChannel.Literal(topic), async (channel, message) =>
      {
        T deserializedMessage = DeserializeMessage<T>(message) ??
         throw new Exception($"Failed to deserialize message for topic '{topic}'");
        await onMessageReceived(deserializedMessage);
      });
    }

    public Task SubscribeAsync(string topic, Func<byte[], Task> onMessageReceived)
    {
      return subscriber.SubscribeAsync(RedisChannel.Literal(topic), async (channel, message) =>
      {
        var rawBytes = AsRawBytes(message);
        await onMessageReceived(rawBytes);
      });
    }

    public async Task<Task<T?>> PeekChannelAsync<T>(string topic, CancellationToken cancellationToken = default)
    {
      var correlation = Guid.NewGuid();

      await subscriber.SubscribeAsync(RedisChannel.Literal(topic), async (channel, message) =>
      {
        var deserializedMessage = DeserializeMessage<T>(message) ??
          throw new Exception($"Failed to deserialize message for topic '{topic}'");
        await eventsMonitor.SetResourceAndNotify(async () => context.AddToHash("channelMessages", correlation.ToString(), deserializedMessage), correlation);
      });

      return eventsMonitor.WaitToGetResource(async () => context.GetFromHash<T>("channelMessages", correlation.ToString()), (message) => message != null, correlation, cancellationToken)
        .ContinueWith(async (messageTask) =>
        {
          try
          {
            return await messageTask;
          }
          finally
          {
            await subscriber.UnsubscribeAsync(RedisChannel.Literal(topic));
            context.RemoveFromHash("channelMessages", correlation.ToString());
          }
        }, cancellationToken).Unwrap();
    }

    public Task UnsubscribeAsync(string topic)
    {
      return subscriber.UnsubscribeAsync(RedisChannel.Literal(topic));
    }

    #region "STREAMS"
    public Task AddToStreamAsync(string streamKey, byte[] data)
    {
      return database.StreamAddAsync(streamKey, [
        new NameValueEntry("type", "data"),
        new NameValueEntry("data", data)
      ]);
    }

    public Task TerminateWithErrorAsync(string streamKey, string errorMessage)
    {
      return database.StreamAddAsync(streamKey, [
        new NameValueEntry("type", "error"),
        new NameValueEntry("data", errorMessage)
      ]);
    }

    public Task SignalStreamEndAsync(string streamKey)
    {
      return database.StreamAddAsync(streamKey, [
        new NameValueEntry("type", "end")
      ]);
    }

    public async IAsyncEnumerable<byte[]> ConsumeStreamAsync(string streamKey, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
      using var blockingConnection = await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions
      {
        EndPoints = { connectionString },
        AsyncTimeout = ServerCapabilities.GlobalTimeoutSeconds * 1000,
        SyncTimeout = ServerCapabilities.GlobalTimeoutSeconds * 1000,
      });

      var db = blockingConnection.GetDatabase();
      var lastId = "0-0";
      var communicationEnded = false;

      try
      {
        while (!cancellationToken.IsCancellationRequested && !communicationEnded)
        {
          var result = await db.ExecuteAsync("XREAD", "BLOCK", ServerCapabilities.GlobalTimeoutSeconds * 1000, "COUNT", ServerCapabilities.IntraServerBufferedChunks, "STREAMS", streamKey, lastId);
          if (result.IsNull) break;

          var (idsToDelete, chunks, streamEnded) = HandleStreamResult((RedisResult[])result!);

          if (idsToDelete.Count > 0)
          {
            await database.StreamDeleteAsync(streamKey, [.. idsToDelete]);
          }

          foreach (var data in chunks)
          {
            yield return data;
          }

          if (streamEnded)
          {
            communicationEnded = true;
          }
          else if (idsToDelete.Count > 0)
          {
            lastId = ((string)idsToDelete.Last())!;
          }
        }
      }
      finally
      {
        await database.KeyDeleteAsync(streamKey);
      }
    }

    public async Task DeleteKeyAsync(string key)
    {
      await database.KeyDeleteAsync(key);
    }
    #endregion

    private static RedisValue SerializeMessage<T>(T message)
    {
      return System.Text.Json.JsonSerializer.Serialize(message);
    }

    private static T? DeserializeMessage<T>(RedisValue message)
    {
      try
      {
        return System.Text.Json.JsonSerializer.Deserialize<T>(message!);
      }
      catch
      {
        return default;
      }
    }

    private static byte[] AsRawBytes(RedisValue redisValue)
    {
      if (redisValue.IsNull) return [];
      return (byte[])redisValue!;
    }

    private static (List<RedisValue> toBeDeleted, IEnumerable<byte[]> chunks, bool streamEnded) HandleStreamResult(RedisResult[]? streamsArray)
    {
      var toBeDeleted = new List<RedisValue>();
      var chunks = new List<byte[]>();
      var streamEnded = false;

      foreach (var streamResult in streamsArray!)
      {
        var streamData = (RedisResult[])streamResult!;
        var messages = (RedisResult[])streamData![1]!;

        foreach (var message in messages!)
        {
          var messageParts = (RedisResult[])message!;
          var messageId = (string)messageParts![0]!;
          var fields = (RedisResult[])messageParts[1]!;
          // fields layout: ["type", <typeValue>, "data", <dataValue>] for data entries
          // fields layout: ["type", "error", "data", <errorMessage>] for error entries
          //                 ["type", "end"] for sentinel entries
          var type = (string)fields![1]!;

          toBeDeleted.Add(messageId);

          if (type == "end")
          {
            streamEnded = true;
          }
          else if (type == "error")
          {
            throw new Exception((string)fields[3]!);
          }
          else
          {
            chunks.Add((byte[])fields[3]!);
          }
        }
      }

      return (toBeDeleted, chunks, streamEnded);
    }
  }
}