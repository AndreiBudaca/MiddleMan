using MiddleMan.Communication.SyncMechanisms;
using MiddleMan.Core;
using MiddleMan.Data.InMemory;
using StackExchange.Redis;

namespace MiddleMan.Communication.Channels
{
  public class RedisCommunicationChannel : ICommunicationChannel
  {
    private readonly ISubscriber subscriber;
    private readonly AsyncResourceMonitor<Guid> eventsMonitor = new();
    private readonly IInMemoryContext context;

    public RedisCommunicationChannel(string connectionString, IInMemoryContext context)
    {
      this.context = context;
      var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
      {
        EndPoints = { connectionString },
        AsyncTimeout = ServerCapabilities.GlobalTimeoutSeconds * 1000,
        SyncTimeout = ServerCapabilities.GlobalTimeoutSeconds * 1000,
      });

      subscriber = redis.GetSubscriber();
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
        Console.WriteLine($"Received raw bytes message on topic '{topic}' with length {rawBytes.Length}");
        await onMessageReceived(rawBytes);
      });
    }

    public async Task<Task<T?>> SubscribeAndPeekChannelAsync<T>(string topic)
    {
      var correlation = Guid.NewGuid();

      await subscriber.SubscribeAsync(RedisChannel.Literal(topic), async (channel, message) =>
      {
        var deserializedMessage = DeserializeMessage<T>(message) ?? 
          throw new Exception($"Failed to deserialize message for topic '{topic}'");
        await eventsMonitor.SetResourceAndNotify(async () => context.AddToHash("channelMessages", correlation.ToString(), deserializedMessage), correlation);
      });

      return eventsMonitor.WaitToGetResource(async () => context.GetFromHash<T>("channelMessages", correlation.ToString()), (message) => message != null, correlation);
    }

    public Task UnsubscribeAsync(string topic)
    {
      return subscriber.UnsubscribeAsync(RedisChannel.Literal(topic));
    }

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
  }
}