using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;

namespace ExpenseHub.UnitTests.Support;

internal sealed class FakeReportPublisher : IReportPublisher
{
    private readonly List<Guid> _publishedIds = [];

    public bool Confirm { get; set; } = true;

    public IReadOnlyList<Guid> PublishedIds => _publishedIds;

    public Task<bool> PublishAsync(Guid reportId, CancellationToken cancellationToken)
    {
        _publishedIds.Add(reportId);
        return Task.FromResult(Confirm);
    }
}
