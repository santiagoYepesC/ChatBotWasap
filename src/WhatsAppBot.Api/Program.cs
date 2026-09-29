using WhatsAppBot.Api.Extensions;
using WhatsAppBot.Api.Infrastructure.Bootstrap;
using WhatsAppBot.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using WhatsAppBot.Api.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

builder.Services
    .AddBusiness()
    .AddPersistence()
    .AddInfrastructure(builder.Configuration, builder.Environment)
    .AddIntegrations();

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage) ? $"{entry.Key} is invalid." : error.ErrorMessage))
            .ToArray();
        return new BadRequestObjectResult(ResponseE<object?>.Fail(
            new ApiErrorDTO
            {
                Code = "ValidationFailed",
                Message = "One or more request values are invalid.",
                Details = details
            },
            context.HttpContext.TraceIdentifier));
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (args is ["admin", "bootstrap"])
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BootstrapCommand>().RunAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<WhatsAppBot.Api.Middleware.GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapMetaWhatsAppWebhook();
app.Run();

public partial class Program;
