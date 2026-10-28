using System;
using ExpenseHub.Core.Domain;

namespace ExpenseHub.Api.Application;

internal sealed record ReportInput(DateOnly? FromDate, DateOnly? ToDate, ReportScope? Scope);
