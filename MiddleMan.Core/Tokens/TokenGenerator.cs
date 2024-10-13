using MiddleMan.Core.Tokens.Model;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MiddleMan.Core.Tokens
{
  public static class TokenManager
  {
    public const int DefaultValidity = 30;

    public static string Generate(TokenData data)
    {
      ArgumentNullException.ThrowIfNull(nameof(data));
      ArgumentException.ThrowIfNullOrWhiteSpace(data.Secret, nameof(data.Secret)); 

      var jsonData = new Token
      {
        Identifier = data.Identifier,
        Name = data.Name,
        ValidTill = DateTime.UtcNow.AddMinutes(data.Validity),
      };

      var jsonString = JsonSerializer.Serialize(jsonData);
      var tokenData = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString));

      var signatureBytes = Encoding.UTF8.GetBytes($"{tokenData}{data.Secret}");
      var signature = Convert.ToBase64String(SHA256.HashData(signatureBytes));

      return $"{tokenData}.{signature}";
    }

    public static Token? Parse(string data, string secret)
    {
      if (data == null) return null;

      var dataParts = data.Split('.', StringSplitOptions.RemoveEmptyEntries);
      if (dataParts.Length != 2) return null;

      var receivedTokenData = dataParts[0];
      var receivedSignature = dataParts[1];

      var signatureBytes = Encoding.UTF8.GetBytes($"{receivedTokenData}{secret}");
      var signature = Convert.ToBase64String(SHA256.HashData(signatureBytes));
      
      if (receivedSignature != signature) return null;

      var jsonData = Encoding.UTF8.GetString(Convert.FromBase64String(receivedTokenData));
      var token = JsonSerializer.Deserialize<Token>(jsonData);
      if (token == null) return null;

      if (token.ValidTill < DateTime.UtcNow) return null;
      return token;
    }
  }
}
