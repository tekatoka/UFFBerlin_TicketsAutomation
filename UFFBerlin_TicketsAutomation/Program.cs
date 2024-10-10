using UFFBerlin_TicketsAutomation.Data; // Your custom services like EmailService and GoogleDriveService
using Syncfusion.Blazor;
using UFFBerlin_TicketsAutomation.Data.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NDaF5cWWtCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXZcdHVQRmlYUU13XkE=");

var builder = WebApplication.CreateBuilder(args);
//builder.WebHost.UseUrls("http://*:5000");
builder.WebHost.UseUrls("http://*:5000;https://*:5001");

// Add services to the container.
builder.Services.AddRazorPages();
//builder.Services.AddServerSideBlazor().AddHubOptions(options =>
//{
//    options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
//    options.KeepAliveInterval = TimeSpan.FromMinutes(1);
//    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
//    options.MaximumReceiveMessageSize = 1024 * 1024 * 5; // 5 MB
//}); ;

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 64 * 1024; // 64 KB
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(1);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    });

builder.Logging.SetMinimumLevel(LogLevel.Debug);


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
builder.Services.AddScoped<CustomAuthenticationStateProvider>();  // Register it explicitly
builder.Services.AddAuthorizationCore();

builder.Services.AddSingleton<CircuitHandler, CustomCircuitHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("wwwroot/appsettings.json", optional: false, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

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
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes to handle API requests
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // Enable attribute-based routing for controllers
    endpoints.MapBlazorHub();
    endpoints.MapFallbackToPage("/_Host");
});

app.UseCors("AllowAll");

app.Run();
