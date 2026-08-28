using JACO.CR.Web.Data;
using JACO.CR.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<CrDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<ApprovalApiClient>();
builder.Services.AddScoped<CRLookupService>();
builder.Services.AddSingleton<CRAttachmentStorage>();
builder.Services.AddHttpClient();

// Shared SSO: trusts the login cookie issued by JACO Portal. See
// JACO-Portal/Docs/SSO.md for the full mechanism -- same key ring + same
// cookie name across Portal/CR/Approval is what makes this work without a
// shared database.
var keyRingPath = builder.Configuration["SharedAuth:KeyRingPath"] ?? @"C:\JACO\_shared\dpkeys";
Directory.CreateDirectory(keyRingPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
    .SetApplicationName("JACO-Platform");

var cookieName = builder.Configuration["SharedAuth:CookieName"] ?? ".JACO.Auth";
var portalLoginUrl = builder.Configuration["SharedAuth:PortalLoginUrl"] ?? "http://localhost:5010/Account/Login";
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = cookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            var returnUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}{ctx.Request.Path}{ctx.Request.QueryString}";
            ctx.Response.Redirect($"{portalLoginUrl}?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CRAdmin", p => p.RequireRole("CR_ADMIN", "PORTAL_ADMIN", "SYSTEM_ADMIN"));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
