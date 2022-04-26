using Mailer;
using Mailer.Sender;
using MailerCommon;
using MailerRestApi;
using MailerRestApi.Services;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddJsonFile("documents.json");
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<TimedHostedService>();
builder.Services.AddSingleton<CalendarService>();
builder.Services.AddSingleton<ICustomLogger<PublisherEmailer>, CustomLogger<PublisherEmailer>>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.Configure<CalendarOptions>(
    builder.Configuration.GetSection("Calendar"));
    builder.Services.AddApplicationInsightsTelemetry(
        options =>
        {
            // can't do this after Build()
            options.InstrumentationKey = System.Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_INSTRUMENTATIONKEY");
            options.ConnectionString = System.Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        });

builder.Services.AddRazorPages();

var app = builder.Build();

app.Logger.LogInformation($"Logging is working");

app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
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

app.MapGet("/friend/{name}.ics", (IMemoryCache memory, string name) =>
{
    app.Logger.LogInformation($"Getting Friend (ics) Calendar: {name.ToUpper()} (app.Logger)");
    return memory.Get<string>($"{name.ToUpper()}");
}
);

string scheduleRootFolder = app.Configuration.GetValue<string>("Schedules:StaticScheduleRootFolder");
string[] scheduleFiles = new string[] { "clm", "pw", "mfs" };

app.MapGet("/health", () =>
    {
        List<string> missingFiles = new();
        foreach(string file in scheduleFiles)
        {
            string path = Path.Combine(scheduleRootFolder, $"{file}.html");
            if (!File.Exists(path))
            {
                app.Logger.LogInformation($"Missing File: {path}");
                missingFiles.Add(path);
            }
        }
        
        if (missingFiles.Count == 0)
        {
            app.Logger.LogInformation($"Health: Green");
            return Results.Ok("Green");
        }
        else
        {
            app.Logger.LogInformation($"Health: Red");
            return Results.Ok($"Red. Missing static HTML files: {string.Join(",", missingFiles)}");
        }        
    }
);

app.MapGet("/schedules/generate/{meetingName}/{key}", (
    IScheduleService scheduler, 
    IConfiguration configuration,
    string meetingName,
    string key) =>
{
    string apiKey = configuration.GetValue<string>("ScheduleGeneratorApiKey");
    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(apiKey))
        return Results.BadRequest("Bad key");

    scheduler.Run(meetingName);
    return Results.Ok();
});

app.MapRazorPages();

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



