namespace InventoryManagementSystem.Application.Common.Interfaces;

public interface IBarcodeGenerationService
{
    string GenerateBarcodeSvg(string barcodeValue, string format = "EAN13", double width = 2, double height = 60);
}
