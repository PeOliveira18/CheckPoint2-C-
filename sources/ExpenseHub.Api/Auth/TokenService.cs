using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Api.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseHub.Api.Auth;

internal sealed class TokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public TokenResponse CreateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        DateTime now = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime expires = now.AddMinutes(_options.ExpirationMinutes);

        List<Claim> claims =
        [
            new(ClaimNames.Subject, user.Id),
            new(ClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimNames.SecurityStamp, user.SecurityStamp ?? string.Empty),
        ];
        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimNames.Role, role));
        }

        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(CreateSigningKey(_options.SigningKey), SecurityAlgorithms.HmacSha256),
        };

        string accessToken = new JsonWebTokenHandler().CreateToken(descriptor);
        return new TokenResponse(accessToken, "Bearer", _options.ExpirationMinutes * 60);
    }

    public static SymmetricSecurityKey CreateSigningKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));
}
