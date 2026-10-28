using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;

namespace ExpenseHub.Api.Messaging;

internal sealed class UnconfiguredReportPublisher : IReportPublisher
{
    public Task<bool> PublishAsync(Guid reportId, CancellationToken cancellationToken) => Task.FromResult(false);
}
