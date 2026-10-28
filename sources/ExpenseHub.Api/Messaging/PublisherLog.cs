using System;
using Microsoft.Extensions.Logging;

namespace ExpenseHub.Api.Messaging;

internal static partial class PublisherLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Relatório {ReportId} publicado com confirmação do broker.")]
    public static partial void Published(ILogger logger, Guid reportId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Relatório {ReportId}: publicação sem confirmação do broker.")]
    public static partial void NotConfirmed(ILogger logger, Exception exception, Guid reportId);
}
