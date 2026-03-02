namespace MiddleMan.Web.Communication.Constants
{
  public static class IntraServerCommunicationConstants
  {
    public static string InvocationChannelName(Guid correlation) => $"InterServerInvocationData:{correlation}";

    public static string ResponseChannelName(Guid correlation) => $"InterServerInvocationResponseData:{correlation}";
  }
}