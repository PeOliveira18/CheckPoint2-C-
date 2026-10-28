using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExpenseHub.Api.Application;

internal interface IReportPublisher
{
    Task<bool> PublishAsync(Guid reportId, CancellationToken cancellationToken);
}
