namespace ExpenseHub.Core.Messaging;

internal sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string QueueName { get; set; } = "expensehub.reports";

    public int PublishTimeoutSeconds { get; set; } = 5;

    public int RetryDelaySeconds { get; set; } = 5;
}
