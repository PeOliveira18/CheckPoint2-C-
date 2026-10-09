namespace ExpenseHub.Api.Contracts;

/// <summary>
/// Resposta do login com o token bearer.
/// </summary>
/// <param name="AccessToken">Token JWT a enviar no cabeçalho <c>Authorization: Bearer</c>.</param>
/// <param name="TokenType">Tipo do token (sempre <c>Bearer</c>).</param>
/// <param name="ExpiresIn">Validade em segundos.</param>
internal sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
