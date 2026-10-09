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

/// <summary>
/// Emite tokens JWT assinados para usuários autenticados.
/// </summary>
internal sealed class TokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenService"/> class.
    /// </summary>
    /// <param name="options">Configuração do JWT.</param>
    /// <param name="timeProvider">Relógio do servidor.</param>
    public TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Cria o token de acesso com identificador, e-mail, roles e security stamp do usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="roles">Roles atuais do usuário.</param>
    /// <returns>Token e metadados de expiração.</returns>
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

    /// <summary>
    /// Cria a chave simétrica usada para assinar e validar tokens.
    /// </summary>
    /// <param name="signingKey">Chave configurada.</param>
    /// <returns>Chave de segurança.</returns>
    public static SymmetricSecurityKey CreateSigningKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));
}
