# Inventory MCP integration

## Implemented: server and dashboard facts tool

The API hosts an authenticated MCP endpoint at `/api/process/mcp` using
`ModelContextProtocol.AspNetCore` 2.3.0 and stateless Streamable HTTP.

The only registered tool is `GetDashboardFacts`:

```json
{
  "startDate": "2026-10-01",
  "endDate": "2026-10-23"
}
```

Dates are inclusive and must use `yyyy-MM-dd`; the existing query validator limits
the period to 90 days. The tool returns a structured `DashboardFactsDto` using
camel-case JSON fields, including `productStocks` and `dailyProductUsage`.
Stock and approved open purchase orders are current snapshots. Only posted usage
is date-filtered. Usage is not sales, and missing observations do not imply zero demand.

The tool calls the existing MediatR query. It does not call Groq, change inventory,
or accept arbitrary SQL. Invalid dates and query validation errors become MCP tool errors.

## Identity and company scope

- Send the existing access JWT in `Authorization: Bearer <access-token>`.
- The endpoint requires the existing `InternalUser` policy and a positive `company_id`
  claim. Anonymous users and supplier accounts cannot list or invoke inventory tools.
- `CompanyId` is not a tool argument. The query uses `ICurrentUserService` and
  the existing EF company filters, with a fresh request scope for every HTTP request.
- The existing optional `X-Company-Id` mismatch check also applies to this route.
- This endpoint deliberately does not authenticate using the browser access cookie.
  MVC antiforgery filters do not run on MCP's minimal API routes. Bearer-only
  authentication avoids introducing a cookie-authenticated path outside those filters.
- Other routes keep their existing cookie and CSRF behavior. Angular must not read
  HttpOnly cookies or store access JWTs in browser storage to call MCP.

Use a local MCP client with this endpoint and the authenticated request's identity.
This first slice does not provide public OAuth discovery or configure third-party
MCP hosts. Keep it within the application's deployment boundary.

## Implemented: backend MCP client and dashboard connection

`InventoryMcpClient` is registered as a scoped service. Its `GetFactsAsync` method
retrieves the access token from the successful JWT authentication ticket and calls
`GetDashboardFacts` over MCP, then deserializes structured content to `DashboardFactsDto`.
It does not read raw browser cookies, issue a new token, or accept company/tool/URL
arguments from the model. Each operation owns and disposes its HTTP and MCP clients.
The HTTP handler disables cookies and redirects; normal HTTPS certificate validation remains enabled.

Configure `Ai:Mcp:Endpoint` with the trusted inventory MCP HTTPS endpoint in the
deployment configuration. Development defaults to `https://localhost:7152/api/process/mcp`.
Other environments require an explicit endpoint. Never build this destination from
the incoming Host header because the originating user's token is forwarded to it.

The dashboard generation controller validates the requested dates with the existing
query validator, then calls this MCP client to obtain facts before calling the existing
AI dashboard service. Date validation keeps the existing 422 response. MCP connection,
tool, and JSON errors return 502; upstream authentication/permission HTTP failures
preserve 401/403; MCP timeouts return 504. Groq errors retain separate error handling.
The GET facts endpoint remains a direct-query fallback independent of MCP and Groq.

## Implemented: bounded Groq tool-calling turn

`IDashboardMcpClient` is an Application-layer contract; the API implements it with the
authenticated MCP client. Infrastructure does not depend on the API or MCP SDK types.
On a definition cache miss, `GroqDashboardAiService` discovers the allowlisted
`GetDashboardFacts` schema through MCP. `GroqAiClient.GenerateWithToolsAsync` sends
that schema and the selected dates/language to Groq without the inventory dataset.

The first provider turn uses `tool_choice: required` with parallel calls disabled.
It must return exactly one allowed function call. The backend parses the arguments,
rejects unknown/extra fields or dates different from the user's period, and calls the
MCP facts tool with the originating user's identity. It then appends the assistant's
tool call and the tool result linked by `tool_call_id` to the same conversation.
The second provider turn omits tool schemas and requests final dashboard JSON.
The existing structural, language-code, dataset, product, unit and chart validators
still run before the result is cached or returned.

This slice supports one required evidence tool, not open-ended multi-tool selection.
There are normally two Groq requests on a successful cache miss and zero on a cache
hit. A final-turn 429 with Retry-After of at most five seconds permits one retry
after that delay plus one second, so the maximum is three provider requests.
Tool selection/execution is never repeated by that retry. Longer waits or a second
429 propagate to the shared cooldown. The controller's initial
MCP snapshot determines the cache key and response facts; a cache miss makes a second
MCP facts read for the model's actual tool call. If the inventory changed between
reads (ignoring snapshot timestamps), generation is rejected rather than mixing
inconsistent facts and descriptions. A later snapshot/session optimization can remove
that second read without weakening freshness or cache behavior.

The tool-completion log is `Dashboard MCP tool GetDashboardFacts completed; requesting
final dashboard.` No access tokens or tool-result data are logged by that message.
Use a Groq model that supports local function calling; the configured model is not
changed automatically. Live model availability and quota remain provider-dependent.

## Provider token-limit handling

On 23 October a live reproduction with the configured Groq model returned 200 for
tool selection but 413 for the final dashboard request: the provider reported 10,359
input tokens against an account/model ITPM limit of 7,000. The current company had
46 stock rows and 70 usage rows, rather than the earlier four-product snapshot.
The old generic 502 message hid this upstream rejection.

The evidence-selection turn now uses a short instruction with an output cap of 512
tokens. The final turn receives the dashboard rules only once, no repeated tool
schemas, and an output cap of 3,072. Output caps bound generation; they do not solve
input limits by themselves. The rules are concise and the model-only evidence is
columnar: product name/unit appear once, stock and usage rows refer to their numeric
catalog index, and all original stock/usage quantities and dates are retained.
Actual product GUIDs remain in the server lookup and in the normal browser/MCP facts.
Unicode names are sent as text instead of literal escaped Unicode sequences inside
the tool-result string. Small authoritative attention/recent-usage samples help the
summary keep product names, quantities and units together.

The model chooses a unit-compatible `chartSelections` key in `productIds`. The
server expands it to actual GUIDs before the public definition is validated. Share
charts use up to eight products with the highest nonnegative quantities in that
selection; trend charts use only products with two recorded dates. Other chart
types retain the full unit selection. Model display text must describe subset
shares as selected products and must not expose internal indexes or selection keys.
These choices affect chart views, not the complete facts returned to the browser.

Unknown selections are omitted. When individual widgets fail validation, usable
widgets are independently revalidated and retained; invalid root/language metadata
or a dashboard without usable widgets still fails. No wrong-unit chart is rendered
to avoid a 502. No alternate key/model is introduced. The only automatic retry is
the single bounded short final-turn 429 retry described above.

Non-429 provider failures now record the upstream status, stage, bounded message
and safe error code in server logs. The public error includes only provider status,
safe code and stage, never the raw provider body, credentials or token values. A 413
has an explicit input-limit message; 429 retains the existing Retry-After/cooldown.
Input limits can still be reached with much larger datasets; this is a compact
encoding, not an unlimited-context guarantee.

References: [Groq API request options](https://console.groq.com/docs/api-reference),
[provider error statuses](https://console.groq.com/docs/errors).

## Dashboard charts and operational summary

The widget contract supports `bar_chart`, `line_chart`, `area_chart`,
`stacked_bar_chart`, `pie_chart` and `doughnut_chart`, alongside cards and tables.
Stock can use bar/pie/doughnut; usage can use every supported chart type.
Circular usage charts aggregate period quantities by product. Their labels are
`productName` and `issuedQuantity`; other usage charts require `date` and
`issuedQuantity`. Stock composition labels are `productName` and `availableQuantity`.
Pie/doughnut charts require 2–8 selected products with nonnegative values and a
positive total. All quantity charts retain the one-known-unit rule. Line and area
charts require two recorded dates per selected product. Missing dates are not
filled with zeros. Composition is quantity share, not monetary value.

Facts now include the server-local `snapshotDate` and derived `stockHealth` counts:
positive quantity above reorder level, positive quantity at/below reorder level,
and zero/negative quantity. These disjoint counts cover all stock products,
including inactive products, and never add quantities across different units.
The snapshot date is included in the AI cache fingerprint.

The prompt requests an operational briefing about the actual stock situation,
open orders, recorded usage, attention points and evidence limitations, rather
than a description of the dashboard. "Today" refers only to observations on
`snapshotDate` inside the selected range; absence of a record does not mean zero
usage. Claims about natural-language accuracy remain model-dependent. Authoritative
stock-status counts are rendered separately below the generated summary. Static
labels/fallback text use the existing dictionaries; the actual AI summary remains
generated in the requested language. Fallback rendering also includes a valid
stock doughnut and usage area chart when the corresponding evidence permits them.

References: [Chart.js pie/doughnut](https://www.chartjs.org/docs/latest/charts/doughnut.html),
[area charts](https://www.chartjs.org/docs/latest/charts/area.html).

## Next slice: recommendation evidence

Add product stock, usage, pending purchase order, and supplier lead-time
tools for evidence-based recommendations. Business calculations stay in application
services; the model explains the results and reports insufficient evidence when needed.

References: [Groq local tool calling](https://console.groq.com/docs/tool-use/local-tool-calling),
[C# SDK tools](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/tools/tools.html),
[stateless HTTP](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/stateless/stateless.html).

## Verification

The API builds successfully. A one-off local MCP connection probe used the actual
endpoint registration, tool and database query in a loopback-only host with an
ephemeral signing key, without running production startup migrations or workers.
It discovered `GetDashboardFacts`, confirmed that only `startDate` and `endDate`
appear in the input schema, and retrieved 4 products and 10 usage observations for
company 1. An unused company scope returned empty datasets on the same MCP client.
Anonymous/cookie-only requests returned 401; supplier/missing-company requests
returned 403; company-header mismatch returned 409. Invalid formats and periods
longer than 90 days returned tool errors.
The existing cookie authentication handler still returned 200 on a protected
non-MCP route in that same host.

A follow-up isolated HTTPS probe exercised the actual `InventoryMcpClient` through
a cookie-authenticated request. It forwarded the validated ticket's token as Bearer
to MCP and returned 200 with 4 products for company 1 and 0 for an unused company.
The fixture used an ephemeral JWT signing key and short-lived certificate, trusting
only that exact certificate in its fixture HTTP handlers. Production TLS validation
was not changed. The probe read existing facts without database writes or Groq calls.
An additional one-off HTTPS probe exercised the actual controller, AI service, MCP
client, tool and existing read-only database query with scripted Groq HTTP responses.
English/Myanmar requests returned 200 with two provider turns, while an unchanged
cached request made zero provider calls. Wrong dates, unknown tool names, malformed
arguments and an extra tool turn were rejected with 502. Invalid periods returned
422, unsupported language returned 400, and a simulated 429 propagated its cooldown;
cache hits remained available during cooldown. The fixture checked `tool_call_id`
correlation and saw the actual four-product tool result in the final provider request.
This verifies orchestration with controlled provider responses, not real Groq model
behavior, Burmese quality or the full production browser flow. No real Groq quota
was consumed and no database writes were made.

The chart extension passed API and Angular production builds. Manual checks in the
ignored local probe accepted the four new chart types and rejected circular data
with a single product, zero total, negative values or mixed units; stock-health
boundary counts remained disjoint. An isolated browser preview used actual dashboard
components and a current database facts snapshot with fixed illustrative definitions
to verify pie/doughnut/area/stacked rendering, desktop light/dark and Myanmar text at
390px width. Document width was 386px, and the browser reported no runtime errors.
This preview does not establish which charts or prose a live model will choose.

After the provider-limit fix, a live English generation reached 200 on both provider
turns with 5,119 input tokens on the final turn. A subsequent local replay of the
actual model response returned 200 after omitting its mixed-unit bar chart, retaining
four valid widgets. The manual fixture checked all 46 stock rows and 70 usage rows
for unchanged values, dates and units through compact encoding, verified selection
expansion to actual GUIDs, safe 413 diagnostics, and existing cache/cooldown behavior.

The final implementation was also exercised live with `language=mm`: both provider
turns and the actual controller returned 200, with 5,141 input tokens on the final
turn, 46 stock rows and 70 usage rows preserved. This confirms request/response and
widget validation, not Burmese language quality: the configured model can still
produce English widget titles or inaccurate Burmese terminology despite instructions.
The running development API was not stopped/restarted by the verification host.

The existing running API still needs its own rebuild/restart to load these changes.

Dashboard facts now include `baseUnitSymbol` from `UnitOfMeasure.Symbol` for both
stock and usage rows. The renderer uses this symbol for unit cells, chart badges,
tooltips, legends and underlying data; a missing symbol displays `—`, never the
internal code. `baseUnit` retains the original code for compatibility validation
and grouping, so units with identical symbols are not silently combined. Model-only
evidence exposes the symbol as its `baseUnit` display value and omits internal
codes. The generation cache version changed to discard old code-based text.
