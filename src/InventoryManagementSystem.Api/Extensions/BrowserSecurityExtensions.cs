using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Extensions;

public static class BrowserSecurityExtensions
{
    public const string CorsPolicy = "TrustedClient";

    public static IServiceCollection AddBrowserSecurity(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // MVC's built-in antiforgery filter requires the ViewFeatures services.
        services.AddControllersWithViews();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        services.Configure<MvcOptions>(options =>
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? (environment.IsDevelopment()
                ? ["http://localhost:4200", "https://localhost:4200"]
                : []);

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
        {
            if (origins.Length > 0)
                policy.WithOrigins(origins);
            policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));

        return services;
    }
}
