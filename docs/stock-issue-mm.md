# Stock Issue — ပစ္စည်းထုတ်ပေးခြင်း

Warehouse မှ Department ဆီ ပစ္စည်းထုတ်ပေးတဲ့ voucher ဖြစ်တယ်။
တစ်စောင်မှာ Warehouse တစ်ခု၊ Department တစ်ခုနဲ့ Product အများကြီး ပါနိုင်တယ်။

## Flow

`Create → Draft → Post → Posted`

- Draft သိမ်းတာ၊ ပြင်တာ၊ ဖျက်တာမှာ stock မပြောင်းဘူး။
- Post လုပ်မှ warehouse stock လျော့ပြီး OUT transactions သိမ်းတယ်။
- Posted ဖြစ်ပြီးသားကို Edit/Delete မရဘူး။
- Post request ထပ်လာရင် သိမ်းထားတဲ့ result ကိုပဲ ပြန်ပေးတယ်။ Stock ထပ်မလျော့ဘူး။

## Backend code ကို ဖတ်ရမယ့်အစဉ်

Paths တွေက `src/InventoryManagementSystem.Application/Process/StockIssues` အောက်မှာရှိတယ်။

1. `DTOs` — API က ပြန်ပို့တဲ့ header နဲ့ item အချက်အလက်။
2. `Commands/CreateStockIssueDraft` — input validation၊ active records စစ်ခြင်း၊ Draft သိမ်းခြင်း။
3. `Queries/GetStockIssueById` — voucher တစ်စောင်ကို `AsNoTracking()` နဲ့ဖတ်ပြီး DTO ပြောင်းခြင်း။
4. `Queries/GetStockIssues` — filter → Count → OrderByDescending → Skip/Take → DTO အစဉ်နဲ့ list ဖတ်ခြင်း။ Products pagination ပုံစံကို သုံးတယ်။
5. `Commands/UpdateStockIssueDraft` — Draft ကိုသာပြင်ပြီး ရှိပြီးသား item rows ကို ပြန်သုံးတယ်။ ဖြုတ်ထားတဲ့ items ကို soft delete လုပ်တယ်။
6. `Commands/DeleteStockIssueDraft` — Draft header နဲ့ items ကို soft delete လုပ်တယ်။
7. `Commands/CreateStockIssuePost` — stock စစ်၊ လျော့၊ ledger သိမ်း၊ Posted ပြောင်းခြင်း။

`CompanyId` filter နဲ့ new records တွေမှာ CompanyId ဖြည့်တာကို DbContext က ကိုင်တွယ်တယ်။
Audit dates တွေအတွက် လက်ရှိ project အတိုင်း `DateTime.Now` သုံးတယ်။

### Remove လို့ရေးထားပေမယ့် soft delete ဖြစ်ရတဲ့အကြောင်း

Delete handler မှာ `Remove()` / `RemoveRange()` သုံးထားတယ်။
`InventoryManagementDbContext.Custom.cs` ရဲ့ `SaveChangesAsync()` က audit fields ရှိတဲ့ entities တွေကို အောက်ပါအတိုင်း ကိုင်တွယ်တယ်။

```csharp
case EntityState.Deleted:
    entry.State = EntityState.Modified;
    entity.DeletedOn = DateTime.Now;
    entity.DeletedBy = currentUserId;
    break;
```

`Deleted` ကနေ `Modified` ပြောင်းလိုက်တာကြောင့် SQL DELETE အစား UPDATE ထွက်တယ်။
Header နဲ့ items တွေ database ထဲမှာ ကျန်နေပြီး list/detail queries ရဲ့ `!DeletedOn.HasValue` က မပြအောင်ဖယ်ထားတယ်။
Handler တိုင်းမှာ DeletedOn/DeletedBy ထပ်ဖြည့်ဖို့မလိုဘူး။

## Posting မှာ ဘာတွေဖြစ်သလဲ

```csharp
await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
```

`BeginTransactionAsync` ရဲ့ လက်ရှိ default isolation က Serializable ဖြစ်တယ်။
`await using` က transaction ကို အလုပ်ပြီးချိန်မှာ dispose လုပ်ပေးတယ်။
Stock မလုံလောက်လို့ exception ဖြစ်ရင် Commit မရောက်ဘဲ အဲဒီ transaction ရဲ့ changes တွေ rollback ဖြစ်မယ်။

Product တစ်ခုချင်းစီအတွက် ရွေးထားတဲ့ warehouse ရဲ့ balance ကိုဖတ်ပြီး quantity လျော့တယ်။
Items ရဲ့ Quantity က လက်ရှိ warehouse balance သိမ်းထားတဲ့ product base unit အတိုင်းဖြစ်တယ်။
ဒီ feature မှာ သီးခြား UOM conversion မလုပ်ဘူး။

ပြီးရင် `StockTransaction` မှာ `StockIssueId`, `StockIssueItemId` နဲ့ချိတ်ထားတဲ့ OUT rows သိမ်းတယ်။
Item တစ်ခုမှာ movement တစ်ခုပဲရှိဖို့ unique index ထားတယ်။
ဒီ rows ကို Stock Transactions page ကနေ သီးခြား Edit/Delete မရအောင် existing mutation guard မှာ စစ်ထားတယ်။

`Product.CurrentStock` ကို warehouse balances တွေရဲ့ စုစုပေါင်းနဲ့ ပြန်ညှိတယ်။
Stock updates၊ ledger rows နဲ့ Posted status အားလုံး transaction တစ်ခုအတွင်း Commit လုပ်တယ်။

## API

Base route: `/api/process/stock-issues`

| Method | Route | လုပ်ဆောင်ချက် |
|---|---|---|
| GET | `/` | Search၊ Warehouse/Department/Status၊ startDate/endDate filters၊ pagination |
| GET | `/{id}` | Detail |
| POST | `/` | Create Draft |
| PUT | `/{id}` | Update Draft |
| DELETE | `/{id}` | Soft delete Draft |
| POST | `/{id}/post` | Post and deduct stock |

## Client

`stock-issue.ts` ကို Supplier component ပုံစံအတိုင်း state → form → constructor/menu → load → CRUD methods အစဉ်နဲ့ထားတယ်။

- Create Issue dropdown မှာ Edit/Delete/Post ရှိတယ်။ Row ကိုရွေးပြီး သုံးနိုင်တယ်။ Detail button သို့မဟုတ် Issue No ကိုနှိပ်ရင် voucher အပြည့်ပြတယ်။
- Table နဲ့ Detail မှာ Created On/By၊ Updated On/By နဲ့ Posted On/By ပြတယ်။
- Issue No ကို frontend က generate လုပ်ပြီး disabled input မှာပြတယ်။ Backend မှာ company အတွင်း မထပ်ဖို့စစ်တယ်။
- Product ရွေးတာ သို့မဟုတ် Barcode/SKU scan ပြီး Enter နှိပ်တာနဲ့ item ထည့်နိုင်တယ်။
- Warehouse ပြောင်းရင် row တစ်ခုချင်းရဲ့ current balance ကို ပြန်ဖတ်တယ်။
- Balance မသိသေးရင် `-` ပြတယ်။ Post လုပ်ချိန်မှာ backend က stock ကိုပြန်စစ်တယ်။
- Start Date နဲ့ End Date က Issue Date ကိုစစ်တယ်။ ရက်တစ်ဖက်တည်းရွေးလည်းရပြီး End Date တစ်ရက်လုံး ပါဝင်တယ်။ Clear နှိပ်ရင် date filters ပါရှင်းတယ်။
- `number:'1.0-4'` က `5.0000` ကို `5`၊ `5.25` ကို `5.25` လို့ပြတယ်။

```html
[(selection)]="selectedStockIssue"
```

`[(...)]` က two-way binding ဖြစ်တယ်။ Table မှာ row ရွေးတာနဲ့ component ရဲ့ `selectedStockIssue` ပြောင်းတယ်။
Edit/Delete/Post methods တွေက အဲဒီ selected record ကိုသုံးတယ်။

HTTP callback ပြီးချိန် `cdr.markForCheck()` က ပြောင်းထားတဲ့ data ကို UI မှာပြန်စစ်ဖို့ အမှတ်ပေးတယ်။
`takeUntilDestroyed` က page ကထွက်သွားရင် subscriptions တွေကို ပိတ်ပေးတယ်။

## ကိုယ်တိုင်စစ်ကြည့်ရန်

1. Stock 20 ရှိတဲ့ product ကို quantity 5 နဲ့ Draft သိမ်းပါ။ Stock 20 ပဲရှိရမယ်။
2. Draft quantity ကို 4 ပြင်ပါ။ Detail မှာ 4 ပြပြီး stock မပြောင်းရဘူး။
3. Post လုပ်ပါ။ Stock 16၊ OUT row တစ်ခုနဲ့ Posted status ဖြစ်ရမယ်။
4. Post ထပ်လုပ်ပါ။ Stock 16 ပဲရှိပြီး movement မထပ်ရဘူး။
5. Posted voucher ကို Edit/Delete လုပ်ပါ။ Error message ပြရမယ်။
6. မလုံလောက်တဲ့ quantity နဲ့ Post လုပ်ပါ။ Draft အဖြစ်ကျန်ပြီး stock/ledger မပြောင်းရဘူး။
7. Company ပြောင်းပြီး အရင် company voucher ကိုဖတ်ပါ။ မတွေ့ရဘူး။

နားလည်မှုလေ့ကျင့်ရန်—Create Draft က stock မပြောင်းဘဲ Post မှာသာပြောင်းတဲ့အကြောင်းနဲ့
Post handler ထဲက SaveChanges နှစ်ကြိမ်ကို transaction တစ်ခုထဲထားတဲ့အကြောင်း ပြန်ရှင်းပြကြည့်ပါ။
