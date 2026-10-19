# Purchase Return — Draft API ရှင်းလင်းချက်

ရေးသားသည့်နေ့: 2026-10-14။ Stack: C#/.NET၊ EF Core၊ SQL Server၊ MediatR၊ FluentValidation။

ဖတ်ရန်အစီအစဉ်: အပိုင်း ၁–၈ က feature/API/manual verification၊ ၉–၁၀ က entities၊
၁၁ က command၊ ၁၂ က validator၊ ၁၃ က create handler၊ ၁၄ က detail query၊
၁၅ က controller၊ ၁၆ က DbContext၊ ၁၇ က reconstruction မေးခွန်းများ ဖြစ်သည်။
Code ကိုကြည့်ရင်း section တစ်ခုစီရဲ့ရှင်းပြချက်ကို တွဲဖတ်ပါ။ ဒီစာတမ်းက လက်ရှိ
implementation ကိုရှင်းပြတာဖြစ်ပြီး code ကိုထပ်ထည့်ခိုင်းတဲ့ instruction မဟုတ်ပါ။

## ၁။ Feature ရဲ့တာဝန်နဲ့ လက်ရှိအခြေအနေ

Supplier တစ်ယောက်ဆီက PO/GRN အများကြီးမှာ လက်ခံထားတဲ့ပစ္စည်းတွေကို Return
တစ်စောင်ထဲ စုပြီးပြန်ပို့ဖို့ပါ။ Header မှာ SupplierId၊ item တစ်ကြောင်းစီမှာ
GoodReceiptItemId ရှိတယ်။ မူလ GRN line ကနေ Product/UOM/Warehouse/PO ကိုယူတယ်။

ဥပမာ GRN-001 က Rice 2 pcs၊ GRN-005 က Rice 3 pcs ဆိုရင် supplier ဆီ Rice
5 pcs ပို့ပေမယ့် database မှာ မူလ receipt line အလိုက် နှစ်ကြောင်းသိမ်းမယ်။
ပစ္စည်းတူတာနဲ့ receipt line တူတာ မတူပါဘူး။

လက်ရှိပြီးထားတာ: Draft create၊ ID အလိုက် detail read၊ request validation၊
company/supplier/receipt validation၊ returnable quantity စစ်ခြင်း၊ idempotency hash၊
transaction နဲ့ response mapping။ Draft က stock ကိုမပြောင်းဘူး။

2026-10-15 update: Posting/stock movement နဲ့ GRN guards ရေးပြီးဖြစ်သည်။ အပိုင်း ၁၉ ကိုကြည့်ပါ။

2026-10-19 update: Return list/filter/pagination backend ထည့်ပြီးဖြစ်သည်။ အပိုင်း ၂၀ ကိုကြည့်ပါ။

မပြီးသေးတာ: Return reversal၊
frontend၊ refund/accounting။ ဒီ Draft API ကို complete return workflow လို့မယူဆပါနဲ့။

## ၂။ Request ကနေ response အထိ flow

```text
HTTP POST
→ Authentication / CSRF
→ Controller: Mediator.Send(command)
→ ValidationBehavior → FluentValidation
→ CreatePurchaseReturnCommandHandler
→ Transaction
→ Existing key/hash စစ်
→ ReturnNo / Supplier / GRN / quantity စစ်
→ Header + Items SaveChangesAsync
→ GetPurchaseReturnByIdQuery နဲ့ DTO ဖတ်
→ Commit → 201 response
```

`record` က request data ကိုသယ်တယ်။ `IRequest<PurchaseReturnDto>` က response
type ကိုသတ်မှတ်တယ်။ `IRequestHandler<...>` က request ကိုလုပ်ဆောင်တဲ့ class ပါ။
Controller က business logic မရေးဘဲ command ကိုပို့ပြီး HTTP response ပြန်ပေးတယ်။
Validator/handler တွေကို ရှိပြီးသား assembly registration ကရှာပြီး DI ထဲထည့်တယ်။

## ၃။ API သုံးနည်း

```http
POST /api/process/purchase-returns
Content-Type: application/json
X-XSRF-TOKEN: <လက်ရှိ csrf request token>
```

```json
{
  "returnNo": "PR-2026-00000002",
  "supplierId": 1,
  "returnDate": "2026-10-14T00:00:00",
  "returnReason": "ပစ္စည်းအရည်အသွေး မပြည့်မီသောကြောင့်",
  "idempotencyKey": "e9b93c5b-7f20-4c26-b634-0c25aebdba11",
  "items": [
    { "goodReceiptItemId": 101, "returnQuantity": 2 },
    { "goodReceiptItemId": 205, "returnQuantity": 3 }
  ]
}
```

Supplier/item IDs က နမူနာသာဖြစ်တယ်။ ကိုယ့် company ရဲ့ လက်ခံပြီးသား GRN IDs
အမှန်ကိုသုံးပါ။ Login cookies နဲ့ CSRF token/cookie အတွဲ လိုတယ်။ Scalar မှာ
ရှိပြီးသား CSRF integration ကိုသုံးနိုင်တယ်။ CompanyId၊ Status၊ PostedBy/On၊
request hash နဲ့ TotalAmount ကို request မှာမပို့ရဘူး။

Response က existing DefaultResponseModel ပုံစံဖြစ်ပြီး `data` ထဲ Return DTO ပါတယ်။
Create နဲ့ တူညီသော retry နှစ်ခုလုံးက လက်ရှိ implementation မှာ 201 ပြန်ပေးတယ်။
Retry ဆို document အသစ်မဖန်တီးဘဲ မူလ ID ပြန်ပေးတယ်။ `Location` header က
အောက်က detail endpoint ကိုညွှန်တယ်။

```http
GET /api/process/purchase-returns/{id}
```

Detail က current company ထဲ မဖျက်ထားတဲ့ Return ကိုသာဖတ်တယ်။ SuperAdmin
ဖြစ်လည်း ဒီ endpoint က current company ကို explicit စစ်တယ်။ မတွေ့ရင် 404။
Create/read နှစ်ခုလုံး `[Authorize]` ရှိပြီး dedicated role restriction မထည့်ရသေးဘူး။

## ၄။ Rules နဲ့ syntax

Validator: SupplierId positive၊ ReturnDate required၊
Reason max 500၊ key nonempty၊ items nonempty/non-null၊ duplicate GRN line မပါရ၊
quantity positive နဲ့ decimal(18,4) အတွင်းရှိရ။ Date မှာ SQL range/future-date
business rule ထပ်မစစ်ထားဘူး။ DB mapping က datetime ဖြစ်တာကြောင့် အလွန်ဟောင်းတဲ့
date တချို့ကို database ကလက်မခံနိုင်သေးတဲ့ limitation ရှိတယ်။

`RuleFor` က property တစ်ခု၊ `RuleForEach` က item တစ်ကြောင်းစီကိုစစ်တယ်။
`CascadeMode.Stop` က အဲဒီ rule chain မှာ fail ရင် နောက်စစ်ချက်မဆက်စေဘူး။
`Must` က true/false ပြန်တဲ့ ကိုယ်ပိုင် condition ဖြစ်တယ်။

Handler က supplier ရှိပြီး company တူ၊ မဖျက်ထားကြောင်းစစ်တယ်။ Receipt line
နဲ့ GRN header နှစ်ခုလုံး company တူ၊ မဖျက်ထားဘဲ GRN Status=true ဖြစ်ရမယ်။
GRN supplier က request supplier နဲ့တူရမယ်။ FK တစ်ခုတည်းက ဒီ rules မစစ်ပေးဘူး။

```csharp
var returnableQuantity = receiptItem.ReceivedQuantity - alreadyReturned;
```

10 pcs လက်ခံ၊ Posted returns 3 pcs ဆိုရင် ကျန် 7 pcs။ Draft returns မနုတ်ဘူး၊
stock reserve မလုပ်ဘူး။ `GroupBy` က GRN line ID တူတာစု၊ `Sum` က quantity ပေါင်း၊
`ToDictionaryAsync` က ID → quantity lookup ဖန်တီးတယ်။ `TryGetValue` မတွေ့ရင်
decimal output default 0 ဖြစ်တယ်။ Posting မှာ quantity နဲ့ stock ကို ပြန်စစ်ရမယ်။

## ၅။ Idempotency နဲ့ concurrency

Key တူ/data တူ → မူလ Return ပြန်ပေး။ Key တူ/data မတူ → 400။ Deleted Return
ရဲ့ key ကိုပြန်သုံး → 400။ Request hash ကို original create မှာသိမ်းပြီး နောင်
edit လုပ်လည်း ပြန်မပြောင်းသင့်ဘူး။

Hash payload မှာ strings Trim၊ items ID နဲ့ OrderBy၊ quantity G29၊ date O နဲ့
InvariantCulture သုံးတယ်။ JSON UTF-8 bytes ကို SHA256 တွက်ပြီး hex string
64 characters အဖြစ်သိမ်းတယ်။ ဒါက password encryption မဟုတ်ဘဲ payload comparison ပါ။

Custom SQL application lock မသုံးတော့ဘူး။ ရှိပြီးသား BeginTransactionAsync ရဲ့
default Serializable isolation နဲ့ database unique indexes ကိုသုံးထားတယ်။
ပြိုင်တူ create requests မှာ duplicate documents ကို index ကပိတ်ပေးတယ်၊ ဒါပေမယ့်
deadlock/unique conflict ဖြစ်နိုင်ပြီး အဲဒီ error အတွက် automatic retry မထည့်ရသေးဘူး။
ထို့ကြောင့် concurrent request နှစ်ခုလုံး successful response ရမယ်လို့ မအာမခံဘူး။

`SaveChangesAsync` က transaction အတွင်း SQL ရေးတယ်။ `CommitAsync` ပြီးမှ
transaction အတည်ဖြစ်တယ်။ `await using` ကြောင့် commit မဖြစ်ဘဲ exception ထွက်ရင်
dispose လုပ်ပြီး rollback ဖြစ်တယ်။ DB unique indexes က နောက်ဆုံးကာကွယ်မှုအဖြစ်
ဆက်ရှိတယ်။ Deleted Return ရဲ့ number ကိုလည်း index အတိုင်း ပြန်မသုံးရဘူး။

## ၆။ Response mapping ကိုတစ်နေရာတည်းထားခြင်း

`GetPurchaseReturnByIdQueryHandler` ထဲမှာ mapping တိုက်ရိုက်ထားတယ်။ Create နဲ့ retry
က Mediator ကတစ်ဆင့် ဒီ query ကိုပြန်ခေါ်တယ်။ သီးခြား reader class မသုံးဘူး။
Header၊ source GRN/PO၊ product/UOM/warehouse နဲ့ audit fields ဖြည့်ပေးတယ်။
PostedBy ရှိရင် Identity service က username ယူပေးတယ်။ Draft မှာ posting fields null။

`AsNoTracking` က read-only query အတွက် tracking မလုပ်စေဘူး။ `Select` က DTO
projection လုပ်ပြီး `SingleOrDefaultAsync` က query execute လုပ်တယ်။ Projection
အတွင်း Items.ToList() က nested collection materialization ဖြစ်ပြီး သီးခြား
synchronous database call မဟုတ်ဘူး။ Field ထပ်ထည့်ရင် reader တစ်နေရာမှာပြင်နိုင်တယ်။

## ၇။ Manual စမ်းသပ်နည်း

လက်ရှိ docs ရေးသည့်အချိန် automated tests မဖန်တီးထားဘူး။ API/database manual
verification ကို လုပ်ပြီးသားလို့မယူဆပါနဲ့။ ကိုယ့် test data နဲ့ အောက်က cases စမ်းပါ။

| Case | မျှော်လင့်ချက် |
|---|---|
| Valid receipt lines, supplier/company တူ | 201၊ Draft header/items သိမ်း၊ stock မပြောင်း |
| Same key + same body retry | 201၊ ID တူ၊ header/items ထပ်မတိုး |
| Same key + quantity ပြောင်း | 400၊ record အသစ်မတိုး |
| Different keys နဲ့ valid create နှစ်ခု | နံပါတ်အသစ် သီးခြားနှစ်ခုရရမယ် |
| Same request နှစ်ခုတစ်ပြိုင်နက် | Document မထပ်ရ၊ DB conflict ဖြစ်ရင် same key/body နဲ့ retry လုပ်ပြီး မူလ ID ရရမယ် |
| Supplier/company မတူသော GRN line | 400 |
| Draft/deleted GRN သို့ deleted item | 400 |
| Received 10၊ prior Posted 3၊ request 8 | 400 |
| Empty items / duplicate IDs / quantity 0 | 422 validation error |
| Login မရှိ | Auth configuration အရ unauthorized |
| CSRF header/cookie မရှိ | Antiforgery rejection |
| GET valid ID | 200၊ names နဲ့ source references ပြည့်စုံ |
| GET missing/deleted/other-company ID | 404 |

Business failures အများစုကို လက်ရှိ handler က InvalidOperationException ပစ်လို့
400 ဖြစ်တယ်။ Validator failures က existing middleware အရ 422 ဖြစ်တယ်။

## ၈။ နောက်အဆင့်နဲ့ reconstruction

Posting မှာ source receipt ပြန်စစ်၊ remaining returnable ပြန်တွက်၊ warehouse stock
စစ်၊ linked stock movement ဖန်တီး၊ Status/PostedBy/PostedOn ပြောင်းပြီး transaction
တစ်ခုထဲ commit လုပ်ဖို့လိုတယ်။ Repeated posting နဲ့ competing returns ကို သီးခြား
ကာကွယ်ရမယ်။ Posted records တိုက်ရိုက် edit/delete မပေးဘဲ reversal design လိုတယ်။

လေ့ကျင့်ရန်: source မကြည့်ဘဲ received 10/prior returned 3/requested 8 နမူနာရဲ့
quantity rule ကိုပြန်ရေးပါ။ Key တူ/data တူ နဲ့ key တူ/data မတူ flow ကို မြန်မာလို
ပြန်ရှင်းပါ။ Implementation ရှိတာကို တစ်ယောက်တည်းနားလည်ရေးနိုင်ပြီလို့ မမှတ်ယူပါနဲ့။

## ၉။ Entity code — database မှာသိမ်းမယ့် document

Source: `src/InventoryManagementSystem.Domain/Entities/PurchaseReturn.cs`။ အောက်က code သည် 2026-10-14 source snapshot ဖြစ်သည်။ နောက် code ပြောင်းလျှင် ဒီစာမျက်နှာလည်း ပြန်ညှိရန်လိုသည်။

```csharp
using InventoryManagementSystem.Domain.Common;
using InventoryManagementSystem.Domain.Enums;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public class PurchaseReturn : BaseAuditableEntity<Guid>, IMustHaveCompany 
{ 
    public int? CompanyId { get; set; }
    public int SupplierId { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public PurchaseReturnStatus Status { get; set; } = PurchaseReturnStatus.Draft;
    public decimal TotalAmount { get; set; }
    public string? ReturnReason { get; set; }
    public DateTime? PostedOn { get; set; }
    public string? PostedBy { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public string? IdempotencyRequestHash { get; set; }
    [JsonIgnore]
    public virtual Supplier? Supplier { get; set; }
    [JsonIgnore]
    public virtual ICollection<PurchaseReturnItem> Items { get; set; } = new List<PurchaseReturnItem>();
}
```

`class PurchaseReturn : BaseAuditableEntity<Guid>, IMustHaveCompany` မှာ `:` က inheritance/interface implementation ပါ။ Base class က Guid Id နဲ့ Created/Updated/Deleted audit fields ပေးတယ်။ Interface က CompanyId property ရှိရမယ်ဆိုတဲ့ contract ပါ။ Interface ကို implement လုပ်ရုံနဲ့ company filter အလိုအလျောက် မဖြစ်ဘူး၊ DbContext မှာ filter ရေးထားရတယ်။

`int?` / `DateTime?` / `Guid?` က value type ကို null ခွင့်ပြုတာပါ။ `string?` က nullable reference annotation ပါ။ `get; set;` က property ဖတ်/ပြင်နိုင်တယ်။ Status က public setter ဖြစ်လို့ posted immutability ကို domain က အခုအချိန်မှာ မကာကွယ်သေးဘူး။ နောင် posting/edit handlers မှာ rule ထိန်းဖို့လိုတယ်။

`= string.Empty` က string အစတန်ဖိုး အလွတ်ပေးတာ၊ required validation လုပ်ပေးတာမဟုတ်ဘူး။ `= PurchaseReturnStatus.Draft` က entity အသစ်ရဲ့ initial status ပါ။ Enum က Draft=0/Posted=1 ဖြစ်ပြီး DB မှာ int သိမ်းတယ်။ `TotalAmount` ကို model မှာထားပေမယ့် ဒီ handler က မတွက်၊ response မှာလည်း မပြသေးဘူး။ လက်ရှိ default 0 ဖြစ်မယ်။

`SupplierId` က FK value၊ `Supplier` က navigation property ပါ။ `virtual` ရှိရုံနဲ့ related entity အလိုအလျောက် load မဖြစ်ဘူး။ `[JsonIgnore]` က JSON serialization မှာ navigation မထုတ်ဖို့ဖြစ်ပြီး database FK ကိုဖယ်တာမဟုတ်ဘူး။

`ICollection<PurchaseReturnItem>` က item collection contract ဖြစ်ပြီး `new List<...>()` က အစကတည်းက null မဟုတ်တဲ့ list ဖန်တီးပေးတယ်။ Header မှာ GRN ID မရှိတာက GRN အများကြီးထည့်နိုင်ဖို့ပါ။

## ၁၀။ Item entity — source receipt line ကိုခြေရာခံခြင်း

Source: `src/InventoryManagementSystem.Domain/Entities/PurchaseReturnItem.cs`။ အောက်က code သည် 2026-10-14 source snapshot ဖြစ်သည်။ နောက် code ပြောင်းလျှင် ဒီစာမျက်နှာလည်း ပြန်ညှိရန်လိုသည်။

```csharp
using InventoryManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace InventoryManagementSystem.Domain.Entities;

public class PurchaseReturnItem : BaseAuditableEntity<long>, IMustHaveCompany
{
    public int? CompanyId { get; set; }
    public Guid PurchaseReturnId { get; set; }
    public long GoodReceiptItemId { get; set; }
    public decimal ReturnQuantity { get; set; } = 0;
    [JsonIgnore]
    public virtual PurchaseReturn? PurchaseReturn { get; set; }
    [JsonIgnore]
    public virtual GoodReceiptItem? GoodReceiptItem { get; set; }
}
```

Item Id က `long` ဖြစ်ပြီး migration မှာ SQL identity column ဖြစ်တယ်။ `PurchaseReturnId` က header ကိုချိတ်ပြီး `GoodReceiptItemId` က မူလ receipt line ကိုချိတ်တယ်။ ProductId တစ်ခုတည်းနဲ့ ချိတ်မထားတာက product တူတဲ့ receipt lines ကိုခွဲသိဖို့ပါ။

`decimal` က quantity decimal arithmetic အတွက်သုံးတယ်။ `= 0` က default ဖြစ်ပြီး zero quantity ကိုခွင့်ပြုတာမဟုတ်ဘူး၊ validator ကပိတ်တယ်။ Header/item company နှစ်ခုလုံးသတ်မှတ်ထားတာက တစ်ခုစီ query လုပ်တဲ့အခါ company scope ရှိဖို့ပါ။ Navigation nullable ဖြစ်ပေမယ့် FK fields တွေ non-nullable ဖြစ်တဲ့အတွက် relationship က DB မှာ required ဖြစ်တယ်။

## ၁၁။ Command code — client ကပို့တဲ့ input

Source: `src/InventoryManagementSystem.Application/Process/PurchaseReturns/Commands/CreatePurchaseReturnDraft/CreatePurchaseReturnCommand.cs`။ အောက်က code သည် 2026-10-14 source snapshot ဖြစ်သည်။ နောက် code ပြောင်းလျှင် ဒီစာမျက်နှာလည်း ပြန်ညှိရန်လိုသည်။

```csharp
ï»¿using MediatR;
using System;
using System.Collections.Generic;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;

public record CreatePurchaseReturnItemInput(
    long GoodReceiptItemId,
    decimal ReturnQuantity
);

public record CreatePurchaseReturnCommand(
    string ReturnNo,
    int SupplierId,
    DateTime ReturnDate,
    string? ReturnReason,
    Guid IdempotencyKey,
    List<CreatePurchaseReturnItemInput> Items
) : IRequest<PurchaseReturnDto>;
```

`using` က namespace ထဲက type ကို အမည်တိုနဲ့သုံးနိုင်စေတယ်။ Service ဖန်တီးပေးတာမဟုတ်ဘူး။ `namespace ...;` က file-scoped namespace syntax ပါ။

`record Name(...)` က positional record syntax ဖြစ်ပြီး constructor နဲ့ properties ရလာတယ်။ Request data သယ်ဖို့သုံးထားတာ၊ entity/database table မဟုတ်ဘူး။ `List<CreatePurchaseReturnItemInput>` က line အများကြီးကို request တစ်ခုထဲမှာသယ်တယ်။ Record ဖြစ်တာနဲ့ အတွင်း List က immutable ဖြစ်မသွားဘူး။

`: IRequest<PurchaseReturnDto>` ရဲ့ generic argument က result type ပါ။ MediatR က ဒီ command type ကို လက်ခံတဲ့ handler ကိုရှာမယ်။ Command ရှိရုံနဲ့ DB မသိမ်းဘူး။ `CompanyId` ကို user context ကယူပြီး Product/UOM/Warehouse ကို source receipt ကယူတယ်။ Client ကို ထပ်ပို့ခိုင်းစရာမလိုဘူး။

## ၁၂။ Validator code — input ပုံစံနဲ့ value စစ်ခြင်း

Source: `src/InventoryManagementSystem.Application/Process/PurchaseReturns/Commands/CreatePurchaseReturnDraft/CreatePurchaseReturnCommandValidator.cs`။ အောက်က code သည် 2026-10-14 source snapshot ဖြစ်သည်။ နောက် code ပြောင်းလျှင် ဒီစာမျက်နှာလည်း ပြန်ညှိရန်လိုသည်။

```csharp
using FluentValidation;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;

public class CreatePurchaseReturnCommandValidator : AbstractValidator<CreatePurchaseReturnCommand>
{
    public CreatePurchaseReturnCommandValidator()
    {
        RuleFor(v => v.ReturnNo)
            .NotEmpty().WithMessage("Return Number is required.")
            .MaximumLength(50).WithMessage("Return Number cannot exceed 50 characters.");

        RuleFor(v => v.SupplierId)
            .GreaterThan(0).WithMessage("A valid Supplier must be selected.");

        RuleFor(v => v.ReturnDate)
            .NotEmpty().WithMessage("Return Date is required.");

        RuleFor(v => v.ReturnReason)
            .MaximumLength(500).WithMessage("Return Reason cannot exceed 500 characters.");

        RuleFor(v => v.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.");

        RuleFor(v => v.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("At least one return item is required.")
            .Must(items => items.Where(item => item is not null)
                .Select(item => item.GoodReceiptItemId).Distinct().Count()
                == items.Count(item => item is not null))
            .WithMessage("Each goods receipt item can appear only once in a return.");

        RuleForEach(v => v.Items)
            .NotNull().WithMessage("Return items cannot be null.")
            .SetValidator(new CreatePurchaseReturnItemInputValidator());
    }
}

public class CreatePurchaseReturnItemInputValidator : AbstractValidator<CreatePurchaseReturnItemInput>
{
    public CreatePurchaseReturnItemInputValidator()
    {
        RuleFor(v => v.GoodReceiptItemId)
            .GreaterThan(0).WithMessage("A valid Goods Receipt Item must be selected.");

        RuleFor(v => v.ReturnQuantity)
            .GreaterThan(0).WithMessage("Return Quantity must be greater than 0.")
            .PrecisionScale(18, 4, true)
            .WithMessage("Return Quantity must fit decimal(18,4), with at most 4 decimal places.");
    }
}
```

`AbstractValidator<CreatePurchaseReturnCommand>` ကို inherit လုပ်ပြီး constructor ထဲမှာ rules register လုပ်တယ်။ `v => v.SupplierId` က lambda ဖြစ်ပြီး v ရဲ့ property ကိုရွေးတာပါ။ Method တွေကို `.` နဲ့ဆက်ရေးတာ fluent API chain ဖြစ်တယ်။ `WithMessage` က သူ့ရှေ့က validator fail တဲ့ message ပါ။

`NotEmpty` က Items မှာ null/empty list၊ Guid မှာ Guid.Empty၊ DateTime မှာ default value ကိုပိတ်တယ်။ Date ရဲ့ default က 0001-01-01 ပါ။ Date business rule အားလုံးမှန်ကြောင်း မအာမခံဘူး။ Optional ReturnReason မှာ max length ပဲစစ်ထားလို့ null ခွင့်ရှိတယ်။

Items rule မှာ `Cascade(CascadeMode.Stop)` က အဲဒီ property rule chain ထဲမှာ fail ဖြစ်ရင် ဆက်မစစ်စေဘူး။ Null list ကို `.Where` ဖတ်မိတာကာကွယ်တယ်။ တခြား rules အားလုံးရပ်တာမဟုတ်ဘူး။

`Must(items => ...)` က bool result လိုတဲ့ custom condition ပါ။ `Where(item => item is not null)` က null element ဖယ်၊ `Select` က receipt IDs ထုတ်၊ `Distinct` က IDs ထပ်တာဖယ်၊ `Count` က အရေအတွက်တွက်တယ်။ `[101,102,101]` ဆို left count=2/right count=3 ဖြစ်ပြီး fail။ `[101,102]` ဆို 2=2 ဖြစ်ပြီး pass။ ဒီ calculation မှာ null ကိုဖယ်ထားတာက exception ရှောင်ဖို့၊ null item ကိုလက်ခံတာမဟုတ်ဘူး။

`RuleForEach` က item တစ်ကြောင်းစီကိုစစ်တယ်။ `[null]` က nonempty list ဖြစ်လို့ list NotEmpty နဲ့မလုံလောက်ဘူး။ Item NotNull က အဲဒီအခြေအနေကိုပိတ်ပြီး `SetValidator` က non-null item ကို child validator နဲ့စစ်တယ်။

`PrecisionScale(18,4,true)` က decimal(18,4) သိမ်းနိုင်တဲ့ precision/scale စစ်တာ။ 2.1250 pass၊ 2.12501 fail။ `true` က trailing zeros ကိုစစ်ရာမှာလျစ်လျူရှုတယ်။ DB lookup ကို validator မှာမလုပ်ထားဘူး။ Existing ValidationBehavior က handler မဝင်ခင် validators run ပြီး fail ရင် ValidationException ပစ်တယ်။ Middleware က 422 ပြန်ပေးတယ်။

## ၁၃။ Create handler code — block တစ်ခုစီ၏တာဝန်

Source: `src/InventoryManagementSystem.Application/Process/PurchaseReturns/Commands/CreatePurchaseReturnDraft/CreatePurchaseReturnCommandHandler.cs`။ အောက်က code သည် 2026-10-14 source snapshot ဖြစ်သည်။ နောက် code ပြောင်းလျှင် ဒီစာမျက်နှာလည်း ပြန်ညှိရန်လိုသည်။

```csharp
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnDraft;

public class CreatePurchaseReturnCommandHandler : IRequestHandler<CreatePurchaseReturnCommand, PurchaseReturnDto> 
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;
    public CreatePurchaseReturnCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<PurchaseReturnDto> Handle(CreatePurchaseReturnCommand request, CancellationToken cancellationToken)
    {
        int? companyId = _currentUserService.CompanyId;
        string requestHash = CalculateRequestHash(request);
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);


        PurchaseReturn? existingReturn = await _context.PurchaseReturns
            .AsNoTracking()
            .SingleOrDefaultAsync(pr => pr.IdempotencyKey == request.IdempotencyKey && pr.CompanyId == companyId, cancellationToken);

        if (existingReturn != null)
        {
            if (existingReturn.DeletedOn.HasValue)
            {
                throw new InvalidOperationException(
                    "The return created with this key has been deleted.");
            }

            if (!string.Equals(
                existingReturn.IdempotencyRequestHash,
                requestHash,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "This idempotency key was already used for a different request.");
            }

            var existingResponse = await _sender.Send(new GetPurchaseReturnByIdQuery(existingReturn.Id), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return existingResponse;
        }

        var returnNo = request.ReturnNo.Trim();
        if (await _context.PurchaseReturns.AnyAsync(pr => pr.CompanyId == companyId
            && pr.ReturnNo == returnNo, cancellationToken))
            throw new InvalidOperationException("Return number is already in use. Reopen Create Return to get a new number.");

        bool supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == request.SupplierId && s.CompanyId == companyId && !s.DeletedOn.HasValue, cancellationToken);
        if(supplierExists == false)
        {
            throw new InvalidOperationException($"Supplier with ID {request.SupplierId} does not exist.");
        }

        List<long> receiptItemIds = request.Items.Select(i => i.GoodReceiptItemId).ToList();

        List<GoodReceiptItem> receiptItems = await _context.GoodReceiptItems
            .AsNoTracking()
            .Include(item => item.GoodReceipt)
            .Where(item => receiptItemIds.Contains(item.Id) && item.CompanyId == companyId && !item.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);

        if(receiptItems.Count != receiptItemIds.Count)
        {
            throw new InvalidOperationException("One or more Good Receipt Items do not exist.");
        }

        Dictionary<long, decimal> returnedQuantities = await _context.PurchaseReturnItems
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId
            && receiptItemIds.Contains(i.GoodReceiptItemId)
            && !i.DeletedOn.HasValue
            && i.PurchaseReturn!.CompanyId == companyId
            && !i.PurchaseReturn.DeletedOn.HasValue
            && i.PurchaseReturn.Status == PurchaseReturnStatus.Posted)
            .GroupBy(i => i.GoodReceiptItemId)
             .Select(group => new
             {
                 ReceiptItemId = group.Key,
                 Quantity = group.Sum(item => item.ReturnQuantity)
             }).ToDictionaryAsync(item => item.ReceiptItemId,item => item.Quantity,cancellationToken);

        foreach (var receiptItem in receiptItems)
        {
            var receipt = receiptItem.GoodReceipt;

            if (receipt.CompanyId != companyId
                || receipt.DeletedOn.HasValue
                || !receipt.Status)
            {
                throw new InvalidOperationException(
                    "Only active, received goods receipts can be returned.");
            }

            if (receipt.SupplierId != request.SupplierId)
            {
                throw new InvalidOperationException(
                    "All receipt items must belong to the selected supplier.");
            }

            returnedQuantities.TryGetValue(receiptItem.Id, out var alreadyReturned);

            decimal returnableQuantity = receiptItem.ReceivedQuantity - alreadyReturned;

            CreatePurchaseReturnItemInput requestedItem = request.Items.Single(item => item.GoodReceiptItemId == receiptItem.Id);

            if (requestedItem.ReturnQuantity > returnableQuantity)
            {
                throw new InvalidOperationException(
                    $"Return quantity exceeds the remaining quantity for receipt item {receiptItem.Id}.");
            }
        }

        PurchaseReturn purchaseReturn = new PurchaseReturn
        {
            ReturnNo = returnNo,
            SupplierId = request.SupplierId,
            ReturnDate = request.ReturnDate,
            ReturnReason = request.ReturnReason?.Trim(),
            IdempotencyKey = request.IdempotencyKey,
            IdempotencyRequestHash = requestHash,
            Status = PurchaseReturnStatus.Draft,
            CompanyId = companyId
        };
        foreach (var item in request.Items)
        {   
            purchaseReturn.Items.Add(new PurchaseReturnItem
            {
                GoodReceiptItemId = item.GoodReceiptItemId,
                ReturnQuantity = item.ReturnQuantity,
                CompanyId = companyId
            });
        }
        _context.PurchaseReturns.Add(purchaseReturn);
        await _context.SaveChangesAsync(cancellationToken);
        var response = await _sender.Send(new GetPurchaseReturnByIdQuery(purchaseReturn.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    private static string CalculateRequestHash(CreatePurchaseReturnCommand command)
    {
        var payload = new
        {
            ReturnNo = command.ReturnNo.Trim(),
            command.SupplierId,
            ReturnDate = command.ReturnDate.ToString("O",CultureInfo.InvariantCulture),
            ReturnReason = command.ReturnReason?.Trim(),
            Items = command.Items.OrderBy(i => i.GoodReceiptItemId).Select(i => new { i.GoodReceiptItemId,
                ReturnQuantity = i.ReturnQuantity.ToString(
                    "G29", CultureInfo.InvariantCulture)
            }).ToArray()
        };
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(payload)));
    }
}
```

**Dependencies / method signature**

`private readonly` က field ကို constructor မှာပေးပြီး နောက်ထပ် reference ပြန်မသတ်မှတ်ဖို့ပါ။ Object ကို immutable ဖြစ်စေခြင်းမဟုတ်ဘူး။ Constructor parameters ကို DI ကပေးတယ်။ `_context` က DB access၊ `_currentUserService` က current company၊ `_sender` က existing detail query ကိုပို့ဖို့ပါ။

`async Task<PurchaseReturnDto>` က await လုပ်ပြီး DTO result ပြန်မယ်။ `CancellationToken` ကို DB/MediatR calls ဆီဆက်ပို့ထားလို့ request cancellation ကိုလိုက်နာနိုင်တယ်။ Async က code ကို new thread တစ်ခုမှာအလိုအလျောက် run စေတာမဟုတ်ဘူး။ `var` နဲ့ explicit type က compile-time type ရှိတာတူပြီး `var` က compiler ကို infer လုပ်ခိုင်းတာပါ။

**Transaction / idempotency lookup**

`await using var transaction` က async disposal လုပ်ပေးတယ်။ Shared BeginTransactionAsync က default Serializable isolation သုံးတယ်။ Custom SQL lock မသုံးတော့ဘူး။ `AsNoTracking` က read-only lookup ဖြစ်လို့ EF tracking မလုပ်စေဘူး။ `SingleOrDefaultAsync` က row တစ်ခု သို့မဟုတ် null ပြန်ပေးပြီး row အများကြီးဆို exception ဖြစ်မယ်။ `&&` က conditions နှစ်ခုလုံး true ဖြစ်ရမယ်။ Company+key သည် unique index scope နဲ့ကိုက်တယ်။

Existing Return တွေ့မှ deleted/hash စစ်တယ်။ မတွေ့သေးတဲ့ request မှာ hash compare မလုပ်ဘူး။ `string.Equals(..., StringComparison.Ordinal)` က case/culture rules မသုံးဘဲ exact string နှိုင်းတယ်။ Different hash ဆို key ကို data အသစ်အတွက်ပြန်သုံးနေတာဖြစ်လို့ပိတ်တယ်။ Matching hash ဆို query ပြန်ခေါ်ပြီး existing result ပြန်ပေးတယ်။ ဒီ retry branch မှာ Add/Save မလုပ်ဘူး။

**Number / supplier / receipt lookup**

`Trim()` က အစ/အဆုံး whitespace ဖယ်တယ်။ `AnyAsync` က record ရှိမရှိ bool ပြန်တယ်။ ReturnNo ကို frontend ကထုတ်ပြီး backend က duplicate စစ်သည်။ Supplier lookup က active/company တူကြောင်းစစ်တယ်။

`Select(i => i.GoodReceiptItemId).ToList()` က request ရဲ့ memory list ကနေ IDs ထုတ်တာဖြစ်လို့ ဒီ ToList က database call မဟုတ်ဘူး။ Receipt query မှာ `Contains(item.Id)` က ID အစုထဲပါသလားစစ်တယ်။ EF က SQL query အဖြစ် translate လုပ်တယ်။ `Include(item => item.GoodReceipt)` က header data ကိုယူပေးတယ်။ `ToListAsync` မှ query execute ဖြစ်ပြီး results memory ထဲရောက်တယ်။ မတိုင်ခင် query building ဖြစ်တယ်။ Requested count နဲ့ fetched count မတူရင် မတွေ့တဲ့/inaccessible/deleted ID ရှိနိုင်တယ်။ Duplicate IDs မပါကြောင်း validator အပေါ် ဒီ check ကမှီတယ်။

**အရင်ပြန်ပို့ပြီး quantity တွက်ခြင်း**

Posted header နဲ့ active lines ပဲရွေးပြီး `GroupBy` က receipt ID တူတဲ့ lines စုတယ်။ `group.Key` က group ID၊ `Sum` က return quantities ပေါင်းတယ်။ `ToDictionaryAsync(keySelector,valueSelector,token)` က ID → decimal lookup ဖန်တီးတယ်။ `i.PurchaseReturn!` ရဲ့ ! က compiler nullability warning ကို suppress လုပ်တာ၊ runtime null guard မဟုတ်ဘူး။ Relationship ရှိကြောင်း schema ကထောက်ပံ့ထားတယ်။

`foreach` ထဲမှာ header company/deleted/received စစ်တယ်။ `||` က condition တစ်ခုခု true ဆိုပိတ်မယ်။ `!receipt.Status` က false ဖြစ်သော receipt ကိုပိတ်တာပါ။ Supplier ကိုလည်းတူရမယ်။ `TryGetValue(id,out var alreadyReturned)` မတွေ့ရင် out decimal default 0 ဖြစ်တယ်။ `ReceivedQuantity - alreadyReturned` က returnable ဖြစ်တယ်။ `request.Items.Single(...)` က memory list ထဲက matching input တစ်ခုကိုရှာတယ်။ မတွေ့/တစ်ခုထက်ပိုဆို error ဖြစ်လို့ validator + earlier ID checks လိုတယ်။ Draft quantity check က stock check မဟုတ်ဘူး။

**Entity တည်ဆောက်ခြင်း / Save**

`new PurchaseReturn { ... }` က object initializer ဖြစ်တယ်။ ReturnDate သည် request date၊ audit date သည် shared SaveChanges code ရဲ့ DateTime.Now ဖြစ်တယ်။ `?.Trim()` က optional reason null ဆို null ပဲထားတယ်။ Draft မှာ PostedBy/On မပေးလို့ null ဖြစ်တယ်။

`purchaseReturn.Items.Add(...)` က header navigation collection ထဲထည့်တာ။ EF က header/item graph ကို Add လုပ်တဲ့အခါ FK relationship ချိတ်ပေးတယ်။ Item ကိုသီးခြား DbSet Add ပြီး FK မပေးထားရင် Guid.Empty FK ပြဿနာဖြစ်နိုင်တယ်။ Header Guid generation/item identity generation ကို EF/database mapping ကလုပ်တယ်။

`_context.PurchaseReturns.Add` က tracking state ကို Added သတ်မှတ်တာ၊ SQL insert မဖြစ်သေးဘူး။ `SaveChangesAsync` မှ SQL ရေးပြီး generated IDs/audit values ရလာတယ်။ Response query က transaction တစ်ခုထဲမှာ အဲဒီ data ကိုမြင်နိုင်တယ်။ `CommitAsync` ပြီးမှ transaction အတည်ဖြစ်တယ်။ Save နောက် response query fail ဖြစ်လည်း commit မဖြစ်သေးလို့ rollback ဖြစ်နိုင်တယ်။ Unique conflicts/deadlocks အတွက် automatic retry မရှိသေးဘူး။

**Hash helper**

`private static` က class အတွင်းသုံးပြီး instance fields မလိုတဲ့ method ဖြစ်တယ်။ `new { ... }` က anonymous object၊ `command.SupplierId` လိုရေးတာက property name ကို infer လုပ်တယ်။ `OrderBy` က item order ပြောင်းပို့လည်း canonical payload တူဖို့၊ `Select` က hash ထဲလိုတဲ့ fields ပဲယူဖို့၊ `ToArray` က JSON serialize လုပ်မယ့် data ကို materialize လုပ်ဖို့ပါ။

Date format O က round-trip representation၊ G29 က decimal trailing zeros မတူခြင်းကို normalize လုပ်ဖို့၊ InvariantCulture က machine locale ကြောင့် representation မပြောင်းဖို့ပါ။ ဒီ handler က timezone ပြောင်း/UTC ပြောင်းမလုပ်ဘူး။ ReturnDate က time/date-kind representation မတူလာရင် hash ကွာနိုင်လို့ retry မှာ မူလpayload ကိုပြန်ပို့ပါ။

JsonSerializer → UTF-8 bytes → SHA256 → hex string ဖြစ်တယ်။ Key က identify၊ hash က payload တူမတူစစ်ဖို့ပါ။ Company သည် lookup scope ထဲရှိပြီး key သည် lookup identifier ဖြစ်လို့ payload hash ထဲထပ်မထည့်ထားဘူး။

## ၁၄။ Detail query code — DTO ဖြည့်ခြင်း

Source: `src/InventoryManagementSystem.Application/Process/PurchaseReturns/Queries/GetPurchaseReturnById/GetPurchaseReturnByIdQuery.cs`။ အောက်က code သည် 2026-10-14 source snapshot ဖြစ်သည်။ နောက် code ပြောင်းလျှင် ဒီစာမျက်နှာလည်း ပြန်ညှိရန်လိုသည်။

```csharp
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using MediatR;
using InventoryManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;

public class GetPurchaseReturnByIdQueryHandler : IRequestHandler<GetPurchaseReturnByIdQuery, PurchaseReturnDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetPurchaseReturnByIdQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<PurchaseReturnDto> Handle(GetPurchaseReturnByIdQuery request, CancellationToken cancellationToken)
    {
        int? companyId = _currentUserService.CompanyId;
        PurchaseReturnDto? result = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(pr => pr.Id == request.Id && pr.CompanyId == companyId && !pr.DeletedOn.HasValue)
            .Select(pr => new PurchaseReturnDto
            {
                Id = pr.Id,
                ReturnNo = pr.ReturnNo,
                SupplierId = pr.SupplierId,
                SupplierName = pr.Supplier != null ? pr.Supplier.CompanyName : string.Empty,
                ReturnDate = pr.ReturnDate,
                ReturnReason = pr.ReturnReason,
                Status = pr.Status,
                PostedBy = pr.PostedBy,
                PostedOn = pr.PostedOn,
                CreatedBy = pr.CreatedBy,
                CreatedOn = pr.CreatedOn,
                Items = pr.Items
                .Where(item => item.CompanyId == companyId && !item.DeletedOn.HasValue)
                .OrderBy(item => item.Id)
                .Select(item => new PurchaseReturnItemDto
                {
                    Id = item.Id,
                    PurchaseReturnId = item.PurchaseReturnId,
                    GoodReceiptItemId = item.GoodReceiptItemId,
                    GoodReceiptId = item.GoodReceiptItem!.GoodReceiptId,
                    ReceiptNo = item.GoodReceiptItem.GoodReceipt.ReceiptNo,
                    PurchaseOrderId = item.GoodReceiptItem.GoodReceipt.PurchaseOrderId,
                    PurchaseOrderNo = item.GoodReceiptItem.GoodReceipt.PurchaseOrder != null
                    ? item.GoodReceiptItem.GoodReceipt.PurchaseOrder.PurchaseOrderNo : string.Empty,
                    ProductId = item.GoodReceiptItem.ProductId,
                    ProductName = item.GoodReceiptItem.Product.Name,
                    UomId = item.GoodReceiptItem.UomId,
                    UomName = item.GoodReceiptItem.Uom.Name,
                    WarehouseId = item.GoodReceiptItem.GoodReceipt.WarehouseId,
                    WarehouseName = item.GoodReceiptItem.GoodReceipt.Warehouse != null
                    ? item.GoodReceiptItem.GoodReceipt.Warehouse.Name : string.Empty,
                    ReceivedQuantity = item.GoodReceiptItem.ReceivedQuantity,
                    ReturnableQuantity = item.GoodReceiptItem.ReceivedQuantity -
                        (_context.PurchaseReturnItems.Where(returned => returned.CompanyId == companyId
                            && returned.GoodReceiptItemId == item.GoodReceiptItemId
                            && !returned.DeletedOn.HasValue
                            && returned.PurchaseReturn!.CompanyId == companyId
                            && !returned.PurchaseReturn.DeletedOn.HasValue
                            && returned.PurchaseReturn.Status == PurchaseReturnStatus.Posted)
                            .Sum(returned => (decimal?)returned.ReturnQuantity) ?? 0),
                    ReturnQuantity = item.ReturnQuantity
                }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new KeyNotFoundException("Purchase Return was not found.");

        if (!string.IsNullOrWhiteSpace(result.PostedBy))
        {
            var names = await _identityService.GetUserNamesAsync(new[] { result.PostedBy }, cancellationToken);
            result.PostedByName = names.TryGetValue(result.PostedBy, out var name) ? name : "Unknown user";
        }

        return result;
    }
}
```

Handler ကို GetPurchaseReturnByIdQueryHandler.cs မှာ တစ်ခုတည်းထားသည်။

```csharp
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;

public class GetPurchaseReturnByIdQueryHandler : IRequestHandler<GetPurchaseReturnByIdQuery, PurchaseReturnDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;

    public GetPurchaseReturnByIdQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, IIdentityService identityService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _identityService = identityService;
    }

    public async Task<PurchaseReturnDto> Handle(GetPurchaseReturnByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;
        var result = await _context.PurchaseReturns
            .AsNoTracking()
            .Where(pr => pr.Id == request.Id && pr.CompanyId == companyId && !pr.DeletedOn.HasValue)
            .Select(pr => new PurchaseReturnDto
            {
                Id = pr.Id,
                ReturnNo = pr.ReturnNo,
                SupplierId = pr.SupplierId,
                SupplierName = pr.Supplier != null ? pr.Supplier.CompanyName : string.Empty,
                ReturnDate = pr.ReturnDate,
                ReturnReason = pr.ReturnReason,
                Status = pr.Status,
                PostedBy = pr.PostedBy,
                PostedOn = pr.PostedOn,
                CreatedBy = pr.CreatedBy,
                CreatedOn = pr.CreatedOn,
                Items = pr.Items
                .Where(item => item.CompanyId == companyId && !item.DeletedOn.HasValue)
                .OrderBy(item => item.Id)
                .Select(item => new PurchaseReturnItemDto
                {
                    Id = item.Id,
                    PurchaseReturnId = item.PurchaseReturnId,
                    GoodReceiptItemId = item.GoodReceiptItemId,
                    GoodReceiptId = item.GoodReceiptItem!.GoodReceiptId,
                    ReceiptNo = item.GoodReceiptItem.GoodReceipt.ReceiptNo,
                    PurchaseOrderId = item.GoodReceiptItem.GoodReceipt.PurchaseOrderId,
                    PurchaseOrderNo = item.GoodReceiptItem.GoodReceipt.PurchaseOrder != null
                    ? item.GoodReceiptItem.GoodReceipt.PurchaseOrder.PurchaseOrderNo : string.Empty,
                    ProductId = item.GoodReceiptItem.ProductId,
                    ProductName = item.GoodReceiptItem.Product.Name,
                    UomId = item.GoodReceiptItem.UomId,
                    UomName = item.GoodReceiptItem.Uom.Name,
                    WarehouseId = item.GoodReceiptItem.GoodReceipt.WarehouseId,
                    WarehouseName = item.GoodReceiptItem.GoodReceipt.Warehouse != null
                    ? item.GoodReceiptItem.GoodReceipt.Warehouse.Name : string.Empty,
                    ReturnQuantity = item.ReturnQuantity
                }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);

        if (result == null)
            throw new KeyNotFoundException("Purchase Return was not found.");

        if (!string.IsNullOrWhiteSpace(result.PostedBy))
        {
            var names = await _identityService.GetUserNamesAsync(new[] { result.PostedBy }, cancellationToken);
            result.PostedByName = names.TryGetValue(result.PostedBy, out var name) ? name : "Unknown user";
        }

        return result;
    }
}
```

ဒီ query ကို GET controller၊ create result နဲ့ retry result သုံးနေတယ်။ သီးခြား PurchaseReturnReader class မရှိဘူး။ `IRequestHandler<GetPurchaseReturnByIdQuery,PurchaseReturnDto>` က query နဲ့ result types သတ်မှတ်တယ်။

`Where` က ID/current company/not-deleted စစ်တယ်။ Global DbContext filter မှာ SuperAdmin bypass ရှိပေမယ့် ဒီ explicit condition က current company သာယူတယ်။ `DeletedOn.HasValue` က nullable DateTime မှာ value ရှိလားစစ်ပြီး `!` ကမရှိတာကိုရွေးတယ်။

`Select(pr => new PurchaseReturnDto { ... })` က entity အကုန် load မလုပ်ဘဲ response fields ကိုရွေးတဲ့ projection ဖြစ်တယ်။ Navigation ကို projection ထဲသုံးတဲ့အတွက် ဒီနေရာမှာ Include ထပ်မလိုဘူး။ EF က relationships ကို SQL query အဖြစ် translate လုပ်ပေးတယ်။ Inner Items.ToList က nested collection projection materialization ဖြစ်တယ်။ DB terminal method က SingleOrDefaultAsync ဖြစ်နေသေးတယ်။

`condition ? valueIfTrue : valueIfFalse` က ternary operator ပါ။ Supplier navigation မရှိရင် string.Empty ပေးတာ၊ optional PO/Warehouse names တွေမှာလည်း fallback ရှိတယ်။ Return item ရဲ့ FK နဲ့ source receipt line ကိုသုံးပြီး Product/UOM/GRN/PO/Warehouse ဖြည့်တယ်။ Source GRN ကိုပြောင်းရင် response source values လည်းပြောင်းနိုင်တဲ့အတွက် posted source mutation guards နောက်မှာလိုတယ်။ Historical snapshot fields သီးခြားမသိမ်းသေးဘူး။

`result == null` ဆို KeyNotFoundException ပစ်ပြီး existing middleware က 404 ပြန်ပေးတယ်။ PostedBy nonempty ဆို IdentityService ကို user IDs array `new[] { ... }` နဲ့ပို့တယ်။ Dictionary.TryGetValue က username ရှာပြီး မတွေ့ရင် Unknown user ပြတယ်။ Draft မှာ PostedBy null ဖြစ်လို့ Identity query မလုပ်ဘူး။

DTO က client ကိုပြမယ့် data structure၊ entity က persistence model၊ command က input ဖြစ်တယ်။ သုံးခုကိုတူတူမယူဆပါနဲ့။ `TotalItems => Items.Count` က expression-bodied computed property ဖြစ်လို့ response Items count ကိုဖတ်တိုင်းတွက်တယ်။ DB column မဟုတ်ဘူး။

## ၁၅။ Controller code — HTTP boundary

Source: `src/InventoryManagementSystem.Api/Controllers/Process/PurchaseReturnsController.cs`။ အောက်က code သည် 2026-10-14 source snapshot ဖြစ်သည်။ နောက် code ပြောင်းလျှင် ဒီစာမျက်နှာလည်း ပြန်ညှိရန်လိုသည်။

```csharp
using InventoryManagementSystem.Application.Common.Models;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Commands;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagementSystem.Api.Controllers.Process;

[Authorize]
[Route("api/process/purchase-returns")]
[ApiController]
public class PurchaseReturnsController : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a purchase return by ID in the current company")]
    public async Task<IActionResult> GetPurchaseReturnById(Guid id, CancellationToken cancellationToken)
    {
        PurchaseReturnDto result = await Mediator.Send(new GetPurchaseReturnByIdQuery(id), cancellationToken);
        return Ok(new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status200OK,
            Success = true,
            Message = "Purchase return retrieved successfully.",
            Data = result
        });
    }

    [HttpPost]
    [EndpointSummary("Create a purchase return draft")]
    public async Task<IActionResult> CreatePurchaseReturn(
        [FromBody] CreatePurchaseReturnCommand command, CancellationToken cancellationToken)
    {
        PurchaseReturnDto result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetPurchaseReturnById), new { id = result.Id }, new DefaultResponseModel
        {
            StatusCode = StatusCodes.Status201Created,
            Success = true,
            Message = "Purchase return draft saved successfully.",
            Data = result
        });
    }
}
```

`[Authorize]` က authenticated user လိုတယ်။ ဒီတစ်ခုတည်းက administrator-only မဖြစ်စေဘူး။ `[Route]` က URL prefix၊ `[ApiController]` က API binding/validation conventions၊ `[HttpPost]` နဲ့ `[HttpGet]` က HTTP methods သတ်မှတ်တယ်။ `{id:guid}` က route GUID constraint ပါ။ EndpointSummary က API docs စာသားသာဖြစ်ပြီး business rule မဟုတ်ဘူး။

`[FromBody]` က JSON body ကို command အဖြစ် model bind လုပ်ခိုင်းတယ်။ `Task<IActionResult>` က response အမျိုးအစား 200/201 စသည်ကို action ကရွေးပြန်နိုင်စေတယ်။ `Mediator.Send` က existing ApiControllerBase ရဲ့ sender ကိုသုံးပြီး request နဲ့ matching handler ကိုပို့တယ်။ Handler class ကို controller က new မလုပ်ရဘူး။

GET မှာ `new GetPurchaseReturnByIdQuery(id)` ပို့ပြီး `Ok(...)` က 200 ဖြစ်တယ်။ POST မှာ `CreatedAtAction` က 201 response နဲ့ detail URL Location header ထည့်ပေးတယ်။ `nameof(GetPurchaseReturnById)` က method name ကို compile-time string ထုတ်တာဖြစ်ပြီး method rename လုပ်ရာမှာ literal string ထက်ပိုလုံခြုံတယ်။ `new { id = result.Id }` က route values object ပါ။

DefaultResponseModel ရဲ့ Success/Message/Data က project response envelope ဖြစ်တယ်။ Create message မှာ draft saved ဆိုတာအသစ်နဲ့ retry နှစ်ခုလုံးအတွက်သုံးထားတယ်။ ဒီ endpoint များကို controller ထည့်တာနဲ့ existing MapControllers က route register လုပ်ပေးတယ်။ Global auth/antiforgery/exception handling ကိုမပြောင်းထားဘူး။

## ၁၆။ DbContext mapping code — schema နဲ့ query filter

အောက်က code သည် လက်ရှိ InventoryManagementDbContext.cs မှ Purchase Return mapping extract ဖြစ်သည်။

```csharp
        modelBuilder.Entity<PurchaseReturn>(entity =>
        {
            entity.ToTable("PurchaseReturns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReturnNo).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ReturnDate).HasColumnType("datetime");
            entity.Property(e => e.PostedBy).HasMaxLength(100);
            entity.Property(e => e.PostedOn).HasColumnType("datetime");
            entity.Property(e => e.ReturnReason).HasMaxLength(500);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.CreatedBy).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.UpdatedBy).HasMaxLength(50);
            entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            entity.Property(e => e.DeletedBy).HasMaxLength(50);
            entity.Property(e => e.DeletedOn).HasColumnType("datetime");
            entity.HasOne(d => d.Supplier)
                  .WithMany()
                  .HasForeignKey(d => d.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(d => new { d.CompanyId, d.IdempotencyKey })
                  .IsUnique()
                  .HasFilter("[IdempotencyKey] IS NOT NULL");
            entity.HasIndex(e => new { e.CompanyId, e.ReturnNo }).IsUnique().HasFilter(null);
            entity.Property(d => d.IdempotencyRequestHash)
                  .HasMaxLength(64)
                  .IsUnicode(false);
        });

        modelBuilder.Entity<PurchaseReturnItem>(entity =>
        {
            entity.ToTable("PurchaseReturnItems");
            entity.Property(e => e.Id);
            entity.Property(e => e.PurchaseReturnId);
            entity.Property(e => e.GoodReceiptItemId);
            entity.Property(e => e.ReturnQuantity).HasColumnType("decimal(18,4)");
            entity.HasOne(d => d.PurchaseReturn)
                  .WithMany(p => p.Items)
                  .HasForeignKey(d => d.PurchaseReturnId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.GoodReceiptItem)
                  .WithMany()
                  .HasForeignKey(d => d.GoodReceiptItemId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new
            {
                e.PurchaseReturnId,
                e.GoodReceiptItemId
            }).IsUnique();
        });
```

`modelBuilder.Entity<T>(entity => { ... })` က entity type တစ်ခုရဲ့ EF mapping ကို configure လုပ်တယ်။ `ToTable` က table name၊ `HasKey` က primary key၊ `Property` က column rules သတ်မှတ်တယ်။ Item Id ကို explicit HasKey မရေးပေမယ့် inherited Id property ကို EF convention က PK အဖြစ်သိတယ်။ `entity.Property(e => e.Id)` က property register လုပ်တာ၊ key ကြေညာခြင်းမဟုတ်ဘူး။

`HasMaxLength` က column length configuration၊ `IsRequired` က database null constraint ဖြစ်တယ်။ Empty/whitespace string ကို database NOT NULL ကမပိတ်လို့ validator NotEmpty လည်းလိုတယ်။ `HasColumnType` က SQL datatype၊ `HasConversion<int>` က enum ကို int သိမ်းဖို့ပါ။ Request validator မဟုတ်ဘူး။

`HasOne(...).WithMany(...).HasForeignKey(...)` က relationships သတ်မှတ်တယ်။ Supplier.WithMany() မှာ reverse collection မရှိ၊ Return.Items မှာ collection ရှိလို့ WithMany(p => p.Items) သုံးတယ်။ Restrict က referenced supplier/receipt line ကို physical delete လုပ်မရစေတယ်။ Cascade က header physical delete လုပ်ရင် child lines ကို database ကဖျက်ပေးနိုင်တယ်။ Soft delete က audit code က DeletedOn ရေးတာဖြစ်ပြီး SQL DELETE မဟုတ်လို့ cascade နဲ့တူတူမဟုတ်ဘူး။ Posted delete rules သီးခြားလိုတယ်။

`new { d.CompanyId, d.IdempotencyKey }` က composite index fields သတ်မှတ်တာ။ IsUnique က တူညီတဲ့ pair မထပ်ရ။ `[IdempotencyKey] IS NOT NULL` filter က key မရှိတဲ့ legacy rows ကိုမစစ်ဖို့ပါ။ ReturnNo index ရဲ့ HasFilter(null) က nullable CompanyId အတွက် EF ရဲ့ automatic filter ကိုဖယ်ပြီး null-company rows ကိုပါ uniqueness စစ်ဖို့ပါ။ Item composite index က Return တစ်စောင်ထဲ GRN line တစ်ကြောင်းမထပ်စေဘူး။

Hash column max 64 နဲ့ IsUnicode(false) က hex characters အတွက် varchar(64) သိမ်းဖို့ပါ။ Quantity decimal(18,4) က receipt/stock precision နဲ့တူပြီး money amount decimal(18,2) ကသီးခြားဖြစ်တယ်။ Header audit fields ကို length/date mappings ထားပေမယ့် item audit fields က convention mappings သုံးထားတာကို migration မှာတွေ့နိုင်တယ်။ လက်ရှိ design အမှန်ကိုရှင်းပြထားခြင်းဖြစ်ပြီး hidden schema change မလုပ်ထားဘူး။

DbContext.Custom.cs ထဲ company filters က—

```csharp
modelBuilder.Entity<PurchaseReturn>()
    .HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
modelBuilder.Entity<PurchaseReturnItem>()
    .HasQueryFilter(e => IsSuperAdminUser || e.CompanyId == CurrentCompanyId);
```

`||` က bypass သို့မဟုတ် current-company match ရရင် query ထဲပါစေတယ်။ ဒီ filters က query visibility ကိုထိန်းပြီး inserted row CompanyId ကိုမသတ်မှတ်ပေးဘူး။ Current project SaveChangesAsync က added IMustHaveCompany rows မှာ CompanyId မရှိ/current company ရှိရင်ဖြည့်ပေးတယ်။ Handler မှာ explicit သတ်မှတ်ထားတာက document company ကိုရှင်းလင်းစေတယ်။

Audit interceptor မဟုတ်ဘဲ လက်ရှိ DbContext SaveChangesAsync override က Created/Updated/Deleted values ကိုသတ်မှတ်တယ်။ Business ReturnDate ကိုမပြောင်းဘူး။ Entity delete state ကို Modified ပြောင်းပြီး DeletedOn ရေးတဲ့ soft-delete ပုံစံဖြစ်တယ်။

## ၁၇။ ကိုယ်တိုင် ပြန်စဉ်းစားရန်

1. Company A/B မှာ key တူရင် ဘာကြောင့် company condition ထပ်လိုသလဲ။
2. Existing Return မရှိခင် hash comparison လုပ်ရင် ဘယ်လို create ပိတ်သွားမလဲ။
3. GRN line 101/102 က product တူရင် ဘာကြောင့် line နှစ်ကြောင်းလိုသလဲ။
4. Received 10၊ Posted return 3၊ Draft return 4 ရှိရင် returnable ဘယ်လောက်လဲ။ Draft အသစ် 7 သိမ်းလို့ရပေမယ့် နှစ်စောင်လုံး Posted လုပ်နိုင်မလား။
5. Add၊ SaveChanges၊ Commit သုံးခု ဘယ်အချိန်မှာ database အပေါ် ဘာလုပ်သလဲ။
6. Draft မှာ PostedOn ဘာကြောင့် null လဲ၊ ReturnDate နဲ့ CreatedOn ဘာကွာသလဲ။
7. GET နဲ့ create response field အသစ်ထည့်ချင်ရင် mapping ဘယ်နေရာပြင်ရမလဲ။

နမူနာ 4 ရဲ့အဖြေ: returnable 7 ဖြစ်တယ်။ Draft မreserve လုပ်လို့ အခြား Draft ရှိလည်း 7 သိမ်းနိုင်တယ်။ Posted တစ်စောင်လုပ်ပြီးနောက် ကျန် returnable ပြောင်းသွားလို့ နှစ်စောင်လုံး မဖြစ်မနေ Posted လုပ်နိုင်တာမဟုတ်ဘူး။ Posting မှာ quantity/stock ကိုပြန်စစ်ရန်လိုသည်။

## ၁၈။ StockTransaction နဲ့ Return ချိတ်ခြင်း — 2026-10-15

ဤအပိုင်းသည် stock links ထည့်စဉ် ရှင်းလင်းချက်ဖြစ်သည်။ Posting code ကို အပိုင်း ၁၉ တွင် ဆက်ရေးပြီးဖြစ်သည်။
`StockTransaction` entity ထဲမှာ အောက်က fields/navigation properties ထည့်ထားသည်။

```csharp
public Guid? PurchaseReturnId { get; set; }
public long? PurchaseReturnItemId { get; set; }

[JsonIgnore]
public virtual PurchaseReturn? PurchaseReturn { get; set; }

[JsonIgnore]
public virtual PurchaseReturnItem? PurchaseReturnItem { get; set; }
```

ပထမ FK က Return စာရွက်ကိုချိတ်ပြီး ဒုတိယ FK က ဘယ် return line ကြောင့် movement
ဖြစ်သလဲဆိုတာချိတ်သည်။ `?` သုံးထားလို့ အရင် IN/OUT/TRANSFER records တွေမှာ null
ထားနိုင်သည်။ Existing records ကို Return တစ်စောင်နဲ့ ခန့်မှန်းချိတ်မပေးပါ။
Navigation properties က related objects ကိုဖတ်ဖို့ဖြစ်ပြီး FK columns နဲ့မတူပါ။

DbContext ရဲ့ existing StockTransaction mapping block ထဲမှာသာ ထပ်ထည့်ထားသည်။
Custom file method သို့ reader class အသစ် မလိုပါ။

```csharp
entity.HasOne(d => d.PurchaseReturn).WithMany()
    .HasForeignKey(d => d.PurchaseReturnId)
    .OnDelete(DeleteBehavior.Restrict);

entity.HasOne(d => d.PurchaseReturnItem).WithMany()
    .HasForeignKey(d => d.PurchaseReturnItemId)
    .OnDelete(DeleteBehavior.Restrict);
```

`HasOne` က movement တစ်ကြောင်းရဲ့ source Return တစ်ခု၊ `WithMany()` က source
တစ်ခုကို movements အများကြီးကချိတ်နိုင်ကြောင်း သတ်မှတ်သည်။ Reverse collection
မလိုလို့ အလွတ်ရေးထားသည်။ `Restrict` က movement ချိတ်ထားသော source ကို physical
delete လုပ်ရာမှာပိတ်သည်။ Soft delete နဲ့ Posted edit rules ကိုတော့ မပိတ်ပေးပါ။

ဒီ FK နှစ်ခုက header/item pair တူညီကြောင်း၊ company တူကြောင်း၊ movement quantity
မှန်ကြောင်း မစစ်ပေးပါ။ Posting handler မှာ မှန်တဲ့ source IDs ကို အတူဖြည့်ရမည်။
Item FK ကို unique မလုပ်ထားပါ၊ နောင် reversal movement မှာ source line ကိုပြန်ချိတ်
နိုင်ဖို့လိုနိုင်သည်။ Duplicate posting ကို workflow/transaction အဆင့်မှာကာကွယ်ရမည်။

Schema ပြောင်းလို့ migration လိုသည်။ Expected changes က StockTransactions table မှာ
nullable uniqueidentifier/bigint columns နှစ်ခု၊ FK indexes နဲ့ Restrict foreign keys
နှစ်ခုဖြစ်သည်။ ဒီအဆင့်မှာ migration ဖန်တီးခြင်း/database update မလုပ်ထားပါ။


## ၁၉။ Posting backend အပြီးသတ် — 2026-10-15

လက်ရှိ backend မှာ Draft → Posted၊ stock လျော့ခြင်း၊ movement နဲ့ audit ရေးခြင်း
ပြီးဖြစ်သည်။ အပေါ်က Draft-only မှတ်ချက်များသည် အစောပိုင်းအဆင့်ကိုရှင်းပြခြင်းဖြစ်သည်။
ဒီအပိုင်းက လက်ရှိ posting behavior ကိုဖော်ပြသည်။ Custom file method/reader class အသစ်မရှိပါ။

```http
POST /api/process/purchase-returns/{id}/post
X-XSRF-TOKEN: <လက်ရှိ csrf token>
```

JSON body မလိုပါ။ Existing login cookies/antiforgery အတွဲလိုသည်။ Controller က
route ID ကို CreatePurchaseReturnPostCommand ထဲထည့်ပို့သည်။ Dedicated posting role
အသစ်မထည့်ဘဲ existing Authorize ကိုသုံးသည်။ 200 + Data ထဲ Return DTO ပြန်ရသည်။
Posted ပြီးသားဆို result ပြန်ပေးပြီး stock ထပ်မလျော့ပါ။ Create key ထပ်မလိုပါ။

### Handler code အပြည့်

Source: src/InventoryManagementSystem.Application/Process/PurchaseReturns/Commands/CreatePurchaseReturnPost/CreatePurchaseReturnPostCommandHandler.cs

```csharp
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Application.Process.PurchaseReturns.DTOs;
using InventoryManagementSystem.Application.Process.PurchaseReturns.Queries.GetPurchaseReturnById;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace InventoryManagementSystem.Application.Process.PurchaseReturns.Commands.CreatePurchaseReturnPost;

public class CreatePurchaseReturnPostCommandHandler : IRequestHandler<CreatePurchaseReturnPostCommand, PurchaseReturnDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;
    public CreatePurchaseReturnPostCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }
    public async Task<PurchaseReturnDto> Handle(CreatePurchaseReturnPostCommand request, CancellationToken cancellationToken)
    {
        var companyId = _currentUserService.CompanyId;

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        PurchaseReturn? postedReturn = await _context.PurchaseReturns
            .Include(pr => pr.Items)
            .ThenInclude(item => item.GoodReceiptItem)
            .ThenInclude(receiptItem => receiptItem!.GoodReceipt)
            .FirstOrDefaultAsync(x => x.Id == request.PurchaseReturnId && x.CompanyId == companyId && !x.DeletedOn.HasValue, cancellationToken);

        if(postedReturn == null)
        {
            throw new KeyNotFoundException("Purchase return is not found");
        }

        if(postedReturn.Status == PurchaseReturnStatus.Posted)
        {
            var result = await _sender.Send(new GetPurchaseReturnByIdQuery(request.PurchaseReturnId),cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        if(postedReturn.Status != PurchaseReturnStatus.Draft)
        {
            throw new InvalidOperationException("Only Draft Return can be posted.");
        }

        List<PurchaseReturnItem> items = postedReturn.Items.Where(x => !x.DeletedOn.HasValue).ToList();

        if (items.Count == 0)
        {
            throw new InvalidOperationException("At least One return is required.");
        }

        List<long> receiptItemIds = items.Select(i => i.GoodReceiptItemId).ToList();
        if (receiptItemIds.Distinct().Count() != receiptItemIds.Count)
            throw new InvalidOperationException("Each receipt item can appear only once in a return.");

        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("Login is required to post a return.");
        if (!await _context.Suppliers.AnyAsync(s => s.Id == postedReturn.SupplierId && s.CompanyId == companyId
            && !s.DeletedOn.HasValue, cancellationToken))
            throw new InvalidOperationException("The return supplier is unavailable.");

        var returnedQuantities = await _context.PurchaseReturnItems
            .AsNoTracking()
            .Where(item => 
            receiptItemIds.Contains(item.GoodReceiptItemId)
            && item.CompanyId == companyId
            &&  !item.DeletedOn.HasValue
            && item.PurchaseReturn!.CompanyId == companyId
            && !item.PurchaseReturn.DeletedOn.HasValue
            && item.PurchaseReturn.Status == PurchaseReturnStatus.Posted)
            .GroupBy(item => item.GoodReceiptItemId)
            .Select(group => new
            {
                ReceiptItemId = group.Key,
                Quantity = group.Sum(item => item.ReturnQuantity)
            })
            .ToDictionaryAsync(item => item.ReceiptItemId, item  => item.Quantity,cancellationToken);
        
        var stockQuantities = new Dictionary<long, Decimal>();
        foreach (var item in items)
        {
            var source = item.GoodReceiptItem;

            if (item.CompanyId != companyId || source == null || source.DeletedOn.HasValue || source.CompanyId != companyId)
            {
                throw new InvalidOperationException("The source receipt item is unavailable.");
            }

            var receipt = source.GoodReceipt;

            if (receipt.CompanyId != companyId|| receipt.DeletedOn.HasValue|| !receipt.Status)
            {
                throw new InvalidOperationException(
                    "Only active, received goods receipts can be returned.");
            }

            if (receipt.SupplierId != postedReturn.SupplierId)
            {
                throw new InvalidOperationException(
                    "The receipt supplier must match the return supplier.");
            }

            returnedQuantities.TryGetValue(source.Id, out var alreadyReturned);

            var returnableQuantity =
                source.ReceivedQuantity - alreadyReturned;

            if (item.ReturnQuantity <= 0
                || item.ReturnQuantity > returnableQuantity)    
            {
                throw new InvalidOperationException(
                    $"Invalid return quantity for receipt item {source.Id}. " +
                    $"Remaining returnable quantity is {returnableQuantity}.");
            }

            if (source.ReceivedQuantity <= 0|| !source.ReceivedBaseQuantity.HasValue|| source.ReceivedBaseQuantity.Value <= 0)
            {
                throw new InvalidOperationException(
                    "The receipt has no valid recorded stock quantity.");
            }

            var totalBaseReturned = decimal.Round((alreadyReturned + item.ReturnQuantity) * source.ReceivedBaseQuantity.Value / source.ReceivedQuantity,4);

            var previousBaseReturned = decimal.Round(alreadyReturned * source.ReceivedBaseQuantity.Value / source.ReceivedQuantity,4);

            var stockQuantity = totalBaseReturned - previousBaseReturned;

            if (stockQuantity <= 0)
            {
                throw new InvalidOperationException(
                    "Return quantity is below the supported stock precision.");
            }

            stockQuantities.Add(item.Id, stockQuantity);
        }
        var stockGroups = items.GroupBy(item => new
        {
            ProductId = item.GoodReceiptItem!.ProductId,
            WarehouseId = item.GoodReceiptItem.GoodReceipt.WarehouseId
        });

        foreach (var group in stockGroups
            .OrderBy(group => group.Key.ProductId)
            .ThenBy(group => group.Key.WarehouseId))
        {
            var requiredQuantity = group.Sum(item => stockQuantities[item.Id]);

            if (!await _context.Warehouses.AnyAsync(w => w.Id == group.Key.WarehouseId && w.CompanyId == companyId
                && !w.DeletedOn.HasValue, cancellationToken))
                throw new InvalidOperationException("The source warehouse is unavailable.");

            var stock = await _context.WarehouseStocks
                .SingleOrDefaultAsync(
                    stock => stock.CompanyId == companyId
                        && stock.ProductId == group.Key.ProductId
                        && stock.WarehouseId == group.Key.WarehouseId
                        && !stock.DeletedOn.HasValue,
                    cancellationToken);

            if (stock == null || stock.Quantity < requiredQuantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for product {group.Key.ProductId} " +
                    $"in warehouse {group.Key.WarehouseId}. " +
                    $"Required quantity is {requiredQuantity}.");
            }

            stock.Quantity -= requiredQuantity;
        }

        var now = DateTime.Now;
        foreach (var item in items)
        {
            var source = item.GoodReceiptItem!;
            _context.StockTransactions.Add(new StockTransaction
            {
                CompanyId = companyId,
                ProductId = source.ProductId,
                WarehouseId = source.GoodReceipt.WarehouseId,
                WarehouseLocationId = 0,
                Quantity = stockQuantities[item.Id],
                TransactionType = "OUT",
                TransactionDate = now,
                UserId = userId,
                PurchaseReturnId = postedReturn.Id,
                PurchaseReturnItemId = item.Id,
                Note = $"Purchase Return: {postedReturn.ReturnNo}"
            });
        }

        var productIds = items.Select(item => item.GoodReceiptItem!.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => p.CompanyId == companyId && productIds.Contains(p.Id) && !p.DeletedOn.HasValue)
            .ToListAsync(cancellationToken);
        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more return products are unavailable.");

        postedReturn.Status = PurchaseReturnStatus.Posted;
        postedReturn.PostedBy = userId;
        postedReturn.PostedOn = now;
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var product in products)
        {
            product.CurrentStock = await _context.WarehouseStocks
                .Where(stock => stock.CompanyId == companyId && stock.ProductId == product.Id && !stock.DeletedOn.HasValue)
                .SumAsync(stock => stock.Quantity, cancellationToken);
        }
        await _context.SaveChangesAsync(cancellationToken);

        var response = await _sender.Send(new GetPurchaseReturnByIdQuery(postedReturn.Id), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}
```

### Stock လျော့ခြင်း

```csharp
stock.Quantity -= requiredQuantity;
```

`-=` သည် မူလ quantity ထဲက required quantity နုတ်ပြီး property ထဲပြန်ထည့်ခြင်းဖြစ်သည်။
Product + Warehouse တူသော lines တွေကို GroupBy နဲ့စုထားလို့ 3+4 ဖြစ်လျှင် stock 7
လိုသည်။ Query က tracked entity ယူထားလို့ EF က property ပြောင်းတာမှတ်ထားသည်။
Group တစ်ခုလျော့ပြီး နောက်တစ်ခု stock မလုံလောက်ရင် commit မဖြစ်လို့ partial stock
deduction မကျန်ပါ။ Product/warehouse/supplier ဖျက်ထားလျှင်လည်း posting ပိတ်သည်။

### Movement တစ်ကြောင်းစီ

Return line တစ်ကြောင်းစီအတွက် OUT transaction တစ်ကြောင်းဖန်တီးသည်။ Quantity သည်
positive ဖြစ်ပြီး stock balance ကို အပေါ်မှာတကယ်လျော့သည်။ OUT စာသားရေးရုံနဲ့
database ကအလိုအလျောက်နုတ်ခြင်းမဟုတ်ပါ။ `stockQuantities[item.Id]` ကမူလ receipt
conversion နဲ့တွက်ထားသော stock quantity ကိုပြန်ယူသည်။ PurchaseReturnId/item ID
နှစ်ခုလုံးဖြည့်ပြီး source document ကို trace လုပ်နိုင်သည်။

WarehouseLocationId=0 က existing GRN movement convention အတိုင်းဖြစ်သည်။ ဒီ flow
က location/bin allocation မလုပ်သေးပါ။ GoodReceipt fields ထဲ Return IDs မထည့်ပါ။

### Partial returns နဲ့ rounding

စုစုပေါင်းပြန်ပို့ပြီး quantity ကို stock unit ပြောင်းပြီး round လုပ်ထားတာထဲက အရင်
ပြန်ပို့ပြီးသား stock quantity ကိုနုတ်သည်။ Partial returns အကြိမ်များလျှင် rounding
အကြွင်းစုမလာဖို့ cumulative method သုံးခြင်းဖြစ်သည်။ decimal.Round(value,4) က
ဒဿမ 4 နေရာထားပြီး default midpoint rule သည် ToEven ဖြစ်သည်။

မူလ ReceivedBaseQuantity မရှိသော legacy receipt ကို conversion ခန့်မှန်းမလုပ်ဘဲ
ပိတ်သည်။ လက်ရှိ conversion setting ကိုပြန်မဖတ်ဘဲ receipt မှာသိမ်းထားသော ratio
ကိုသုံးသည်။ Unit policy အသစ်ပြောင်းထားခြင်းမဟုတ်ပါ။

### Posting audit နဲ့ Save နှစ်ကြိမ်

```csharp
postedReturn.Status = PurchaseReturnStatus.Posted;
postedReturn.PostedBy = userId;
postedReturn.PostedOn = now;
```

UserId ကိုသိမ်းပြီး username ကို detail query က resolve လုပ်သည်။ `now` သည်
DateTime.Now ဖြစ်ပြီး movements နဲ့ PostedOn မှာ အတူသုံးသည်။ ReturnDate ကိုမပြောင်းပါ။
Movement date သည် posting အချိန်ဖြစ်သည်။

ပထမ SaveChangesAsync က stock balances/movements/status ကို transaction ထဲရေးသည်။
ပြီးမှ SQL SumAsync ဖြင့် warehouse stocks ပေါင်းပြီး Product.CurrentStock ကိုညှိသည်။
ပထမ Save မတိုင်ခင် sum ဖတ်လျှင် DB မှာမရေးရသေးသော stock အဟောင်းရနိုင်သည်။
ဒုတိယ Save က product cache update သိမ်းသည်။ Detail DTO ဖတ်ပြီး CommitAsync လုပ်သည်။
အလယ်မှာ fail လျှင် Save နှစ်ကြိမ်ရှိလည်း transaction တစ်ခုထဲဖြစ်လို့ rollback ဖြစ်သည်။
PO receiving quantity/status ကို Return posting ကမပြောင်းပါ။

### Existing flows အတွက် guards

- Return-linked stock movements ကို generic stock update/delete ကနေမပြောင်းနိုင်ပါ။
- Posted return ချိတ်ထားသော GRN ကို reverse မလုပ်နိုင်ပါ။
- Posted return ချိတ်ထားသော GRN ရဲ့ receiving Status မပြောင်းနိုင်ပါ။ Note၊ ReceivedBy၊
  ReceiptDate metadata ကိုတော့ existing update flow အတိုင်းပြင်နိုင်သည်။

Return edit/delete/reversal endpoint မရှိသေးပါ။ နောင်ထည့်လျှင် Posted facts ကို
တိုက်ရိုက်မဖျက်မပြင်ရန်လိုသည်။ ဒီ guards က reversal workflow မဟုတ်ပါ။

### Verification နဲ့ remaining limits

API build စစ်ထားသည်။ Database/API/manual concurrency scenarios မလုပ်ရသေးပါ။
Existing BeginTransactionAsync က default Serializable ဖြစ်သော်လည်း parallel requests
မှာ deadlock/conflict ဖြစ်နိုင်သည်။ Automatic retry မထည့်ထားလို့ နှစ်ခုလုံး 200
ဖြစ်မည်လို့မအာမခံပါ။ Conflict ဖြစ်ပြီး retry လုပ်လျှင် Posted document က stock
ထပ်မလျော့သင့်ပါ။ ဒီ behavior ကို SQL Server နဲ့တကယ်စမ်းရန်လိုသည်။

| Scenario | မျှော်လင့်ချက် |
|---|---|
| Stock 10၊ return 3 post | 200၊ stock 7၊ OUT တစ်ကြောင်း၊ Posted audit ဖြည့် |
| Same Return ထပ် post | Stock 7 ပဲ၊ movement မထပ် |
| Rice/A lines 3+4၊ stock 5 | 400၊ Draft ဆက်ဖြစ်၊ stock မပြောင်း |
| Group တစ်ခု stock လုံလောက်၊ နောက်တစ်ခုမလုံလောက် | အားလုံး rollback |
| Received 10၊ prior Posted 7၊ current 4 | 400၊ stock မပြောင်း |
| Legacy receipt base quantity null | 400 |
| Return movement generic edit/delete | 400 |
| Returned GRN reverse/status ပြောင်း | 400 |
| Different company ID / missing Return | 404 |
| Same Return parallel post | Stock တစ်ကြိမ်သာလျော့၊ conflict ဖြစ်နိုင် |
| Returns နှစ်စောင် remaining quantity ကိုပြိုင်ယူ | ပေါင်းပြီး received limit မကျော်ရ |

ဒီပြင်ချက်မှာ schema အသစ်မရှိလို့ migration အသစ်မလိုပါ။ AddPurchaseReturnStockLinks
ကို developer က update ပြီးဟု အတည်ပြုထားပြီး agent က DB history ကိုတိုက်ရိုက်မစစ်ထားပါ။

လေ့ကျင့်ရန်: stock.Quantity ပြောင်းခြင်း၊ DbSet.Add၊ SaveChangesAsync နဲ့ CommitAsync
လေးခု ဘာကွာလဲကို source မကြည့်ဘဲပြန်ရှင်းပါ။ Product.CurrentStock SQL sum မဖတ်ခင်
ပထမ Save ဘာကြောင့်လိုသလဲကို quantity နမူနာနဲ့ပြန်ရှင်းပါ။

## ၂၀။ Purchase Return list/search/pagination — 2026-10-19

ရှိပြီးသား query request ရဲ့ StartDate/EndDate နာမည်တွေကိုဆက်သုံးထားသည်။
သီးခြား reader/service မထည့်ဘဲ existing query၊ handler၊ controller နဲ့ DTO ကိုသုံးသည်။

```http
GET /api/process/purchase-returns?pageNumber=1&pageSize=20
GET /api/process/purchase-returns?q=PR-001&status=0
GET /api/process/purchase-returns?supplierId=7&startDate=2026-10-01&endDate=2026-10-31&sortField=returnDate&order=-1
```

`status=0` က Draft၊ `status=1` က Posted၊ မပို့လျှင် နှစ်မျိုးလုံးပါသည်။
Date filters က ReturnDate ကိုစစ်ပြီး အစ/အဆုံး calendar days နှစ်ခုလုံးပါသည်။
Search က ReturnNo နဲ့ supplier CompanyName ကိုရှာသည်။ Amount calculation မရှိသေးလို့
TotalAmount search မထည့်ထားပါ။ Decimal ကို keyword string နဲ့ == နှိုင်းလို့မရပါ။

### Query request နဲ့ execution order

Source: src/InventoryManagementSystem.Application/Process/PurchaseReturns/Queries/GetPurchaseReturns/

```text
Current company + not deleted
→ keyword / supplier / status / date filters
→ CountAsync
→ Sort + ThenBy(Id)
→ Skip / Take
→ DTO projection + ToListAsync
→ page ထဲက PostedBy IDs နဲ့ usernames တစ်ကြိမ်ယူ
→ PagedResult ပြန်ပေး
```

`IQueryable<PurchaseReturn>` က SQL query တည်ဆောက်နေဆဲဖြစ်သည်။ Filter တစ်ခုရှိတိုင်း
`query = query.Where(...)` နဲ့ condition ထပ်ထည့်သည်။ Database results အကုန်ဖတ်ပြီး
memory ထဲမှာ pagination လုပ်ထားခြင်းမဟုတ်ပါ။

```csharp
var totalRecords = await query.CountAsync(cancellationToken);
```

Count ကို Skip/Take မတိုင်ခင်တွက်လို့ current filter နဲ့ကိုက်တဲ့ documents စုစုပေါင်း
ရသည်။ ဥပမာ total 43/page size 20 ဆို pages သုံးခုဖြစ်သည်။ တစ်မျက်နှာပေါ်လာတဲ့
items count နဲ့ TotalRecords မတူနိုင်ပါ။

```csharp
query = sortedQuery.ThenBy(pr => pr.Id);

if (request.PageNumber.HasValue && request.PageSize.HasValue)
{
    query = query.Skip((request.PageNumber.Value - 1) * request.PageSize.Value)
        .Take(request.PageSize.Value);
}
```

GetProductsQueryHandler ပုံစံအတိုင်း PageNumber/PageSize ကို nullable int နဲ့ default
null ထားသည်။ နှစ်ခုလုံးပါမှ pagination လုပ်ပြီး တစ်ခုခုမပါလျှင် matching records
အားလုံးပြန်ပေးသည်။ Page-size cap၊ offset early-return နဲ့ normalization မထည့်ထားပါ။
Pagination သုံးလျှင် caller က pageNumber >= 1၊ pageSize > 0 ပို့ရမည်။

```csharp
var items = await query
    .Select(pr => new PurchaseReturnDto { /* response mapping */ })
    .ToListAsync(cancellationToken);
```

ဒီ block က abbreviated extract ဖြစ်သည်။ Skip ကရှေ့ rows ဘယ်လောက်ကျော်မလဲ၊
Take ကဘယ်နှစ်ခုယူမလဲသတ်မှတ်သည်။ Page 2/size 20 ဆို Skip 20/Take 20 ဖြစ်သည်။
ThenBy(Id) က date/supplier တူလျှင်တောင် deterministic order ရှိဖို့ဖြစ်သည်။
Database မှာ data အသစ်ဝင်နေစဉ် offset pagination က snapshot consistency အာမမခံပါ။

Response metadata ကလည်း Products ပုံစံအတိုင်း `PageNumber = request.PageNumber ?? 1`၊
`PageSize = request.PageSize ?? totalRecords` ပြန်ပေးသည်။ `??` က null ဖြစ်မှ ညာဘက်
fallback ကိုယူသည်။ Controller က `[FromQuery] GetPurchaseReturnsQuery request` ကို
တိုက်ရိုက်လက်ခံပြီး Mediator.Send(request) လုပ်သည်။

### Sorting

Supported fields: returnNo၊ supplierName၊ status၊ createdOn၊ postedOn၊ returnDate။
Unknown/missing field ဆို ReturnDate သုံးသည်။ `order=1` က ascending၊ အခြားတန်ဖိုးက
existing project convention အရ descending ဖြစ်သည်။

```csharp
var sortedQuery = request.SortField?.Trim().ToLowerInvariant() switch
{
    "returnno" => request.Order == 1
        ? query.OrderBy(pr => pr.ReturnNo)
        : query.OrderByDescending(pr => pr.ReturnNo),
    // အခြား supported fields များကို ဒီ extract မှာချန်ထားသည်။
    _ => request.Order == 1
        ? query.OrderBy(pr => pr.ReturnDate)
        : query.OrderByDescending(pr => pr.ReturnDate)
};
```

`switch` expression က field name နဲ့ကိုက်တဲ့ query ကိုရွေးသည်။ `_` က default arm၊
`?:` က ascending/descending query ကိုရွေးသည်။ Client ပေးတဲ့ column name ကို raw SQL
ထဲ concatenate မလုပ်ဘဲ supported fields အတိအကျရွေးထားသည်။

### Response နဲ့ username

Existing PurchaseReturnDto သုံးပြီး active item details ပါပြန်ပေးသည်။ ဒါကြောင့်
computed TotalItems က Items.Count နဲ့မှန်သည်။ Document တစ်စောင်မှာ lines အများကြီး
ရှိလျှင် response payload လည်းကြီးနိုင်သည်။ နောင်လိုအပ်ချက်ရှိမှ lightweight list DTO
ကိုခွဲရန်စဉ်းစားနိုင်ပြီး ဒီအဆင့်မှာ structure အသစ်မတိုးထားပါ။

PostedBy usernames ကို row တစ်ခုစီ query မလုပ်ဘဲ page ထဲက distinct user IDs ကိုစုပြီး
GetUserNamesAsync တစ်ကြိမ်သာခေါ်သည်။ Draft မှာ PostedByName null၊ မရှိတော့တဲ့ user
အတွက် Unknown user ပြသည်။ Company ကို explicit စစ်ထားလို့ detail endpoint နဲ့ scope တူသည်။

Response Data ထဲမှာ Items၊ TotalRecords၊ PageNumber၊ PageSize ပါသည်။
StartDate > EndDate သို့မဟုတ် undefined numeric status ဆို application 400 ပြန်ပေးသည်။
Date/number JSON/query binding မအောင်သော input ကို API model binding ကပိတ်သည်။

### Verification state

API build စစ်ထားသည်။ User သတ်မှတ်ချက်အရ manual API/database tests ကို နောက်မှလုပ်မည်။
ဒီအပြောင်းအလဲက read-only ဖြစ်ပြီး stock/schema မပြောင်းလို့ migration မလိုပါ။
နောက်မှစမ်းရန်: empty list၊ search match/no match၊ date endpoint days ပါခြင်း၊
Draft/Posted filters၊ page beyond last၊ stable sort၊ cross-company IDs၊ deleted exclusion။

လေ့ကျင့်ရန်: CountAsync ကို Skip/Take နောက်ရွှေ့လိုက်လျှင် TotalRecords ဘာကြောင့်
မှားသွားမလဲ၊ From/To filters ရှိ/မရှိ query ဘယ်လိုပြောင်းသလဲကို ပြန်ရှင်းပါ။

## ၂၁။ Angular Purchase Return UI — 2026-10-19

ရှိပြီးသား `/process/purchase-return` route နဲ့ sidebar menu ကိုဆက်သုံးပြီး placeholder
ကို list/create/detail/post UI နဲ့အစားထိုးထားသည်။ Theme၊ auth၊ menu role logic မပြောင်းပါ။

### Files နဲ့တာဝန်

- `src/InventoryManagementSystem.Client/src/app/core/models/process/purchase-return.model.ts`:
  Return၊ item၊ source receipt၊ create request၊ filters၊ page response types။
- `src/InventoryManagementSystem.Client/src/app/core/services/process/purchase-returns.service.ts`:
  HttpClient GET/create/post calls နဲ့ HttpParams တည်ဆောက်ခြင်း။
- `src/InventoryManagementSystem.Client/src/app/pages/process/purchase-return/purchase-return.ts`:
  Filters/pagination၊ form၊ receipt lines ရွေးခြင်း၊ save/retry၊ confirmation/post state။
- `purchase-return.html`: PrimeNG table/dialog/select/datepicker/inputnumber bindings။
- `purchase-return.scss`: Existing theme tokens နဲ့ card/labels/messages styling။

UI က Create၊ Detail၊ Post actions ပဲပေးသည်။ Backend မရှိသေးသော Edit/Delete/Reversal
buttons မထည့်ထားပါ။ Stock ပြောင်းတာက Post confirm ပြီး backend ကအတည်ပြုမှဖြစ်သည်။

### List/filter flow

PrimeNG table က lazy-load event ပို့ပြီး first/rows/sortField/sortOrder ကို component
ကဖတ်သည်။ `Math.floor(first / rows) + 1` နဲ့ one-based API page number တွက်သည်။
Filter Apply လုပ်လျှင် first=0 ပြန်ထားပြီး query အသစ်ပို့သည်။ Clear က optional
filters အားလုံးဖယ်သည်။ Default date filter မရှိလို့ records ကိုဒီနေ့တစ်နေ့ထဲမကန့်ထားပါ။
UI က pageNumber/pageSize နှစ်ခုလုံးပို့ပြီး server က Products pagination pattern
အတိုင်း Skip/Take လုပ်သည်။ Status 0 ကို valid filter အဖြစ်ပို့နိုင်ဖို့ nullish
coalescing `??` သုံးထားပြီး `status || undefined` မသုံးပါ။

Request sequence number ကိုအသုံးပြု၍ filter အဟောင်းရဲ့ response နောက်ကျမှရောက်လာလျှင်
အသစ် result ကိုမအစားထိုးစေပါ။ Error ဖြစ်လျှင် loading ပိတ်၊ list error နဲ့ Retry
ပြပြီး အဟောင်း data ကိုအမှန်လိုမပြထားပါ။

### Create form နဲ့ reactive forms

```typescript
readonly purchaseReturnForm = this.formBuilder.group({
  supplierId: [null as number | null, [Validators.required, Validators.min(1)]],
  returnDate: [new Date() as Date | null, Validators.required],
  returnReason: ['', Validators.maxLength(500)]
});
```

FormBuilder.group က controls အစုတည်ဆောက်သည်။ `[initialValue, validators]` ပုံစံဖြစ်ပြီး
`number | null` က supplier မရွေးရသေးသည့်အခြေအနေကို ကိုယ်စားပြုသည်။ `formControlName`
က HTML input ကို matching control နဲ့ချိတ်သည်။ `markAllAsTouched` က invalid fields
အတွက် messages ပြစေသည်။ Browser validation က user experience အတွက်ဖြစ်ပြီး backend
rules ကိုအစားထိုးမရပါ။ Return number ကို frontend က Create ဖွင့်ချိန်မှာထုတ်သည်။

```html
<input pInputText [value]="formReturnNo" disabled />
<p-inputnumber [(ngModel)]="line.returnQuantity"
  [ngModelOptions]="{standalone: true}"
  [minFractionDigits]="0" [maxFractionDigits]="4"></p-inputnumber>
```

`[(ngModel)]` က item quantity state နဲ့ control ကို two-way bind လုပ်သည်။ ဒီ number
input သည် parent FormGroup controls ထဲမပါလို့ standalone option သုံးသည်။ Supplier၊
return date၊ reason က reactive controls၊ dynamic line quantities က separate lines
array ဖြစ်သည်။ Save မှာ နှစ်မျိုးလုံးကိုစစ်သည်။ `isSelected` က receipt line တစ်ခု
နှစ်ခါမထည့်စေဘူး။ Product တူပေမယ့် GRN line မတူရင်ရွေးနိုင်သည်။ Supplier ပြောင်းလျှင်
အရင် selected lines ရှင်းသည်။ Receipt paging/search ပြောင်းလျှင် selected lines ဆက်ရှိသည်။

### Supplier အလိုက် source GRN lines query

Create form ကိုအလုပ်လုပ်စေဖို့ read-only endpoint တစ်ခုထည့်ထားသည်။

```http
GET /api/process/purchase-returns/returnable-items?supplierId=7&pageNumber=1&pageSize=10
```

Optional q က GRN/PO/product နာမည်ရှာပြီး StartDate/EndDate က receipt date ကို filter
လုပ်သည်။ Return header list ရဲ့ date filters က ReturnDate ဖြစ်တာနဲ့မတူပါ။
တစ်လစာရွေးချင်ရင် receipt date range ရွေးနိုင်သလို အရင်လတွေပါ ပြန်ရှာနိုင်သည်။

Query က current company၊ supplier တူ၊ received/မဖျက်ထားသော GRNs ထဲက lines ကိုယူသည်။
Recorded base quantity မရှိသော legacy lines၊ deleted products/warehouses ကိုမရွေးစေပါ။
အရင် Posted returns sum ကိုနုတ်ပြီး remaining > 0 သာပြသည်။ Draft returns မနုတ်ပါ။
This read က stock reservation မလုပ်လို့ UI မှာမြင်ပြီးနောက် quantity ပြောင်းသွားနိုင်သည်။
Create/post handlers က လက်ရှိ quantity ကိုပြန်စစ်သည်။ Stock availability ကို Post မှစစ်သည်။

`Sum(returnItem => (decimal?)returnItem.ReturnQuantity) ?? 0m` က prior returns မရှိသော
line အတွက် 0 ပေးသည်။ Correlated query က source receipt item တစ်ခုစီနဲ့ဆိုင်တဲ့
posted quantities ကို SQL မှာတွက်သည်။ Results ကို client က GRNs အားလုံး fetch ပြီး
ခန့်မှန်းနုတ်ခြင်းမဟုတ်ပါ။ Source list pagination ကလည်း Products pattern ကိုသုံးသည်။
Schema အသစ်မထည့်လို့ migration မလိုပါ။

### Request data နဲ့ date/quantity presentation

Save request မှာ SupplierId၊ ReturnDate၊ optional Reason၊ key နဲ့
`{goodReceiptItemId, returnQuantity}` lines ပဲပို့သည်။ Form ရဲ့ local date ကို
`yyyy-MM-ddT00:00:00` အဖြစ်ပို့ပြီး `toISOString()` ဖြင့် UTC ပြောင်းခြင်းမလုပ်ပါ။
Backend audit values က existing DateTime.Now ကိုဆက်သုံးသည်။

```html
{{ line.returnQuantity | number:'1.0-4' }}
```

UI က 50.0000 → 50၊ 2.5000 → 2.5၊ 2.1250 → 2.125 ပြသည်။ InputNumber မှာလည်း
minFractionDigits=0/maxFractionDigits=4 ဖြစ်သည်။ DB decimal precision မပြောင်းပါ။
PrimeNG form components မှာ size တူအောင် default size သုံးပြီး custom height မသတ်မှတ်ပါ။

### Save idempotency နဲ့ failure recovery

Save ပထမနှိပ်မှ key ကို crypto.randomUUID နဲ့ဖန်တီးပြီး payload snapshot သိမ်းသည်။
Saving အတွင်း form/line controls ပိတ်ပြီး duplicate clicks ပိတ်သည်။ Network/5xx
ကြောင့် save result မသေချာလျှင် fields ဆက်ပိတ်ထားပြီး Retry Save ကမူလ payload/key
ကိုပဲပြန်ပို့သည်။ Data ပြောင်းပြီး same key ပြန်ပို့သည့် hash conflict ကိုရှောင်သည်။
400/422 rejection ရလျှင် input ပြန်ပြင်ခွင့်ပေးသည်။ Unconfirmed save dialog ကိုပိတ်လျှင်
နောက် draft အသစ်မဖန်တီးမီ list မှာရှိ/မရှိစစ်ရန် သတိပေးပြီး list refresh လုပ်သည်။
Tab reload ပြီးနောက် ဒီ pending payload ကို restore မလုပ်သေးပါ။

Save success ရလျှင် list refresh နဲ့ saved detail ပြသည်။ Draft ရဲ့ Post button က
confirmation ပြပြီးမှ POST /{id}/post ခေါ်သည်။ Post result မသေချာလျှင် list/detail
refresh လုပ်ပြီး backend Posted status ကိုပြန်ကြည့်နိုင်သည်။ UI က stock ကိုကိုယ်တိုင်
တွက်နုတ်ခြင်းမလုပ်ပါ။ Posted row မှာ Post button မပြတော့ပါ။

### Change detection / subscription lifecycle

Component က OnPush သုံးသည်။ HTTP subscribe callbacks မှ state ပြောင်းပြီး
`cdr.markForCheck()` နဲ့ next change-detection pass မှာ render ပြန်လုပ်ရန်မှတ်သားသည်။
`detectChanges()` ကိုနေရာတိုင်းမသုံးပါ။ `takeUntilDestroyed(this.destroyRef)` က
page ထွက်သည့်အခါ subscriptions ကိုပိတ်ပေးသည်။ သီးခြား Destroy Subject မဖန်တီးထားပါ။

### Verification

Backend နဲ့ Angular builds စစ်ထားသည်။ Existing API process က output files ကို lock
ထားနိုင်လို့ backend build ကို temporary output folder နဲ့စစ်ထားသည်။ မင်း run ထားတဲ့
API ကိုမပိတ်ထားပါ။ New returnable-items endpoint ကိုသုံးရန် API restart လိုနိုင်သည်။
ဒီ session ရဲ့ browser tool က logged-in Chrome tab ကိုမမြင်ရလို့ live light/dark၊
mobile dialog နဲ့ actual create/post interaction ကိုအတည်မပြုရသေးပါ။ User ရဲ့
စမ်းသပ်မှုနောက်မှလုပ်ရန် preference အတိုင်း DB-changing UI/API tests မလုပ်ထားပါ။

နောက်မှစစ်ရန်: supplier ပြောင်းခြင်း၊ GRN paging across selection၊ decimal quantity၊
empty/error states၊ filters/sort၊ save retry၊ Post confirmation/stock effect၊ mobile
horizontal tables နဲ့ dark overlays။

Reconstruction: Supplier ရွေးချိန်ကနေ source query → Add line → quantity ပြောင်း →
create payload ထုတ်သည်အထိ flow ကို code မကြည့်ဘဲပြန်ရှင်းပါ။

## ၂၂။ Products ပုံစံနဲ့ toolbar / code format ညှိခြင်း

Purchase Return heading နဲ့ description ကိုဖယ်ပြီး Create Return ကိုဘယ်ဘက်၊ search
icon ပါသော input ကိုညာဘက်မှာ Products toolbar ပုံစံအတိုင်းထားသည်။ Search က Enter
နှိပ်လျှင် server query ပို့သည်။ Supplier/status/date filters ကို အောက်တစ်တန်းမှာထားသည်။

Component က explicit boolean/string/number state types၊ `isLoading`၊
`isSubmitting`၊ `formModalVisible`၊ `purchaseReturnForm`၊ `create()`၊ `onSubmit()`၊
`loadMasterData()` နဲ့ constructor service names ကို ရှိပြီးသား Products/Supplier
ရေးပုံအတိုင်းညှိထားသည်။ Service က method တစ်ခုစီမှာ local `const url` သုံးသည်။
Imports/component metadata၊ constructor၊ methods နဲ့ models ကို multi-line spacing
ဖြင့်ဖတ်လွယ်အောင်စီထားသည်။ Return feature files သုံးခုကိုသာ scoped formatter နဲ့စစ်သည်။

Return backend pagination၊ stale response protection၊ save retry key/payload၊
Post confirmation နဲ့ quantity rules မပြောင်းပါ။ Products ရဲ့ browser-side filter
ကိုကူးမသုံးဘဲ existing Return server-side search ကိုဆက်သုံးသည်။

## ၂၃။ Return No ကို frontend မှ generate လုပ်ခြင်း

နောက်ဆုံးရွေးချယ်ထားသောနည်းသည် frontend numbering ဖြစ်သည်။ Format က
PR-2026-00000001 ဖြစ်ပြီး Create ဖွင့်ချိန်မှာ လက်ရှိနှစ်အတွက် နောက်ဆုံးသုံးပြီးသား
နံပါတ်ကို GET /api/process/purchase-returns/last-number?year=2026 ဖြင့်ယူသည်။
Backend က company တူသော deleted rows ပါထည့်တွက်၍ နောက်ဆုံးနံပါတ်စာသားကိုသာပြန်ပေးသည်။
နံပါတ်တစ်တိုးခြင်းနှင့် format လုပ်ခြင်းကို frontend ကလုပ်သည်။

```typescript
const nextNumber = lastNumber + 1;
this.formReturnNo = `PR-${year}-${String(nextNumber).padStart(8, '0')}`;
```

String(nextNumber) က number ကိုစာသားပြောင်းသည်။ padStart(8, '0') က digit 8 လုံး
မပြည့်သေးလျှင်ရှေ့သုညဖြည့်သည်။ year သည် Create ဖွင့်ချိန် local current year ဖြစ်သည်။
ReturnDate ကိုပြန်ပြောင်းလည်း numbering year မပြောင်းပါ။ Year/company တူသော
သုံးပြီးသားနံပါတ်မရှိလျှင် 00000001 ကစသည်။

Input ကို disabled ပြထားပြီး Create request ထဲ returnNo ပို့သည်။ Number ဖတ်နေချိန်
Save ပိတ်ထားသည်။ API error ဖြစ်လျှင်အလွတ်/ခန့်မှန်းနံပါတ်မသုံးဘဲ error ပြသည်။
Dialog ပိတ်ပြီးနောက်မှရောက်လာသော response က dialog အသစ်ကိုမပြောင်းစေရန်
numberRequest counter နှင့်စစ်သည်။ Edit မှာ latest number API မခေါ်ဘဲ မူလနံပါတ်ပြသည်။

Save မှာ backend က company အတွင်းနံပါတ်ထပ်မထပ် deleted rows ပါစစ်ပြီးသိမ်းသည်။
Runtime SQL sequence ကိုမသုံးတော့ပါ။ ရှိပြီးသား sequence migration/schema ကို
history မပျက်စေရန်ချန်ထားပြီး ဒီပြောင်းလဲမှုအတွက် migration အသစ်မလိုပါ။

တစ်ပြိုင်နက် Create ဖွင့်ထားသူနှစ်ယောက် နံပါတ်တူနိုင်သည်။ Save မှာ duplicate check /
unique index ကတားသည်။ နံပါတ်သစ်ယူလိုလျှင် Create ကိုပြန်ဖွင့်ရန် error ပြသည်။
ဒါသည် reservation မဟုတ်သဖြင့် frontend မှထုတ်သောနံပါတ်ကို ကြိုအာမခံမထားပါ။

Idempotency hash မှာ returnNo ပါပြီး retry က မူလနံပါတ်နှင့် request/key ကိုဆက်သုံးသည်။

## ၂၄။ GRN number နှင့် form layout

GRN Receipt No ကို ယခင် GRN generation ပုံစံအတိုင်းထားပြီး disabled control အဖြစ်
ဆက်ထားသည်။ GRN Save မှာ getRawValue() သုံး၍ disabled number ပါအောင်ယူသည်။
Return No ကို frontend က format လုပ်ပြီး create request ထဲထည့်ပို့သည်။

SCSS ဖြုတ်ပြီး `.field` class ကိုဆက်သုံးလျှင် column layout rule မရှိတော့ပါ။
`flex flex-col gap-1` ဖြင့် label အပေါ်၊ input အောက်စီပြီး `w-full` ဖြင့်အကျယ်ဖြည့်သည်။
Reason textarea မှာ pTextarea နှင့် TextareaModule ကိုသုံးသည်။ Custom SCSS မထည့်ပါ။

## ၂၅။ Draft Update နှင့် Soft Delete

Draft ကိုသာပြင်/ဖျက်နိုင်သည်။ Posted Return ကို backend ကပိတ်ထားပြီး UI မှာ
Edit/Delete နှိပ်လျှင် error toast ပြသည်။ ReturnNo၊ မူလ create idempotency key/hash နှင့် stock ကို
Update ကမပြောင်းပါ။ Schema ပြောင်းခြင်းမရှိလို့ ဒီ feature အတွက် migration အသစ်မလိုပါ။

### Update request → handler → response

`PUT /api/process/purchase-returns/{id}` က UpdatePurchaseReturnCommand ကိုပို့သည်။
Body မှာ id၊ supplierId၊ returnDate၊ returnReason နှင့် items ပါသည်။ ReturnNo နှင့်
idempotencyKey မပါပါ။ Route id နှင့် body id ကိုက်ရန် controller ကစစ်သည်။

```csharp
if (purchaseReturn.Status != PurchaseReturnStatus.Draft)
    throw new InvalidOperationException("Only draft purchase returns can be updated.");

purchaseReturn.SupplierId = request.SupplierId;
purchaseReturn.ReturnDate = request.ReturnDate;
purchaseReturn.ReturnReason = request.ReturnReason?.Trim();
```

Entity ကို tracking query နဲ့ယူထားသောကြောင့် properties ပြောင်းပြီး SaveChangesAsync
ခေါ်လျှင် EF ကပြောင်းသည့် values ကိုသိမ်းသည်။ Include(pr => pr.Items) က items ပါယူသည်။
CompanyId နှင့် DeletedOn စစ်ထားလို့ ကိုယ့် company ရဲ့ active Return ကိုသာရသည်။
Transaction အတွင်း Draft စစ်ခြင်းနှင့် save လုပ်ခြင်းကိုအတူလုပ်၍ Post/Delete နှင့်
တစ်ပြိုင်နက်တည်းလုပ်သောအခါ stock flow နှင့်ခွဲထွက်မသွားစေပါ။ Conflict ဖြစ်လျှင်
request fail နိုင်ပြီး list ပြန်ဖတ်၍လက်ရှိအခြေအနေစစ်ရန်လိုသည်။

Validator မှာ supplier၊ date၊ reason max length၊ items အနည်းဆုံးတစ်ကြောင်း၊
GRN line မထပ်ရန်နှင့် quantity > 0 / decimal(18,4) ကိုစစ်သည်။ Handler မှာ source
GRN သည် received ဖြစ်ပြီး supplier/company ကိုက်ရန်၊ posted ပြန်ပို့ပြီးသားနုတ်၍
ကျန်သည့်ပမာဏထက်မကျော်ရန် စစ်သည်။ Future date restrictions နှင့် stock check အသစ်
မထည့်ပါ။ Stock availability ကို Post မှာစစ်သောမူလ flow အတိုင်းထားသည်။

Item အသစ်ဆို Add လုပ်သည်။ ရှိပြီးသားဆို quantity ပြင်သည်။ Request ထဲမပါတော့သော
line ကို Remove လုပ်သည်။ အရင်ဖယ်ထားသော GRN line ကိုပြန်ရွေးလျှင် မူလ row ကိုပြန်ယူပြီး
DeletedOn/DeletedBy ကို null လုပ်သည်။ Database unique index က ReturnId + GRN item
ကို soft-deleted rows အပါအဝင်စစ်လို့ row အသစ်ထပ်မထည့်ရန်လိုသည်။

### Delete နှင့် soft delete audit

`DELETE /api/process/purchase-returns/{id}` က DeletePurchaseReturnCommand ကိုပို့သည်။
Company/active record နှင့် Draft စစ်ပြီး header/items ကို Remove လုပ်သည်။

```csharp
_context.PurchaseReturnItems.RemoveRange(
    purchaseReturn.Items.Where(i => !i.DeletedOn.HasValue));
_context.PurchaseReturns.Remove(purchaseReturn);
await _context.SaveChangesAsync(cancellationToken);
```

ဒီ project ၏ DbContext.ApplyAuditValues တွင် EntityState.Deleted ကို Modified ပြောင်းပြီး
DeletedOn = DateTime.Now နှင့် DeletedBy ကိုဖြည့်သည်။ ထို့ကြောင့် SQL row ကိုဖျက်ခြင်းမဟုတ်ဘဲ
soft delete ဖြစ်သည်။ Draft က stock မလျော့ရသေးလို့ stock reversal မလိုပါ။

### Angular shared form

`edit(row)` က getById ဖြင့် နောက်ဆုံး detail ကိုယူပြီး form reset နှင့် lines ဖြည့်သည်။
Details response ထဲ receivedQuantity / returnableQuantity ထည့်ထား၍ ယခင်ရွေးထားသော
items ကို source list စာမျက်နှာတစ်ခုစီလိုက်ရှာစရာမလိုပါ။ Returnable quantity သည်
received quantity မှ posted returns အားလုံးနုတ်ထားသောပမာဏဖြစ်သည်။

Supplier ကိုပြောင်းလျှင် onSupplierChange က lines ရှင်းပြီး supplier အသစ်၏ GRN lines
ကိုပြန်ရှာသည်။ ReturnNo ကို disabled input တွင် မူလနံပါတ်အဖြစ်ပြသည်။

```typescript
export interface UpdatePurchaseReturnRequest
  extends Omit<CreatePurchaseReturnRequest, 'idempotencyKey' | 'returnNo'> {
    id: string;
}
```

Omit<T, 'field'> က type T ထဲမှ field တစ်ခုဖယ်ထားသော type ကိုတည်ဆောက်သည်။ extends က
ကျန်သော supplier/date/reason/items ပုံစံကိုပြန်သုံးပြီး id ထည့်သည်။ Runtime data ကိုတော့
object destructuring ဖြင့် idempotencyKey နှင့် returnNo ဖယ်ပြီး PUT ပို့သည်။ Create အတွက်မူမူလ
POST နှင့် immutable pending request/key retry flow ကိုဆက်သုံးသည်။ Update retry
သည် တူညီသော draft fields/items ကိုအစားထိုးသိမ်းသည်၊ number မထုတ်၊ stock မလျော့ပါ။

Delete button သည် confirm dialog အရင်ပြပြီး accept မှသာ service.delete ကိုခေါ်သည်။
Success မှာ list ပြန်ဖတ်၊ လက်ရှိဖွင့်ထားသော deleted detail ကိုပိတ်သည်။

နောက်မှစမ်းရန်: Draft metadata/quantity ပြင်ခြင်း၊ supplier ပြောင်းခြင်း၊ item ဖယ်ပြီး
ပြန်ထည့်ခြင်း၊ Draft delete နှင့် list မှပျောက်ခြင်း၊ Posted PUT/DELETE ပိတ်ခြင်း၊
Update/Delete ပြီး stock မပြောင်းခြင်း။ Automated tests/API data mutations မလုပ်ထားပါ။

ပြန်ရေးလေ့ကျင့်ရန်: Delete handler ၏ Draft စစ်ခြင်းမှ SaveChangesAsync အထိကို
ကိုယ်တိုင်ပြန်ရေးပြီး Remove က ဒီ project မှာ soft delete ဖြစ်ပုံကိုရှင်းပြပါ။

## ၂၆။ Products ကဲ့သို့ row selection

Purchase Returns list တွင် single row selection ကိုထည့်သည်။

```html
<p-table [value]="returns" [(selection)]="selectedPurchaseReturn"
  selectionMode="single" dataKey="id">
  <!-- body row -->
  <tr [pSelectableRow]="row">...</tr>
</p-table>
```

`[(selection)]` သည်ရွေးထားသော row ကို TypeScript property နှင့် two-way ချိတ်သည်။
`[pSelectableRow]="row"` ကထို row ကို mouse/keyboard ဖြင့်ရွေးနိုင်စေသည်။ Products
ကဲ့သို့ ရှေ့ဆုံး # column တွင် circle/check-circle icon ပြသည်။ Data page/filter
ပြန်ဖတ်တိုင်း selection ကို null ရှင်း၍ အဟောင်း row ကိုရွေးထားသလိုမပြစေပါ။
Return number link နှင့် action buttons တွင် click propagation ရပ်ထားသဖြင့် detail၊
edit၊ delete၊ post လုပ်ခြင်းနှင့် row ရွေးခြင်းသည် တစ်ချက်နှိပ်ရာတွင်ရောမသွားပါ။
ဒီပြောင်းလဲမှုက list row selection သာဖြစ်ပြီး bulk post/delete မထည့်ပါ။

## ၂၇။ Posted Return ကိုပြင်/ဖျက်ရန်ကြိုးစားလျှင် error ပြခြင်း

Edit/Delete ကို Create Return splitbutton dropdown menu မှာထားပြီး ရွေးထားသော row အတွက်လုပ်သည်။
edit(row) / delete(row) ၏အစတွင် status စစ်ပြီး Draft မဟုတ်လျှင် error toast ပြ၍
return လုပ်သည်။ Edit dialog / delete confirmation မဖွင့်၊ API request မပို့ပါ။
Backend Draft-only check ကိုဆက်ထား၍ API ကိုတိုက်ရိုက်ခေါ်လည်းပိတ်သည်။ Post button
ကိုမူ Draft မှာသာပြသည်။

## ၂၈။ Edit/Delete ကို SplitButton menu မှခေါ်ခြင်း

List row နှင့် Detail မှ သီးခြား Edit/Delete ခလုတ်များကိုဖယ်ထားသည်။ Products/GRN
ကဲ့သို့ Create Return splitbutton ၏ dropdown တွင် Edit/Delete ကိုသုံးသည်။
Table row ရွေးပြီး menu action နှိပ်လျှင် selectedPurchaseReturn ကို edit()/delete()
၏ default argument အဖြစ်ယူသည်။ Row မရွေးလျှင် selection message၊ Posted ရွေးထားလျှင်
မူလ error toast ကိုပြသည်။ Draft ဖြစ်လျှင်သာ edit dialog/delete confirmation ဆက်လုပ်သည်။

## ၂၉။ Return No အလွတ်ဖြစ်လျှင် API version စစ်ရန်

2026-10-19 တွင် run နေသော API ၏ /openapi/v1.json ကိုစစ်ရာ last-number endpoint
မပါသေးသည်ကိုတွေ့သည်။ Source တွင် endpoint ပါသော်လည်း API အဟောင်းကို run ထားပါက
frontend number request ကမအောင်မြင်ပါ။ API ကို Stop → Rebuild → Run ပြန်လုပ်ရမည်။
နံပါတ်ယူမရသော error ကို form အောက်ဆုံးတစ်နေရာတည်းမဟုတ်ဘဲ Return No field အောက်မှာပါ
ပြထားသည်။ HTTP 404 ဆို API rebuild/restart လုပ်ရန် တိုက်ရိုက်ညွှန်ပြသည်။
မရသေးသောအခါ 00000001 ကိုခန့်မှန်းဖြည့်ခြင်းမလုပ်ပါ။
