namespace ExpenseHub.Api.Contracts;

internal sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
