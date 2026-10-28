using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Core.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace ExpenseHub.Worker.Messaging;

internal sealed class ReportConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ReportConsumerService> _logger;

    public ReportConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ReportConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    private TimeSpan RetryDelay => TimeSpan.FromSeconds(_options.RetryDelaySeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConnectionFactory factory = ReportQueue.CreateFactory(_options, "expensehub-worker");
        await using IConnection connection = await ConnectAsync(factory, stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await ReportQueue.DeclareAsync(channel, _options.QueueName, stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, stoppingToken);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);
        await channel.BasicConsumeAsync(_options.QueueName, autoAck: false, consumer, stoppingToken);
        WorkerLog.Listening(_logger, _options.QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
    }

    private async Task<IConnection> ConnectAsync(ConnectionFactory factory, CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                return await factory.CreateConnectionAsync(stoppingToken);
            }
            catch (BrokerUnreachableException exception)
            {
                WorkerLog.BrokerUnavailable(_logger, exception, _options.RetryDelaySeconds);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        MessageDisposition disposition;
        await using (AsyncServiceScope scope = _scopeFactory.CreateAsyncScope())
        {
            ReportMessageHandler handler = scope.ServiceProvider.GetRequiredService<ReportMessageHandler>();
            disposition = await handler.HandleAsync(delivery.Body, delivery.Redelivered, stoppingToken);
        }

        if (disposition == MessageDisposition.Ack)
        {
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        await Task.Delay(RetryDelay, stoppingToken);
        await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, stoppingToken);
    }
}
