using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace ExpenseHub.Core.Messaging;

internal static class ReportQueue
{
    public static ConnectionFactory CreateFactory(RabbitMqOptions options, string clientName)
    {
        ConnectionFactory factory = new()
        {
            HostName = options.HostName,
            Port = options.Port,
            ClientProvidedName = clientName,
            AutomaticRecoveryEnabled = true,
        };

        if (!string.IsNullOrWhiteSpace(options.UserName))
        {
            factory.UserName = options.UserName;
        }

        if (!string.IsNullOrWhiteSpace(options.Password))
        {
            factory.Password = options.Password;
        }

        return factory;
    }

    public static Task DeclareAsync(IChannel channel, string queueName, CancellationToken cancellationToken) =>
        channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

    public static BasicProperties CreateProperties(Guid reportId)
    {
        string id = reportId.ToString("D", CultureInfo.InvariantCulture);
        return new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = id,
            CorrelationId = id,
        };
    }
}
