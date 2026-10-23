using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Infrastructure.Auth;
using InventoryManagementSystem.Infrastructure.Persistence;
using InventoryManagementSystem.Infrastructure.Services;
using InventoryManagementSystem.Infrastructure.Services.AI;
using InventoryManagementSystem.Infrastructure.Services.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace InventoryManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddDbContext<InventoryManagementDbContext>(options =>
        {
            options.UseSqlServer(connectionString,
                b => b.MigrationsAssembly(typeof(InventoryManagementDbContext).Assembly.FullName));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<InventoryManagementDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddTransient<IExportService, ExportService>();
        services.AddTransient<IBarcodeGenerationService, BarcodeGenerationService>();
        services.AddOptions<GroqSettings>()
            .Bind(configuration.GetSection(GroqSettings.SectionName))
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.ApiKey),
                "Configure Ai:Groq:ApiKey before using AI features.");

        services.AddHttpClient<GroqAiClient>(client =>
        {
            client.BaseAddress =
                new Uri("https://api.groq.com/openai/v1/");

            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddSingleton<DashboardAiCache>();
        services.AddScoped<IDashboardAiService, GroqDashboardAiService>();
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.AddTransient<IEmailService, SmtpEmailService>();
        services.Configure<RabbitMqEmailSettings>(configuration.GetSection("RabbitMqEmail"));
        services.AddHostedService<PurchaseOrderEmailWorker>();


        var secretKey = configuration["JwtSettings:SecretKey"] ?? "SuperSecretKeyForInventoryManagementSystem_JwtToken_2026!#";
        var issuer = configuration["JwtSettings:Issuer"] ?? "InventoryManagementSystem";
        var audience = configuration["JwtSettings:Audience"] ?? "InventoryManagementSystemClient";

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("access_token", out var token))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (principal?.IsInRole("Supplier") != true) return;

                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(principal.FindFirst("company_id")?.Value, out var companyId)
                        || !int.TryParse(principal.FindFirst("supplier_id")?.Value, out var supplierId))
                    {
                        context.Fail("Supplier account is not configured correctly.");
                        return;
                    }

                    var inventory = context.HttpContext.RequestServices.GetRequiredService<InventoryManagementDbContext>();
                    var active = await inventory.AspNetUsers.AsNoTracking()
                        .AnyAsync(account => account.Id == userId && account.CompanyId == companyId
                            && account.SupplierId == supplierId
                            && account.Roles.Any(role => role.Name == "Supplier")
                            && account.Roles.All(role => role.Name == "Supplier"), context.HttpContext.RequestAborted);
                    active = active && await inventory.Suppliers.IgnoreQueryFilters()
                        .AnyAsync(supplier => supplier.Id == supplierId && supplier.CompanyId == companyId
                            && supplier.Status && !supplier.DeletedOn.HasValue, context.HttpContext.RequestAborted);
                    active = active && await inventory.Companies.AsNoTracking()
                        .AnyAsync(company => company.Id == companyId && company.IsActive && !company.DeletedOn.HasValue,
                            context.HttpContext.RequestAborted);
                    if (!active) context.Fail("Supplier account is inactive or unavailable.");
                }
            };
        });

        return services;
    }
}
