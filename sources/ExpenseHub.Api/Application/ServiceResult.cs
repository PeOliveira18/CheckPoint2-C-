using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ExpenseHub.Api.Application;

internal sealed class ServiceResult<T>
{
    private ServiceResult(T? value, ServiceError? error, string? message, IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        Value = value;
        Error = error;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public T? Value { get; }

    public ServiceError? Error { get; }

    public string? Message { get; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Succeeded => Error is null;

    public static ServiceResult<T> Success(T value) => new(value, null, null, null);

    public static ServiceResult<T> Failure(ServiceError error, string message) => new(default, error, message, null);

    public static ServiceResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(default, ServiceError.Validation, "Um ou mais campos são inválidos.", errors);
}
