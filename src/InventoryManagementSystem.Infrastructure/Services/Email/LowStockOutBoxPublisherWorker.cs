using InventoryManagementSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Infrastructure.Services.Email;

public sealed class LowStockOutBoxPublisherWorker (
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqEmailSettings> rabbitMqOptions,
    IConfiguration configuration,
    ILogger<LowStockOutBoxPublisherWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly RabbitMqEmailSettings _rabbitMqOptions = rabbitMqOptions.Value;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<LowStockOutBoxPublisherWorker> _logger = logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private readonly LowStockOutBoxPublisher _publisher = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(!_configuration.GetValue<bool>("LowStockNotifications:PublisherEnabled"))
        {
            _logger.LogInformation("Low stock notifications are disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPublisherAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Low stock publisher failed. Retrying in 10 seconds.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task RunPublisherAsync(CancellationToken stoppingToken)
    {
        var settings = _rabbitMqOptions;

        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,   
            VirtualHost = settings.VirtualHost,
            AutomaticRecoveryEnabled = false,
            ClientProvidedName = "Inventory low stock publisher"
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true
            );

        await using var channel = await connection.CreateChannelAsync(channelOptions, stoppingToken);

        await channel.QueueDeclareAsync(
            queue: LowStockOutBoxPublisher.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested
            && connection.IsOpen
            && channel.IsOpen)
        {
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<InventoryManagementDbContext>();

                await _publisher.PublishPendingAsync(
                    context, channel, stoppingToken);
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
