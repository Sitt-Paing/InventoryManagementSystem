# Inventory Management System — Project Learning Plan

ဒီ plan ကို အရင်ရေးပြီးသား feature တွေ ပြန်လေ့လာဖို့နဲ့ နောက်ရေးမယ့် feature တိုင်းကို ကိုယ်တိုင် design၊ implement၊ debug လုပ်တတ်ဖို့ သုံးပါ။ အချိန်ခွဲထားတဲ့ အရင် 12-week plan ကို [Learning roadmap](learning-roadmap.md) မှာ ကြည့်နိုင်ပါတယ်။ ဒီဖိုင်က feature အလိုက် ပြန်ဖတ်ပြီး ဆက်ဖြည့်သွားမယ့် လမ်းညွှန်ပါ။

## ဘယ်လိုစမလဲ

တစ်ကြိမ်မှာ feature တစ်ခုရဲ့ action တစ်ခုပဲ ရွေးပါ။ ဥပမာ **Stock Issue Post**၊ **Product List Search** သို့မဟုတ် **Low Stock Bell ဖွင့်ခြင်း**။ မိနစ် ၃၀ ရရင် ၅ မိနစ် requirement ရေး၊ ၁၅ မိနစ် code လိုက်ဖတ်၊ ၁၀ မိနစ် ကိုယ်တိုင်ပြန်ရှင်းပါ။ မပြီးရင် နောက် session မှာ အဲဒီ action ကို ဆက်ပါ။

Action တစ်ခုကို UI ကနေ စပြီး **Angular → HTTP → API → handler/service → SQL** အတိုင်း လိုက်ကြည့်ပါ။ Background အလုပ်ဆို **SQL → Publisher → Queue → Consumer → ရလဒ်** ကို သီးခြားလိုက်ပါ။ Screen ပေါ်က value တစ်ခုကို ရွေးပြီး ဘယ် column၊ query နဲ့ response field ကနေ ရောက်လာလဲ ရှာကြည့်ပါ။

Method တစ်ခုတွေ့တိုင်း အောက်ပါအတိုင်း ဖတ်ပါ။

1. **ဘာဝင်၊ ဘာထွက်လဲ။** Signature၊ parameter types၊ return type ကို IntelliSense သို့မဟုတ် Go to Definition နဲ့ ကြည့်ပါ။
2. **နောက်ကွယ်မှာ ဘာလုပ်လဲ။** Memory ထဲတွက်တာလား၊ SQL query ထွက်တာလား၊ network call လား၊ data ပြောင်းတာလား ခွဲပါ။
3. **ဘာကြောင့် ဒီ method ကို ရွေးလဲ။** ဥပမာ row လိုရင် `FirstOrDefaultAsync`၊ ရှိမရှိပဲ လိုရင် `AnyAsync`။
4. **Failure မှာ ဘာဖြစ်လဲ။** `null`၊ empty list၊ exception နဲ့ retry တို့ကို သီးခြားစဉ်းစားပါ။
5. **ကိုယ်တိုင် ရေးနိုင်လား။** Source ပိတ်ပြီး အရေးကြီးတဲ့ query၊ rule သို့မဟုတ် method တစ်ခုကို ပြန်ရေးပါ။ Docs ကြည့်လို့ရပါတယ်။

## ရေးပြီးသား feature တွေကို ပြန်လေ့လာရန်

အောက်က ဇယားကို အစဉ်လိုက် တစ်ကြိမ်ဖြတ်ပါ။ သိပြီးသားအပိုင်းကို အမြန်ပြန်ရှင်းပြီး အားနည်းတဲ့အပိုင်းမှာ session ထပ်ယူပါ။ Feature ရှိနေတာနဲ့ ကိုယ်တိုင်ပြန်ရေးနိုင်တာကို သီးခြားမှတ်ပါ။

| Feature | Theory နဲ့ syntax | ကိုယ်တိုင်လုပ်မယ့် exercise |
| --- | --- | --- |
| Product၊ Category၊ Supplier၊ Department၊ Warehouse/Location၊ UOM | CRUD၊ entity/DTO၊ primary key/FK၊ validation၊ EF tracking၊ Angular form | Product တစ်ခု ပြင်တဲ့ flow လိုက်ပါ။ Request field၊ database column နဲ့ UI field ကို တွဲမှတ်ပါ။ |
| Search၊ Filter၊ Sort၊ Pagination၊ Stock Balance Report | LINQ `Where`၊ `Select`၊ `OrderBy`၊ `CountAsync`၊ `Skip`/`Take`၊ date filter | Product search query ပြန်ရေးပါ။ Filter ပြီးမှ count လုပ်တာနဲ့ page ခွဲတာ ဘာကြောင့် အစဉ်လိုလဲ ရှင်းပါ။ |
| Product–Supplier links၊ PO supplier selection | Many-to-many relation၊ junction table၊ server validation၊ dependent dropdown | Supplier ပြောင်းရင် အရင် product selection ကို ဘာကြောင့် ရှင်းလဲ၊ server မှာ ဘာထပ်စစ်ရလဲ လိုက်ဖတ်ပါ။ |
| Purchase Order approval | Status၊ allowed transitions၊ validation၊ concurrency | Draft → Approval → နောက် action တွေကို ဇယားဆွဲပြီး ထပ်နှိပ်တာနဲ့ ခွင့်မရှိတဲ့ transition ကို စမ်းပါ။ [Approval notes](purchase-order-approval.md) |
| Goods Receipt၊ partial receipt၊ UOM | Quantity precision၊ unit conversion၊ transactions၊ duplicate request | PO quantity ၁၀ ကို ၆ နဲ့ ၄ ခွဲလက်ခံတဲ့ example တွက်ပါ။ Storage unit၊ display unit နဲ့ over-receipt check ကို ခွဲရှင်းပါ။ |
| Purchase Return၊ GRN/Return amounts | Source receipt reference၊ returnable balance၊ price snapshots၊ posting | GRN နှစ်ခုက item တွေ ပြန်ပို့တဲ့အခါ ဘယ် quantity နဲ့ ဘယ် unit price သုံးလဲ trace လုပ်ပါ။ [Return](purchase-return-mm.md) · [Amounts](receipt-return-amounts-mm.md) |
| Stock Issue နဲ့ Stock Transactions | Draft/Post၊ warehouse balance၊ before/after၊ atomic commit | Stock မလုံလောက်တဲ့ case နဲ့ Post အောင်မြင်တဲ့ case နှစ်ခုကို စမ်းပြီး ဘယ် row တွေပြောင်းသွားလဲ စစ်ပါ။ [Stock Issue](stock-issue-mm.md) |
| Company selection၊ Auth၊ Supplier Portal | HTTP cookie၊ access/refresh flow၊ CSRF၊ role၊ tenant/supplier scope | Internal user နဲ့ supplier တစ်ယောက်ချင်း ဘယ် record မြင်ရလဲ လိုက်ကြည့်ပါ။ Route guard နဲ့ server policy ဘယ်နေရာမှာ စစ်လဲ ခွဲရှင်းပါ။ [Company scope](company-scope-mm.md) · [Supplier portal](supplier-portal-mm.md) |
| PO email နဲ့ Low Stock Alerts | Outbox၊ RabbitMQ၊ typed options၊ DI scope၊ confirms၊ ACK၊ idempotency | Outbox ID တစ်ခုကို Queue၊ Consumer၊ database result အထိ လိုက်ပါ။ [Low Stock local setup](../README.md#optional-low-stock-alerts) |
| Barcode၊ Printing၊ Excel/CSV export | Lookup၊ output format၊ file response၊ print layout | Barcode နဲ့ SKU lookup ကို ယှဉ်ပါ။ Export request ကနေ file download အထိ data ဘယ်ပုံစံပြောင်းလဲ ရေးပါ။ |
| Angular header/sidebar၊ language၊ theme | Signals၊ event/property binding၊ RxJS၊ router၊ translation pipe၊ CSS variables | Theme toggle သို့မဟုတ် bell click တစ်ခုကို event → state → template update အထိ လိုက်ပါ။ |
| AI Dashboard၊ MCP၊ Redis | Facts နဲ့ AI output ခွဲပုံ၊ validation၊ cache key/TTL၊ fallback၊ concurrent requests | Cache hit၊ miss နဲ့ AI provider မရတဲ့ case သုံးခုအတွက် UI ဘာပြမလဲ trace လုပ်ပါ။ [AI dashboard notes](ai-dashboard-mcp.md) |

## Concept တစ်ခုကို feature အများကြီးနဲ့ ဆက်လေ့လာရန်

- **C#** — `decimal` ကို quantity/amount တွေမှာ၊ `Guid` ကို record ID တွေမှာ၊ `?`/`??` ကို optional values တွေမှာ၊ `async`/`await` ကို database နဲ့ network I/O မှာ တွဲဖတ်ပါ။ [C# async guide](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)
- **EF Core** — Read-only list ကို projection သို့မဟုတ် no-tracking နဲ့ ဖတ်တယ်။ ပြင်မယ့် entity ကို track လုပ်ပြီး save တယ်။ SQL operation အများကြီးကို အတူအောင်မြင်စေချင်ရင် transaction ကို စဉ်းစားတယ်။ [Tracking](https://learn.microsoft.com/en-us/ef/core/querying/tracking) · [Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions) · [Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- **DI နဲ့ options** — `IOptions<T>` က typed settings ဖတ်ဖို့၊ `DbContextOptions<T>` က database provider နဲ့ connection configure လုပ်ဖို့၊ `CreateChannelOptions` က RabbitMQ channel behavior သတ်မှတ်ဖို့ပါ။ Worker တစ်ခုက batch သို့မဟုတ် message တစ်ခုစီအတွက် scoped services ယူသုံးပုံကို PO Email နဲ့ Low Stock code မှာ ယှဉ်ဖတ်ပါ။ [Options](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) · [DbContext](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/) · [Worker scopes](https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service)
- **Messaging** — Publisher confirm က broker လက်ခံကြောင်း၊ Consumer ACK က delivery ကို ကိုင်တွယ်ပြီးကြောင်း ဖော်ပြတယ်။ ID တူတာ ထပ်ရောက်ရင် database ရလဒ် ထပ်မဖြစ်အောင် ဘယ် key သုံးထားလဲ စစ်ပါ။ [RabbitMQ .NET guide](https://www.rabbitmq.com/client-libraries/dotnet-api-guide) · [Confirms and ACK](https://www.rabbitmq.com/docs/confirms)
- **Angular** — HTTP response ရတာနဲ့ signal update လုပ်ပုံ၊ template က `@if`/`@for` နဲ့ ပြပုံ၊ component ပိတ်ချိန် request ကို ရှင်းပုံ လိုက်ကြည့်ပါ။ [HTTP](https://angular.dev/guide/http/making-requests) · [Signals](https://angular.dev/guide/signals) · [Template control flow](https://angular.dev/guide/templates/control-flow)

## နောက် feature အသစ်တိုင်းအတွက် ဖြည့်သုံးရန်

ဒီ template ကို feature note အသစ်ထဲ ကူးပြီး **code မရေးခင် အတိုချုံးဖြည့်ပါ**။ မသေချာသေးတဲ့ business rule ကို မေးခွန်းအဖြစ် မှတ်ထားပါ။

```markdown
# Feature အမည်

- User နဲ့ ရည်ရွယ်ချက်
- Trigger နဲ့ concrete input/output ဥပမာ
- Business rules၊ boundary values၊ ခွင့်ပြုထားတဲ့ status ပြောင်းလဲမှု
- Data ရင်းမြစ်၊ table/field၊ Company သို့မဟုတ် Supplier scope
- Query က ပြန်ပေးရမယ့် ပုံစံ၊ API request/response
- UI မှာ loading၊ empty၊ success၊ failure ဘယ်လိုပြမလဲ
- Request ပြီးမှ နောက်ကွယ်မှာ ဆက်လုပ်နိုင်တဲ့အပိုင်း
- Transaction အကန့်အသတ်၊ retry နဲ့ duplicate ဖြစ်ရင် ကိုင်တွယ်ပုံ
- Normal၊ boundary၊ unauthorized၊ failure/retry စမ်းသပ်ချက်များ
- လေ့လာရမယ့် syntax/method နဲ့ official docs
- Source ပိတ်ပြီး ကိုယ်တိုင်ပြန်ရေးမယ့် အပိုင်း
```

**Pattern ကို requirement ကနေ ရွေးပါ။**

| လိုအပ်ချက် | စတင်စဉ်းစားမယ့်ပုံစံ |
| --- | --- |
| လက်ရှိ data ပြ | SQL query → GET API → Angular |
| Stock/amount ကို ချက်ချင်းပြောင်းပြီး မှန်ကြောင်းပြ | Validate → transaction → commit → result |
| အချိန်ကြာတဲ့ အလုပ်ကို နောက်မှဆက်လုပ် | Durable job/outbox → worker → result/status။ Broker လိုမလို delivery နဲ့ worker ခွဲပုံအပေါ် ဆုံးဖြတ်ပါ။ |
| အလုပ်တစ်ခုကို handler အမျိုးမျိုး သီးခြားလုပ် | Event နဲ့ subscriber တစ်မျိုးစီရဲ့ queue၊ retry နဲ့ duplicate handling |
| Screen ဖွင့်ထားချိန် update လို | Refresh/polling နဲ့ စတင်စဉ်းစားပါ။ ချက်ချင်း update လိုတဲ့အခါ server push လိုမလို ကြည့်ပါ။ |
| Screen ပိတ်ထားချိန် device ဆီ အသိပေး | In-app notification record နဲ့ device push delivery ကို သီးခြား design လုပ်ပါ။ |
| တူညီတဲ့ expensive result ပြန်သုံး | Cache key မှာ company/filter ပါလား၊ TTL နဲ့ data ပြောင်းချိန် refresh ဘယ်လိုလုပ်မလဲ စဉ်းစားပါ။ |

**လက်တွေ့လုပ်တဲ့အစဉ်** — Concrete example တစ်ခုရွေး → ရှိပြီးသား ဆင်တူ code ကိုဖတ် → rule/query ကို ရေး → DB ကနေ UI အထိ လမ်းကြောင်းအသေးတစ်ခု ချိတ် → build နဲ့ development data သုံးပြီး စမ်း → အရေးကြီးတဲ့ method တစ်ခုကို source ပိတ်ပြီး ပြန်ရေးပါ။

**Debug လုပ်တဲ့အစဉ်** — ဘာမျှော်လင့်လဲနဲ့ တကယ်ဘာဖြစ်လဲ ရေးပါ။ Browser Network၊ API log၊ SQL row နဲ့ Queue state တွေထဲက သက်ဆိုင်တာကို request ID၊ record ID သို့မဟုတ် message ID တစ်ခုနဲ့ လိုက်ပါ။ ပြင်ထားတဲ့ source ကို လက်ရှိ process က သုံးနေလား စစ်ပြီး အခြေအနေတစ်ခုချင်း စမ်းပါ။

## တိုးတက်မှု မှတ်တမ်း

Feature တစ်ခုစီကို **Trace လုပ်နိုင် → အကြောင်းပြချက် ရှင်းနိုင် → ပြန်ရေးနိုင် → Requirement ပြောင်းပြီး ပြန်အသုံးချနိုင်** ဆိုတဲ့ အဆင့်တွေနဲ့ မှတ်ပါ။

| ရက်စွဲ | Feature / action | ကိုယ်တိုင်လုပ်ပြီးတာ | မရှင်းသေးတာ | နောက် session |
| --- | --- | --- | --- | --- |
| | | | | |
