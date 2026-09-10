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

    #region "PUB/SUB"
    public Task PublishAsync<T>(string subject, T message, CancellationToken cancellationToken)
    {
      var serializedMessage = SerializeMessage(message);
      return subscriber.PublishAsync(RedisChannel.Literal(subject), serializedMessage);
    }

    public Task SubscribeAsync<T>(string topic, Func<T, Task> onMessageReceived, CancellationToken cancellationToken = default)
    {
      return subscriber.SubscribeAsync(RedisChannel.Literal(topic), async (channel, message) =>
      {
        T deserializedMessage = DeserializeMessage<T>(message) ??
         throw new Exception($"Failed to deserialize message for topic '{topic}'");
        await onMessageReceived(deserializedMessage);
      });
    }

    public Task<TReply?> RequestAsync<TRequest, TReply>(string subject, TRequest request, CancellationToken cancellationToken = default)
    {
      throw new NotImplementedException();
    }

    public Task SubscribeRequestAsync<TRequest, TReply>(string subject, Func<TRequest, Task<TReply?>> handler, CancellationToken cancellationToken = default)
    {
      throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
      return ValueTask.CompletedTask;
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
    #endregion

    #region "STREAMS"
    public Task CreateStreamAsync(string streamKey)
    {
      return database.ExecuteAsync(
        "XADD", streamKey, "MAXLEN", "=", "0", "*", "init", "1");
    }

    public Task AddToStreamAsync(string streamKey, DataChunk data)
    {
      return AddIfStreamExists(streamKey, "type", "data", "data", (RedisValue)data.Data.AsMemory(0, data.Length));
    }

    public Task TerminateWithErrorAsync(string streamKey, string errorMessage)
    {
      return AddIfStreamExists(streamKey, "type", "error", "data", errorMessage);
    }

    public Task SignalStreamEndAsync(string streamKey)
    {
      return AddIfStreamExists(streamKey, "type", "end");
    }

    public async IAsyncEnumerable<DataChunk> ConsumeStreamAsync(string streamKey, [EnumeratorCancellation] CancellationToken cancellationToken)
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
            yield return new DataChunk(data, data.Length);
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

    public async Task DeleteStreamAsync(string streamKey)
    {
      await database.KeyDeleteAsync(streamKey);
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
        return System.Text.Json.JsonSerializer.Deserialize<T>((string)message!);
      }
      catch
      {
        return default;
      }
    }

    private static (List<RedisValue> toBeDeleted, IEnumerable<byte[]> chunks, bool streamEnded) HandleStreamResult(RedisResult[]? streamsArray)
    {
      var toBeDeleted = new List<RedisValue>();
      var chunks = new List<byte[]>();
      var streamEnded = false;

      if (streamsArray == null) return (toBeDeleted, chunks, streamEnded);

      foreach (var streamResult in streamsArray)
      {
        if (streamResult.IsNull) continue;
        var streamData = (RedisResult[])streamResult!;
        if (streamData == null || streamData.Length < 2) continue;
        var messages = (RedisResult[])streamData[1]!;
        if (messages == null) continue;

        foreach (var message in messages)
        {
          if (message.IsNull) continue;
          var messageParts = (RedisResult[])message!;
          if (messageParts == null || messageParts.Length < 2) continue;
          var messageId = (string)messageParts[0]!;
          var fields = (RedisResult[])messageParts[1]!;
          if (fields == null || fields.Length < 2) continue;
          // fields layout: ["type", <typeValue>, "data", <dataValue>] for data entries
          // fields layout: ["type", "error", "data", <errorMessage>] for error entries
          //                 ["type", "end"] for sentinel entries
          //                 ["init", "1"] for stream creation sentinel
          var type = (string)fields[1]!;

          toBeDeleted.Add((RedisValue)messageId);

          if (type == "init")
          {
            continue;
          }

          if (type == "end")
          {
            streamEnded = true;
          }
          else if (type == "error")
          {
            if (fields.Length < 4) throw new Exception($"Error entry missing message for id '{messageId}'");
            throw new Exception((string)fields[3]!);
          }
          else
          {
            if (fields.Length < 4) continue;
            RedisValue dataValue = (RedisValue)fields[3];
            byte[]? bytes = dataValue;
            if (bytes == null) continue;
            chunks.Add(bytes);
          }
        }
      }

      return (toBeDeleted, chunks, streamEnded);
    }

    private async Task AddIfStreamExists(string streamKey, params RedisValue[] values)
    {
      var args = new object[3 + values.Length];
      args[0] = streamKey;
      args[1] = "NOMKSTREAM";
      args[2] = "*";
      for (int i = 0; i < values.Length; i++)
      {
        args[3 + i] = values[i];
      }

      var result = await database.ExecuteAsync("XADD", args);

      if (result.IsNull)
      {
        throw new KeyNotFoundException(
            $"Redis stream '{streamKey}' does not exist.");
      }
    }
  }
}