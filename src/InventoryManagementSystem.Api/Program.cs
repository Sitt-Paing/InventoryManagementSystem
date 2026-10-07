using InventoryManagementSystem.Api.Extensions;
using InventoryManagementSystem.Api.Middleware;
using InventoryManagementSystem.Api.Services;
using InventoryManagementSystem.Application;
using InventoryManagementSystem.Infrastructure;
using InventoryManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Services
builder.Services.AddApplicationServices();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddInfrastructureServices(builder.Configuration);

// Register Global Exception Handler & ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddBrowserSecurity(builder.Configuration, builder.Environment);

builder.Services.AddOpenApiDoc();

var app = builder.Build();

// Initialise and seed database
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
        await initializer.InitialiseAsync();
        await initializer.SeedAsync();
    }

    app.MapOpenApi();
    app.MapScalarApiReference("/docs/scalar");
}


app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseDefaultFiles();

app.UseStaticFiles();

app.UseRouting();
app.UseCors(BrowserSecurityExtensions.CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
