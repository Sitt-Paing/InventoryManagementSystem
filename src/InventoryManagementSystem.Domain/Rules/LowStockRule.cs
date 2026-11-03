using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Domain.Rules;

public static class  LowStockRule
{
    public static bool ShouldNotify(decimal previousQuantity, decimal currentQuantity, decimal reorderLevel)
    {
        if (reorderLevel <= 0) return false; // No notification if reorder level is not set or invalid
        
        bool wasAboveReorderLevel = previousQuantity > reorderLevel;

        bool isNowLow = currentQuantity <= reorderLevel;

        return wasAboveReorderLevel && isNowLow;
    }
}
