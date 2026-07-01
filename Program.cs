using GTAGarageManager.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace GTAGarageManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            builder.Services.AddScoped<GTAGarageManager.Services.GarageService>();
            builder.Services.AddHttpContextAccessor();
            // NEU: Cookie-basierte Authentifizierung
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/login";
                });
            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddSingleton<Supabase.Client>(_ =>
            {
                var url = builder.Configuration["Supabase:Url"] ?? "";
                var key = builder.Configuration["Supabase:Key"] ?? "";
                var options = new Supabase.SupabaseOptions
                {
                    AutoRefreshToken = false,
                    AutoConnectRealtime = false
                };
                return new Supabase.Client(url, key, options);
            });
            var app = builder.Build();
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            // NEU: Auth-Middleware aktivieren
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.MapPost("/login-handler", async (HttpContext context, IConfiguration config) =>
            {
                var form = await context.Request.ReadFormAsync();
                var passwort = form["passwort"].ToString();
                var adminPasswort = config["SiteAuth:AdminPassword"];
                var demoPasswort = config["SiteAuth:DemoPassword"];
                string? rolle = null;
                if (passwort == adminPasswort) rolle = "Admin";
                else if (passwort == demoPasswort) rolle = "Viewer";
                if (rolle != null)
                {
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "GaragenUser"),
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, rolle)
                    };
                    var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new System.Security.Claims.ClaimsPrincipal(identity);
                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) });
                    return Results.Redirect("/");
                }
                return Results.Redirect("/login?fehler=1");
            });
            app.MapPost("/logout", async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            });
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            app.Run();
        }
    }
}