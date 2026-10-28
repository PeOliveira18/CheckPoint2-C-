using System;
using System.Threading.Tasks;
using ExpenseHub.Core.Data;
using ExpenseHub.Core.Messaging;
using ExpenseHub.Core.Reports;
using ExpenseHub.Worker.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Worker;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddExpenseHubDatabase(builder.Configuration);
        builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IReportRepository, EfReportRepository>();
        builder.Services.AddScoped<ReportProcessor>();
        builder.Services.AddScoped<ReportMessageHandler>();
        builder.Services.AddHostedService<ReportConsumerService>();

        await builder.Build().RunAsync();
    }
}
