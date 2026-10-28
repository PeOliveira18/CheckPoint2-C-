using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Core.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Reports;

/// <summary>
/// CP3-I01 — validação de período e escopo da solicitação de relatório.
/// </summary>
[TestClass]
public sealed class ReportRequestValidationTests
{
    /// <summary>Período de um único dia é válido (fromDate igual a toDate).</summary>
    [TestMethod]
    public void SingleDayPeriodIsValid()
    {
        DateOnly day = new(2026, 9, 15);

        Assert.IsEmpty(ReportRules.ValidateRequest(day, day, ReportScope.Mine));
    }

    /// <summary>Data inicial posterior à final é inválida.</summary>
    [TestMethod]
    public void InvertedPeriodIsInvalid()
    {
        Dictionary<string, string[]> errors =
            ReportRules.ValidateRequest(new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 1), ReportScope.Mine);

        AssertKeys(errors, "toDate");
    }

    /// <summary>Datas ausentes são obrigatórias.</summary>
    [TestMethod]
    public void MissingDatesAreInvalid()
    {
        Dictionary<string, string[]> errors = ReportRules.ValidateRequest(null, null, ReportScope.Global);

        AssertKeys(errors, "fromDate", "toDate");
    }

    /// <summary>Escopo ausente ou fora de Mine/Global é inválido.</summary>
    /// <param name="scope">Valor numérico do escopo; -1 representa ausência.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    public void MissingOrUnknownScopeIsInvalid(int scope)
    {
        ReportScope? value = scope < 0 ? null : (ReportScope)scope;

        Dictionary<string, string[]> errors =
            ReportRules.ValidateRequest(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), value);

        AssertKeys(errors, "scope");
    }

    /// <summary>O DTO exige as duas datas e o escopo (primeira barreira da API).</summary>
    [TestMethod]
    public void RequestDtoRequiresDatesAndScope()
    {
        List<string> invalid = Validate(new CreateReportRequest());

        CollectionAssert.AreEquivalent(
            new[] { nameof(CreateReportRequest.FromDate), nameof(CreateReportRequest.ToDate), nameof(CreateReportRequest.Scope) },
            invalid);
    }

    /// <summary>O DTO rejeita valor numérico de escopo que não corresponde a Mine ou Global.</summary>
    [TestMethod]
    public void RequestDtoRejectsUndefinedScope()
    {
        CreateReportRequest request = new()
        {
            FromDate = new DateOnly(2026, 9, 1),
            ToDate = new DateOnly(2026, 9, 30),
            Scope = (ReportScope)9,
        };

        CollectionAssert.AreEquivalent(new[] { nameof(CreateReportRequest.Scope) }, Validate(request));
    }

    private static void AssertKeys(Dictionary<string, string[]> errors, params string[] expected) =>
        CollectionAssert.AreEquivalent(expected, errors.Keys.ToList());

    private static List<string> Validate(CreateReportRequest request)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames).ToList();
    }
}
