using UFFBerlin_TicketsAutomation.Data; // Your custom services like EmailService and GoogleDriveService
using Syncfusion.Blazor;
using UFFBerlin_TicketsAutomation.Data.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXZcdHVQRmlYUU13XkE=");

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://*:5000");
builder.WebHost.UseUrls("http://*:5000;https://*:5001");

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

//app.UseCors(policy => policy
//    .AllowAnyOrigin()
//    .AllowAnyMethod()
//    .AllowAnyHeader());

// Map controller routes to handle API requests
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // Enable attribute-based routing for controllers
    endpoints.MapBlazorHub();
    endpoints.MapFallbackToPage("/_Host");
});

app.Run();
