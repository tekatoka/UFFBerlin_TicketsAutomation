using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using UFFBerlin_TicketsAutomation.Data; // Your custom services like EmailService and GoogleDriveService
using Google.Apis.Drive.v3; // For Google Drive API
using Google.Apis.Gmail.v1;
using Syncfusion.Blazor;

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCd0x0TXxbf1x0ZFZMZF9bRnNPIiBoS35RckVqW35fdXBWRGVcU0Bz");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSyncfusionBlazor();

// Add your custom services
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<GoogleDriveService>();
builder.Services.AddSingleton<CSVService>();
builder.Services.AddSingleton<SettingsService>();

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("wwwroot/appsettings.json", optional: false, reloadOnChange: true);

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Map Blazor hubs and fallback to host page
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
