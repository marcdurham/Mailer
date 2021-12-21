using Mailer;
using Mailer.Sender;
using MailerCommon;
using MailerRestApi;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<TimedHostedService>();
builder.Services.AddSingleton<CalendarService>();
builder.Services.AddSingleton<ICustomLogger, CustomLogger>();
builder.Services.Configure<CalendarOptions>(
    builder.Configuration.GetSection("Calendar"));
builder.Services.AddApplicationInsightsTelemetry(
    options =>
    {
        // can't do this after Build()
        options.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATIONKEY");
        options.ConnectionString = System.Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
    });

var app = builder.Build();

app.Logger.LogInformation($"Logging is working");

app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// Reference Document: https://docs.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-6.0&tabs=visual-studio
app.MapGet("/calendar/{prefix}.ics", async (CalendarService service, string prefix) =>
    {
        app.Logger.LogInformation($"Getting (ics) Calendar by prefix: {prefix}");
        return await service.GetCalendarFileAsync(prefix)
            is string shortCalFile
            ? shortCalFile
            : null;
    }
);

app.MapGet("/friend/{meeting}/{name}.ics", async (IMemoryCache memory, string meeting, string name) =>
    {
        app.Logger.LogInformation($"Getting Friend (ics) Calendar: {meeting}:{name.ToUpper()} (app.Logger)");
        return memory.Get<string>($"{meeting}:{name.ToUpper()}");
    }
);

app.MapGet("/health", () =>
    {
        app.Logger.LogInformation($"Health: Green");
        return Results.Ok("Green");
    }
);

app.Run();

// Example: https://docs.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-6.0&tabs=visual-studio
//app.MapPost("/sendmail", async (Message message) =>
//{
//    // TODO: Temporary secret
//    if (message.Text.Contains("414A621D-BB97-4460-AD94-7C9B03C67A3D"))
//    {
//        SmtpEmailSender.Send(message);
//        return Results.Ok("Mail Sent");
//    }
//    else
//    {
//        return Results.BadRequest("Cannot send mail");
//    }

//})
//.WithName("SendMail");

