using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Domain.Services;

public class UomQuantityCalculator
{
    public decimal Convert(decimal quantity,decimal factor)
    {
        if (quantity < 0) throw new ArgumentException("Quantity should not be negative",nameof(quantity));
        if (factor <= 0) throw new ArgumentException("Factor must be greater than 0",nameof(factor));

        return quantity * factor;
    }
}
