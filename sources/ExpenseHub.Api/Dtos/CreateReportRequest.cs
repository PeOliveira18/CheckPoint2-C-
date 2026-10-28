using System;
using System.ComponentModel.DataAnnotations;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.Api.Dtos;

internal sealed class CreateReportRequest
{
    [Required]
    public DateOnly? FromDate { get; set; }

    [Required]
    public DateOnly? ToDate { get; set; }

    [Required]
    [EnumDataType(typeof(ReportScope))]
    public ReportScope? Scope { get; set; }
}
