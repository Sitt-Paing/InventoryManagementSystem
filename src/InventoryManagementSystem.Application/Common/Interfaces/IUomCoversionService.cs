using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IUomConversionService
{
    Task<decimal> ConvertToBaseUomAsync(Guid productId, long fromUomId, decimal quantity, CancellationToken cancellationToken);
}
