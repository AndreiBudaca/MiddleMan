using NATS.Client.JetStream;

namespace MiddleMan.Communication.Channels.Classes
{
  internal sealed class StreamConsumeSession(
    string streamKey,
    string subject,
    INatsJSContext context,
    INatsJSConsumer consumer,
    string consumerName,
    NatsJSFetchOpts fetchOpts,
    TimeSpan idleTimeout)
  {
    public string StreamKey { get; } = streamKey;
    public string Subject { get; } = subject;
    public INatsJSContext Context { get; } = context;
    public INatsJSConsumer Consumer { get; } = consumer;
    public string ConsumerName { get; } = consumerName;
    public NatsJSFetchOpts FetchOpts { get; } = fetchOpts;
    public TimeSpan IdleTimeout { get; } = idleTimeout;
    public long Expected { get; private set; } = 0;
    public DateTime LastProgress { get; private set; } = DateTime.UtcNow;
    public bool Finished { get; private set; } = false;

    public void MarkProgress() => LastProgress = DateTime.UtcNow;

    public void Advance()
    {
      Expected++;
      MarkProgress();
    }

    public void Complete() => Finished = true;
  }
}
