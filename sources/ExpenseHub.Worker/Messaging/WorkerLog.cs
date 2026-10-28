using System;
using Microsoft.Extensions.Logging;

namespace ExpenseHub.Worker.Messaging;

internal static partial class WorkerLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Consumindo a fila {QueueName} (prefetch 1, ack manual).")]
    public static partial void Listening(ILogger logger, string queueName);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Warning, Message = "RabbitMQ indisponível; nova tentativa de conexão em {DelaySeconds} s.")]
    public static partial void BrokerUnavailable(ILogger logger, Exception exception, int delaySeconds);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Information, Message = "Relatório {ReportId}: processamento iniciado (reentrega: {Redelivered}).")]
    public static partial void Started(ILogger logger, Guid reportId, bool redelivered);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Information, Message = "Relatório {ReportId}: concluído e salvo; mensagem confirmada.")]
    public static partial void Completed(ILogger logger, Guid reportId);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Information, Message = "Relatório {ReportId}: duplicata de solicitação já finalizada; confirmada sem novo efeito.")]
    public static partial void Duplicate(ILogger logger, Guid reportId);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Warning, Message = "Relatório {ReportId}: falha definitiva registrada; mensagem confirmada sem reencaminhamento.")]
    public static partial void Failed(ILogger logger, Guid reportId);

    [LoggerMessage(EventId = 2014, Level = LogLevel.Warning, Message = "Relatório {ReportId}: solicitação inexistente; mensagem descartada.")]
    public static partial void NotFound(ILogger logger, Guid reportId);

    [LoggerMessage(EventId = 2015, Level = LogLevel.Warning, Message = "Relatório {ReportId}: conflito de concorrência; mensagem devolvida à fila.")]
    public static partial void Conflict(ILogger logger, Guid reportId);

    [LoggerMessage(EventId = 2016, Level = LogLevel.Warning, Message = "Relatório {ReportId}: falha transitória; mensagem devolvida à fila.")]
    public static partial void TransientFailure(ILogger logger, Exception exception, Guid reportId);

    [LoggerMessage(EventId = 2017, Level = LogLevel.Warning, Message = "Mensagem sem reportId válido; descartada.")]
    public static partial void InvalidMessage(ILogger logger);
}
