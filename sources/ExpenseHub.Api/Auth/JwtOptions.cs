using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Auth;

/// <summary>
/// Configuração dos tokens JWT emitidos no login (seção <c>Jwt</c>).
/// </summary>
internal sealed class JwtOptions
{
    /// <summary>Nome da seção de configuração.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Emissor do token.</summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audiência do token.</summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Chave simétrica de assinatura (mínimo de 32 caracteres), mantida fora do repositório.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Validade do token em minutos.</summary>
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;
}
