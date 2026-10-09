using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Resultado de uma operação de serviço: um valor ou uma falha tipada.
/// </summary>
/// <typeparam name="T">Tipo do valor em caso de sucesso.</typeparam>
internal sealed class ServiceResult<T>
{
    private ServiceResult(T? value, ServiceError? error, string? message, IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Value = value;
        Error = error;
        Message = message;
        ValidationErrors = validationErrors;
    }

    /// <summary>Valor produzido em caso de sucesso.</summary>
    public T? Value { get; }

    /// <summary>Tipo da falha; nulo em caso de sucesso.</summary>
    public ServiceError? Error { get; }

    /// <summary>Mensagem legível da falha.</summary>
    public string? Message { get; }

    /// <summary>Erros por campo, quando a falha é de validação.</summary>
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    /// <summary>Indica se a operação foi concluída.</summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Succeeded => Error is null;

    /// <summary>Cria um resultado de sucesso.</summary>
    /// <param name="value">Valor produzido.</param>
    /// <returns>Resultado de sucesso.</returns>
    public static ServiceResult<T> Success(T value) => new(value, null, null, null);

    /// <summary>Cria um resultado de falha.</summary>
    /// <param name="error">Tipo da falha.</param>
    /// <param name="message">Mensagem legível.</param>
    /// <returns>Resultado de falha.</returns>
    public static ServiceResult<T> Failure(ServiceError error, string message) => new(default, error, message, null);

    /// <summary>Cria uma falha de validação com erros por campo.</summary>
    /// <param name="errors">Erros por campo.</param>
    /// <returns>Resultado de falha de validação.</returns>
    public static ServiceResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(default, ServiceError.Validation, "Um ou mais campos são inválidos.", errors);
}
