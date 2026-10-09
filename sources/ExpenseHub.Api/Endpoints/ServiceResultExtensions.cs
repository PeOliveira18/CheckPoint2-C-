using System;
using ExpenseHub.Api.Application;
using Microsoft.AspNetCore.Http;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Converte falhas de serviço em respostas <c>ProblemDetails</c> consistentes.
/// </summary>
internal static class ServiceResultExtensions
{
    /// <summary>
    /// Devolve a resposta de sucesso ou o <c>ProblemDetails</c> correspondente à falha.
    /// </summary>
    /// <typeparam name="T">Tipo do valor.</typeparam>
    /// <param name="result">Resultado do serviço.</param>
    /// <param name="onSuccess">Fábrica da resposta de sucesso.</param>
    /// <returns>Resposta HTTP.</returns>
    public static IResult ToHttpResult<T>(this ServiceResult<T> result, Func<T, IResult> onSuccess)
    {
        if (result.Succeeded)
        {
            return onSuccess(result.Value);
        }

        if (result.Error == ServiceError.Validation && result.ValidationErrors is not null)
        {
            return TypedResults.ValidationProblem(result.ValidationErrors, title: result.Message);
        }

        (int status, string title) = result.Error switch
        {
            ServiceError.Validation => (StatusCodes.Status400BadRequest, "Requisição inválida."),
            ServiceError.NotFound => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
            ServiceError.Forbidden => (StatusCodes.Status403Forbidden, "Operação não permitida."),
            _ => (StatusCodes.Status409Conflict, "Conflito com o estado atual."),
        };
        return TypedResults.Problem(title: title, detail: result.Message, statusCode: status);
    }
}
