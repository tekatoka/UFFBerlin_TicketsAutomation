using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using UFFBerlin_TicketsAutomation.Data; // Your custom services like EmailService and GoogleDriveService
using Google.Apis.Drive.v3; // For Google Drive API
using Google.Apis.Gmail.v1;
using Syncfusion.Blazor;
using UFFBerlin_TicketsAutomation.Data.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCd0x0TXxbf1x0ZFZMZF9bRnNPIiBoS35RckVqW35fdXBWRGVcU0Bz");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSyncfusionBlazor();

builder.Services.AddHttpContextAccessor();

// session services
builder.Services.AddDistributedMemoryCache(); // Required for session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(300); // Set session timeout
    options.Cookie.HttpOnly = true; // Make session cookie HTTP-only
    options.Cookie.IsEssential = true; // Mark session cookie as essential
});

// custom scoped services
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<GoogleDriveService>();
builder.Services.AddScoped<CSVService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<GoogleAuthorizationService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();

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

// Enable session middleware
app.UseSession(); // Add session middleware here
app.UseAuthorization();

// Map Blazor hubs and fallback to host page
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
