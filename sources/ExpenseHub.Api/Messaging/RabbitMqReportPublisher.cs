using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Core.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace ExpenseHub.Api.Messaging;

internal sealed class RabbitMqReportPublisher : IReportPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqReportPublisher> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqReportPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqReportPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> PublishAsync(Guid reportId, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.PublishTimeoutSeconds));

        await _lock.WaitAsync(cancellationToken);
        try
        {
            IChannel channel = await GetChannelAsync(timeout.Token);
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.QueueName,
                mandatory: true,
                basicProperties: ReportQueue.CreateProperties(reportId),
                body: new ReportMessage(reportId).ToBytes(),
                cancellationToken: timeout.Token);

            PublisherLog.Published(_logger, reportId);
            return true;
        }
        catch (Exception exception) when (IsPublishFailure(exception, cancellationToken))
        {
            PublisherLog.NotConfirmed(_logger, exception, reportId);
            await ResetAsync();
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ResetAsync();
        _lock.Dispose();
    }

    private static bool IsPublishFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is RabbitMQClientException or IOException or TimeoutException ||
        (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await ResetAsync();
        ConnectionFactory factory = ReportQueue.CreateFactory(_options, "expensehub-api");
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await ReportQueue.DeclareAsync(_channel, _options.QueueName, cancellationToken);
        return _channel;
    }

    private async Task ResetAsync()
    {
        IChannel? channel = _channel;
        IConnection? connection = _connection;
        _channel = null;
        _connection = null;

        try
        {
            if (channel is not null)
            {
                await channel.DisposeAsync();
            }

            if (connection is not null)
            {
                await connection.DisposeAsync();
            }
        }
        catch (Exception exception) when (exception is RabbitMQClientException or IOException)
        {
            _logger.LogDebug(exception, "Falha ao descartar a conexão com o RabbitMQ.");
        }
    }
}
