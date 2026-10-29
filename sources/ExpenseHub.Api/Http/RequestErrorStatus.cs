using System;
using Microsoft.AspNetCore.Http;

namespace ExpenseHub.Api.Http;

internal static class RequestErrorStatus
{
    public static int Select(Exception exception) =>
        exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode
            : StatusCodes.Status500InternalServerError;
}
