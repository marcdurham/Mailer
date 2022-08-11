using ScheduleViewer.EmailDataServices;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("configuration.json",optional: false);

// Add services to the container.

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IEmailDataService, EmailDataService>();
builder.Services.AddScoped<ISpreadSheetService, GoogleSheets>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action=Index}/{id?}");

app.MapFallbackToFile("index.html"); ;

app.Run();
