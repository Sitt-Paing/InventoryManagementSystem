using InventoryManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.Infrastructure.Persistence;

public class ApplicationDbContextInitializer
{
    private readonly ILogger<ApplicationDbContextInitializer> _logger;
    private readonly ApplicationDbContext _context;
    private readonly InventoryManagementDbContext _inventoryContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly IConfiguration _config;

    public ApplicationDbContextInitializer(
        ILogger<ApplicationDbContextInitializer> logger,
        ApplicationDbContext context,
        InventoryManagementDbContext inventoryContext,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config)
    {
        _logger = logger;
        _context = context;
        _inventoryContext = inventoryContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _config = config;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while initialising ApplicationDbContext.");
        }

        try
        {
            if (_inventoryContext.Database.IsSqlServer())
            {
                await _inventoryContext.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An error occurred while initialising InventoryManagementDbContext.");
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // Default roles
        var administratorRole = new IdentityRole("Administrator");
        if (_roleManager.Roles.All(r => r.Name != administratorRole.Name))
        {
            await _roleManager.CreateAsync(administratorRole);
        }

        // Default users
        var adminUserName = _config["DefaultAdmin:UserName"] ?? "devadmin";
        var adminEmail = _config["DefaultAdmin:Email"] ?? "devadmin@gmail.com";
        var adminPassword = _config["DefaultAdmin:Password"] ?? "Password" +
            "ord";
        var administrator = new IdentityUser { UserName = adminUserName, Email = adminEmail };
        if (_userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            var result = await _userManager.CreateAsync(administrator, adminPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(administrator, administratorRole.Name!);
            }
        }
    }
}
