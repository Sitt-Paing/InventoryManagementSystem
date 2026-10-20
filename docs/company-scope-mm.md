# လက်ရှိ company တစ်ခုအတွင်း inventory data ကန့်သတ်ခြင်း

## မူလနှင့်လက်ရှိ filter

မူလ IsSuperAdminUser || condition က SuperAdmin နှင့် user/context မရှိသော request
ကို company စစ်ခြင်းမှကျော်ခွင့်ပေးသည်။ လက်ရှိတော့ company-owned entities အားလုံးတွင်
အောက်ပါပုံစံကိုသုံးသည်။ EmailOutbox filter ကိုပါညှိထားသည်။

```csharp
public int? CurrentCompanyId => _currentUserService?.CompanyId;

.HasQueryFilter(e => CurrentCompanyId.HasValue && e.CompanyId == CurrentCompanyId);
```

`int?` က null ဖြစ်နိုင်သော integer ဖြစ်သည်။ `?.` က service မရှိလျှင် null ပြန်သည်။
HasValue က null မဟုတ်ကြောင်းစစ်သည်။ `&&` က condition နှစ်ခုလုံး true ဖြစ်ဖို့လိုသည်။
`e` ကဖတ်မည့် entity row ကိုဆိုလိုသည်။ CurrentCompanyId သည် context instance မှလာသော
လက်ရှိ request ၏ company_id claim ဖြစ်ပြီး company တစ်ခု၏ ID ကို model ထဲ hardcode
မထားပါ။ Settings page နှင့် company selection endpoint ကို docs/company-settings-mm.md တွင်ဆက်ရှင်းထားသည်။

| လက်ရှိ request | ပုံမှန် inventory query result |
|---|---|
| Company A user | A rows သာ |
| Company A SuperAdmin | A rows သာ |
| Company မရှိသော user/context | Empty၊ CompanyId=null rows ပါမယူ |

Department handler တွင် `.Where(x => !x.DeletedOn.HasValue)` သာရေး၍ company filter
ကို DbContext ကထပ်ပေါင်းပေးသည်။ Soft-delete စစ်ခြင်းက company filter နှင့်သီးခြားဖြစ်သည်။
Department တစ်ခုရွေး၍ update/delete လုပ်ရာတွင်လည်း query ကလက်ရှိ company ထဲက row
ကိုသာရှာပေးသည်။ တခြား features ၏ explicit CompanyId checks ကိုဤအဆင့်မှာမဖယ်ထားပါ။

## အသစ်သိမ်းခြင်း

```csharp
if (!currentCompanyId.HasValue)
    throw new InvalidOperationException("Select a company before creating inventory data.");
if (companyEntity.CompanyId.HasValue && companyEntity.CompanyId != currentCompanyId)
    throw new InvalidOperationException("Inventory data must belong to the current company.");

companyEntity.CompanyId = currentCompanyId.Value;
```

DbContext.SaveChangesAsync တွင် Added ဖြစ်သော IMustHaveCompany entity များအတွက်
တစ်နေရာတည်းလုပ်သည်။ `.Value` သည် nullable integer ၏တန်ဖိုးကိုယူခြင်းဖြစ်ပြီး အပေါ်မှာ
HasValue စစ်ပြီးမှသုံးသည်။ Department create handler က CompanyId ကိုမပို့တော့ပါ။
Business audit timestamps သည် DateTime.Now အတိုင်းဖြစ်သည်။

## ဆက်စပ် flow များ

Login သည်သီးခြား identity store ကိုသုံးသည်။ Company active check သည်ရှိပြီးသား
IgnoreQueryFilters နှင့် explicit company ID ကိုသုံးသည်။ Company account registry
(Company entity) ကို inventory-owned filter ထဲမထည့်ထားပါ။ User ရွေးထားသော account
ကို session ထဲထည့်ပေးသော settings/select-company flow ကိုထည့်ပြီးဖြစ်သည်။

Background email worker သည်ရှိပြီးသား IgnoreQueryFilters ကိုသုံး၍ pending outbox
rows ကိုလုပ်ဆောင်သည်။ Worker တွင်လက်ရှိ logged-in company မရှိသော်လည်း လုပ်ဆောင်ရန်
ရည်ရွယ်ထားသောသီးခြား flow ဖြစ်သည်။ Filter ပြောင်းခြင်းကြောင့်ဤ flow ကိုမပိတ်ထားပါ။
IgnoreQueryFilters ကို inventory API handlers ထဲထည့်၍ bypass မလုပ်ရပါ။

2026-10-20 တွင် user အတည်ပြုချက်ဖြင့် legacy inventory CompanyId=null rows ၁၂၀ ကို company 1 ဖြည့်ပြီးဖြစ်သည်။ devadmin ၏ permanent CompanyId/claims မပြင်ပါ။
Company မချိတ်ထားသော account များ inventory list အလွတ်ရမည်။ Schema မပြောင်းလို့
migration အသစ်မလိုပါ။ API ကို rebuild/restart လုပ်ပြီးမှ runtime တွင်သက်ရောက်မည်။

ကိုယ်တိုင်စမ်းရန်: A/B company accounts ဖြင့် Department/Supplier စာရင်းကွဲခြင်း၊
SuperAdmin တစ်ခု၏ company အပြင် data မရခြင်း၊ company မရှိလျှင် empty list နှင့်
create error ရခြင်း။ ဒီအဆင့်တွင် account/data ပြင်ခြင်း၊ live mutation tests မလုပ်ပါ။

ပြန်ရေးလေ့ကျင့်ရန်: Company A ဖြစ်လျှင် A rows ပဲရပြီး null-company rows ကိုမယူစေသော
HasQueryFilter expression ကိုကိုယ်တိုင်ပြန်ရေးကာ && နှင့် HasValue ကိုရှင်းပြပါ။
