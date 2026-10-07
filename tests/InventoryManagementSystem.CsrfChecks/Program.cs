using System.Net;
using System.Security.Claims;
using InventoryManagementSystem.Api.Extensions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;

// Exercise the production security registration over real HTTP without a database,
// SMTP delivery, or inventory mutations. Test identities exist only in this host.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddBrowserSecurity(builder.Configuration, builder.Environment);
await using var app = builder.Build();
app.UseRouting();
app.UseCors(BrowserSecurityExtensions.CorsPolicy);
app.Use(async (context, next) =>
{
    // Model the API's HTTPS requests without installing a test certificate.
    context.Request.Scheme = "https";
    if (context.Request.Headers.TryGetValue("X-Test-User", out var user))
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.ToString())], "Test"));
    await next();
});
app.MapControllers();
await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features
    .Get<IServerAddressesFeature>()!.Addresses.Single();
using var client = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = new Uri(address) };
var checks = 0;

async Task Check(HttpRequestMessage request, HttpStatusCode expected, string label)
{
    using var response = await client.SendAsync(request);
    if (response.StatusCode != expected)
        throw new Exception($"{label}: expected {expected}, got {response.StatusCode}. {await response.Content.ReadAsStringAsync()}");
    Console.WriteLine($"PASS: {label}");
    checks++;
}

async Task<(string Token, string Cookie)> TokenFor(string? user = null)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, "/__csrf-checks/token");
    if (user != null) request.Headers.Add("X-Test-User", user);
    using var response = await client.SendAsync(request);
    response.EnsureSuccessStatusCode();
    return (await response.Content.ReadAsStringAsync(),
        string.Join("; ", response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0])));
}

HttpRequestMessage Mutation(string method, string action, string? token = null, string? cookie = null, string? user = null)
{
    var request = new HttpRequestMessage(new HttpMethod(method), $"/__csrf-checks/{action}");
    if (token != null) request.Headers.Add("X-XSRF-TOKEN", token);
    if (cookie != null) request.Headers.Add("Cookie", cookie);
    if (user != null) request.Headers.Add("X-Test-User", user);
    return request;
}

await Check(new(HttpMethod.Get, "/__csrf-checks/read"), HttpStatusCode.OK, "GET needs no CSRF token");
foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE" })
    await Check(Mutation(method, "write"), HttpStatusCode.BadRequest, $"{method} rejects missing token");
foreach (var action in new[] { "login", "refresh-token", "logout" })
    await Check(Mutation("POST", action), HttpStatusCode.BadRequest, $"anonymous {action} also rejects missing token");
var anonymous = await TokenFor();
await Check(Mutation("POST", "login", anonymous.Token, anonymous.Cookie), HttpStatusCode.OK, "anonymous login accepts matching pair");
await Check(Mutation("POST", "refresh-token", anonymous.Token, anonymous.Cookie), HttpStatusCode.OK, "anonymous/expired-session refresh accepts matching pair");
var alice = await TokenFor("alice");
foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE" })
    await Check(Mutation(method, "write", alice.Token, alice.Cookie, "alice"), HttpStatusCode.OK, $"{method} accepts authenticated matching pair");
await Check(Mutation("POST", "write", "tampered", alice.Cookie, "alice"), HttpStatusCode.BadRequest, "tampered token rejected");
await Check(Mutation("POST", "write", alice.Token, user: "alice"), HttpStatusCode.BadRequest, "missing correlation cookie rejected");
await Check(Mutation("POST", "write", alice.Token, alice.Cookie, "bob"), HttpStatusCode.BadRequest, "another user's token rejected");
await Check(Mutation("POST", "write", anonymous.Token, anonymous.Cookie, "alice"), HttpStatusCode.BadRequest, "pre-login token rejected after identity change");
await Check(Mutation("POST", "logout", alice.Token, alice.Cookie, "alice"), HttpStatusCode.OK, "logout accepts matching pair");

foreach (var (origin, allowed) in new[] { ("http://localhost:4200", true), ("https://untrusted.example", false) })
{
    using var request = new HttpRequestMessage(HttpMethod.Options, "/__csrf-checks/write");
    request.Headers.Add("Origin", origin);
    request.Headers.Add("Access-Control-Request-Method", "POST");
    request.Headers.Add("Access-Control-Request-Headers", "X-XSRF-TOKEN");
    using var response = await client.SendAsync(request);
    var grantsOrigin = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values)
        && values.Contains(origin);
    if (grantsOrigin != allowed) throw new Exception($"Unexpected CORS permission for {origin}.");
    Console.WriteLine($"PASS: CORS {(allowed ? "allows trusted" : "rejects untrusted")} origin");
    checks++;
}
Console.WriteLine($"All {checks} CSRF/CORS integration checks passed.");
await app.StopAsync();

[ApiController]
[Route("__csrf-checks")]
public class CsrfCheckController : ControllerBase
{
    [HttpGet("token")]
    public string Token([FromServices] IAntiforgery antiforgery) =>
        antiforgery.GetAndStoreTokens(HttpContext).RequestToken!;

    [HttpGet("read")]
    public IActionResult Read() => Ok();

    [AcceptVerbs("POST", "PUT", "PATCH", "DELETE")]
    [Route("write")]
    public IActionResult Write() => Ok();

    [HttpPost("login")]
    [HttpPost("refresh-token")]
    [HttpPost("logout")]
    public IActionResult Session() => Ok();
}
