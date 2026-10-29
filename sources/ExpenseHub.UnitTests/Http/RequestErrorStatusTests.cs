using System;
using System.Text.Json;
using ExpenseHub.Api.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Http;

/// <summary>
/// Corpo de requisição ilegível responde 400 em qualquer ambiente; falhas inesperadas continuam 500.
/// </summary>
[TestClass]
public sealed class RequestErrorStatusTests
{
    /// <summary>JSON inválido (por exemplo, enum desconhecido) vira 400, não 500.</summary>
    [TestMethod]
    public void UnreadableBodyReturnsBadRequest()
    {
        BadHttpRequestException exception = new("Failed to read parameter from the request body as JSON.", new JsonException());

        Assert.AreEqual(StatusCodes.Status400BadRequest, RequestErrorStatus.Select(exception));
    }

    /// <summary>O status próprio da exceção de requisição é preservado.</summary>
    [TestMethod]
    public void BadRequestExceptionKeepsItsStatusCode()
    {
        BadHttpRequestException exception = new("Payload too large.", StatusCodes.Status413PayloadTooLarge);

        Assert.AreEqual(StatusCodes.Status413PayloadTooLarge, RequestErrorStatus.Select(exception));
    }

    /// <summary>Qualquer outra exceção continua sendo erro interno.</summary>
    [TestMethod]
    public void OtherExceptionsReturnInternalServerError()
    {
        Assert.AreEqual(StatusCodes.Status500InternalServerError, RequestErrorStatus.Select(new InvalidOperationException()));
    }
}
