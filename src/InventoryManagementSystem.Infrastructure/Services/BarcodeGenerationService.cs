using System;
using System.Collections.Generic;
using System.Text;
using InventoryManagementSystem.Application.Common.Interfaces;

namespace InventoryManagementSystem.Infrastructure.Services;

public class BarcodeGenerationService : IBarcodeGenerationService
{
    public string GenerateBarcodeSvg(string barcodeValue, string format = "EAN13", double width = 2, double height = 60)
    {
        if (string.IsNullOrWhiteSpace(barcodeValue))
        {
            return @"<svg xmlns=""http://www.w3.org/2000/svg"" width=""100"" height=""30""><text x=""50%"" y=""50%"" fill=""red"" font-size=""12"" text-anchor=""middle"" dominant-baseline=""middle"">No Barcode</text></svg>";
        }

        var trimmed = barcodeValue.Trim();
        bool useEan = format.Equals("EAN13", StringComparison.OrdinalIgnoreCase);

        if (useEan && IsEan13Compatible(trimmed))
        {
            var ean13 = EnsureValidEan13(trimmed);
            var (binary, display) = EncodeEan13Binary(ean13);
            if (!string.IsNullOrEmpty(binary))
            {
                return RenderBinaryBarcodeSvg(binary, display, width, height);
            }
        }

        // Code-128
        var (c128Modules, valid) = EncodeCode128B(trimmed);
        if (valid)
        {
            return RenderRunLengthBarcodeSvg(c128Modules, trimmed, width, height);
        }

        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""200"" height=""40""><text x=""50%"" y=""50%"" fill=""red"" font-size=""12"" text-anchor=""middle"" dominant-baseline=""middle"">Invalid Barcode: {EscapeXml(trimmed)}</text></svg>";
    }

    private static bool IsEan13Compatible(string input)
    {
        if (input.Length != 12 && input.Length != 13) return false;
        foreach (char c in input)
        {
            if (c < '0' || c > '9') return false;
        }
        return true;
    }

    private static string EnsureValidEan13(string input)
    {
        var base12 = input.Length >= 12 ? input.Substring(0, 12) : input.PadLeft(12, '0');
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int d = base12[i] - '0';
            sum += (i % 2 == 1) ? d * 3 : d;
        }
        int check = (10 - (sum % 10)) % 10;
        return base12 + check.ToString();
    }

    private static (string binary, string display) EncodeEan13Binary(string ean13)
    {
        if (ean13.Length != 13) return (string.Empty, string.Empty);

        string[] lPatterns = { "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011" };
        string[] gPatterns = { "0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111" };
        string[] rPatterns = { "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100" };

        string[] parityStructure = {
            "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
            "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"
        };

        int firstDigit = ean13[0] - '0';
        string parity = parityStructure[firstDigit];

        var sb = new StringBuilder();
        sb.Append("101"); // Start

        for (int i = 0; i < 6; i++)
        {
            int digit = ean13[i + 1] - '0';
            sb.Append(parity[i] == 'L' ? lPatterns[digit] : gPatterns[digit]);
        }

        sb.Append("01010"); // Center

        for (int i = 6; i < 12; i++)
        {
            int digit = ean13[i + 1] - '0';
            sb.Append(rPatterns[digit]);
        }

        sb.Append("101"); // Stop

        return (sb.ToString(), ean13);
    }

    private static string RenderBinaryBarcodeSvg(string binary, string displayText, double moduleWidth, double barHeight)
    {
        int quietZoneModules = 10;
        int totalModules = binary.Length + (quietZoneModules * 2);
        double totalWidth = totalModules * moduleWidth;
        double textHeight = 20;
        double totalHeight = barHeight + textHeight + 6;

        var sb = new StringBuilder();
        sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 {totalWidth:F1} {totalHeight:F1}"" width=""100%"" height=""100%"" style=""background-color: transparent;"">");

        double currentX = quietZoneModules * moduleWidth;
        foreach (char bit in binary)
        {
            if (bit == '1')
            {
                sb.AppendLine($@"  <rect x=""{currentX:F2}"" y=""2"" width=""{moduleWidth:F2}"" height=""{barHeight:F2}"" fill=""#000000"" />");
            }
            currentX += moduleWidth;
        }

        sb.AppendLine($@"  <text x=""{totalWidth / 2:F1}"" y=""{barHeight + textHeight}"" font-size=""14"" font-family=""monospace"" font-weight=""600"" fill=""#000000"" text-anchor=""middle"">{EscapeXml(displayText)}</text>");
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static (string ModuleSequence, bool Valid) EncodeCode128B(string input)
    {
        if (string.IsNullOrEmpty(input)) return ("0", false);

        int checksum = 104;
        var values = new List<int> { 104 };

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            int val = c - 32;
            if (val < 0 || val > 95) val = 0;
            values.Add(val);
            checksum += val * (i + 1);
        }

        values.Add(checksum % 103);
        values.Add(106);

        string[] code128Patterns =
        {
            "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
            "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
            "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
            "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
            "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
            "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
            "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
            "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
            "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
            "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
            "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
        };

        var sb = new StringBuilder();
        foreach (int v in values)
        {
            if (v >= 0 && v < code128Patterns.Length)
            {
                sb.Append(code128Patterns[v]);
            }
        }

        return (sb.ToString(), true);
    }

    private static string RenderRunLengthBarcodeSvg(string runLengths, string displayText, double moduleWidth, double barHeight)
    {
        int totalModules = 0;
        foreach (char c in runLengths) totalModules += (c - '0');

        int quietZoneModules = 10;
        double totalWidth = (totalModules + (quietZoneModules * 2)) * moduleWidth;
        double textHeight = 20;
        double totalHeight = barHeight + textHeight + 6;

        var sb = new StringBuilder();
        sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 {totalWidth:F1} {totalHeight:F1}"" width=""100%"" height=""100%"" style=""background-color: transparent;"">");

        double currentX = quietZoneModules * moduleWidth;
        bool isBar = true;

        foreach (char c in runLengths)
        {
            int blockModules = c - '0';
            double blockWidth = blockModules * moduleWidth;

            if (isBar)
            {
                sb.AppendLine($@"  <rect x=""{currentX:F2}"" y=""2"" width=""{blockWidth:F2}"" height=""{barHeight:F2}"" fill=""#000000"" />");
            }

            currentX += blockWidth;
            isBar = !isBar;
        }

        sb.AppendLine($@"  <text x=""{totalWidth / 2:F1}"" y=""{barHeight + textHeight}"" font-size=""14"" font-family=""monospace"" font-weight=""600"" fill=""#000000"" text-anchor=""middle"">{EscapeXml(displayText)}</text>");
        sb.AppendLine("</svg>");

        return sb.ToString();
    }

    private static string EscapeXml(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
    }
}
