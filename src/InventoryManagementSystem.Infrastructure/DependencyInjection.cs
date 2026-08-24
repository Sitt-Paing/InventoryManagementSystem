using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Infrastructure.Auth;
using InventoryManagementSystem.Infrastructure.Persistence;
using InventoryManagementSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
                }
            };
        });

        return services;
    }
}
