using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Auth;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExpenseHub.Api;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();
        builder.Services.AddValidation();
        builder.Services.AddExpenseHubDatabase(builder.Configuration);
        builder.Services.AddExpenseHubAuth(builder.Configuration);
        builder.Services.AddScoped<UserAdminService>();
        builder.Services.AddScoped<IExpenseRepository, EfExpenseRepository>();
        builder.Services.AddScoped<ExpenseService>();
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        WebApplication app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("GetHealth");
        app.MapAuthEndpoints();
        app.MapUserEndpoints();
        app.MapExpenseEndpoints();

        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
        }

        await app.RunAsync();
    }
}
