using JWT.Algorithms;
using JWT.Builder;
using MiddleMan.Core.Tokens.Constants;
using MiddleMan.Core.Tokens.Model;

namespace MiddleMan.Core.Tokens
{
  public static class TokenManager
  {
    private static readonly JwtBuilder _jwtBuilder = JwtBuilder.Create()
      .WithAlgorithm(new HMACSHA256Algorithm());

    public static string Generate(TokenData data)
    {
      ArgumentNullException.ThrowIfNull(nameof(data));
      ArgumentException.ThrowIfNullOrWhiteSpace(data.Secret, nameof(data.Secret));

      var claims = new Dictionary<string, object>()
      {
        { TokenClaims.Issuer, TokenConstants.TokenIssuer },
        { TokenClaims.Subject, data.Identifier },
        { TokenClaims.Expiration, DateTimeOffset.UtcNow.AddMinutes(data.Validity).ToUnixTimeSeconds() },
        { TokenClaims.FullName, data.Name },
      };

      return _jwtBuilder.WithSecret(data.Secret).Encode(claims);
    }

    public static IDictionary<string, object>? Parse(string data, string secret)
    {
      if (data == null) return null;

      return _jwtBuilder.WithSecret(secret)
        .MustVerifySignature()
        .Decode<IDictionary<string, object>>(data);
    }
  }
}
