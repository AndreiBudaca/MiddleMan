namespace MiddleMan.Communication.Messages
{
  public class ClientQuery
  {
    public int Query { get; set; }
    public string RespondTo { get; set; } = string.Empty;
  }
}