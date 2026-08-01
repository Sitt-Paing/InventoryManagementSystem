using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace InventoryManagementSystem.Api.Middleware;

public class EncryptionMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("DefaultSecretKey32BytesLongString!");

    public EncryptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if X-Encrypt-Payload header is present
        if (!context.Request.Headers.ContainsKey("X-Encrypt-Payload"))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        context.Response.Body = originalBodyStream;
        responseBody.Seek(0, SeekOrigin.Begin);
        var plainText = await new StreamReader(responseBody).ReadToEndAsync();

        if (!string.IsNullOrEmpty(plainText))
        {
            var encryptedBase64 = EncryptString(plainText);
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(encryptedBase64);
        }
    }

    private static string EncryptString(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }
}

public static class EncryptionMiddlewareExtensions
{
    public static IApplicationBuilder UseEncryptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EncryptionMiddleware>();
    }
}
