using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ExpenseHub.Api.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Expenses;

/// <summary>
/// Validação declarativa do DTO de entrada (primeira barreira da API).
/// </summary>
[TestClass]
public sealed class ExpenseRequestValidationTests
{
    /// <summary>Um DTO preenchido corretamente é válido.</summary>
    [TestMethod]
    public void ValidRequestHasNoErrors()
    {
        Assert.IsEmpty(Validate(ValidRequest()));
    }

    /// <summary>Valor abaixo de R$ 0,01 ou acima de Int32.MaxValue é inválido.</summary>
    /// <param name="amount">Valor informado.</param>
    [TestMethod]
    [DataRow("0")]
    [DataRow("2147483648")]
    public void AmountOutOfRangeIsInvalid(string amount)
    {
        ExpenseRequest request = ValidRequest();
        request.Amount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        CollectionAssert.Contains(Validate(request), nameof(ExpenseRequest.Amount));
    }

    /// <summary>Descrição curta e data ausente são inválidas.</summary>
    [TestMethod]
    public void ShortDescriptionAndMissingDateAreInvalid()
    {
        ExpenseRequest request = ValidRequest();
        request.Description = "curta";
        request.ExpenseDate = null;

        List<string> invalid = Validate(request);

        CollectionAssert.Contains(invalid, nameof(ExpenseRequest.Description));
        CollectionAssert.Contains(invalid, nameof(ExpenseRequest.ExpenseDate));
    }

    private static ExpenseRequest ValidRequest() =>
        new()
        {
            Description = "Hotel durante o treinamento",
            Amount = 350m,
            ExpenseDate = new DateOnly(2026, 10, 1),
            CategoryId = 3,
        };

    private static List<string> Validate(ExpenseRequest request)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).ToList();
    }
}
