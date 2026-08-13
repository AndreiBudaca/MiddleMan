using MiddleMan.Web.Communication.Source;
using MiddleMan.Web.Communication.SourceSink;

namespace MiddleMan.Web.Communication
{
  public class CommunicationSourceContext
  {
    public required ICommunicationSource Source { get; set; }
    public ICommunicationSourceSink? Sink { get; set; }
  }
}