namespace InventoryManagementSystem.Infrastructure.Services.Email;

public class RabbitMqEmailSettings
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Queue { get; set; } = "inventory.purchase-order-emails";
}
