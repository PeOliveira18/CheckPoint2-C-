using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Messaging;
using ExpenseHub.Core.Reports;
using Microsoft.Extensions.Logging;

namespace ExpenseHub.Worker.Messaging;

internal sealed class ReportMessageHandler
{
    private readonly ReportProcessor _processor;
    private readonly ILogger<ReportMessageHandler> _logger;

    public ReportMessageHandler(ReportProcessor processor, ILogger<ReportMessageHandler> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task<MessageDisposition> HandleAsync(ReadOnlyMemory<byte> body, bool redelivered, CancellationToken cancellationToken)
    {
        ReportMessage? message = ReportMessage.TryParse(body);
        if (message is null)
        {
            WorkerLog.InvalidMessage(_logger);
            return MessageDisposition.Ack;
        }

        Guid reportId = message.ReportId;
        WorkerLog.Started(_logger, reportId, redelivered);
        try
        {
            ReportProcessingOutcome outcome = await _processor.ProcessAsync(reportId, cancellationToken);
            return ToDisposition(outcome, reportId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            WorkerLog.TransientFailure(_logger, exception, reportId);
            return MessageDisposition.Requeue;
        }
    }

    private MessageDisposition ToDisposition(ReportProcessingOutcome outcome, Guid reportId)
    {
        switch (outcome)
        {
            case ReportProcessingOutcome.Completed:
                WorkerLog.Completed(_logger, reportId);
                return MessageDisposition.Ack;
            case ReportProcessingOutcome.AlreadyFinished:
                WorkerLog.Duplicate(_logger, reportId);
                return MessageDisposition.Ack;
            case ReportProcessingOutcome.Failed:
                WorkerLog.Failed(_logger, reportId);
                return MessageDisposition.Ack;
            case ReportProcessingOutcome.NotFound:
                WorkerLog.NotFound(_logger, reportId);
                return MessageDisposition.Ack;
            default:
                WorkerLog.Conflict(_logger, reportId);
                return MessageDisposition.Requeue;
        }
    }
}
