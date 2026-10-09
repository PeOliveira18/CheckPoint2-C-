using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Dtos;

/// <summary>
/// Dados para <c>POST /register</c>. Não aceita roles: o usuário é criado sem nenhuma.
/// </summary>
internal sealed class RegisterRequest
{
    /// <summary>E-mail, usado também como nome de usuário.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Senha (mínimo de 8 caracteres, com maiúscula, minúscula, dígito e símbolo).</summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
