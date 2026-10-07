using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
