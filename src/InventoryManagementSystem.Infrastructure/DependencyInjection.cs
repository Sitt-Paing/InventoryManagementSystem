using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Infrastructure.Identity;
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
            options.UseSqlServer(connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddDbContext<InventoryManagementDbContext>(options =>
            options.UseSqlServer(connectionString,
                b => b.MigrationsAssembly(typeof(InventoryManagementDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<InventoryManagementDbContext>());
        services.AddScoped<ApplicationDbContextInitializer>();

        services.AddScoped<IIdentityService, IdentityService>();

        var secretKey = configuration["JwtSettings:SecretKey"] ?? "SuperSecretKeyForInventoryManagementSystem_JwtToken_2026!#";
        var issuer = configuration["JwtSettings:Issuer"] ?? "InventoryManagementSystem";
        var audience = configuration["JwtSettings:Audience"] ?? "InventoryManagementSystemClient";

        services.AddAuthentication(options =>
        {
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
        });

        return services;
    }
}
