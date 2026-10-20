# Company Settings — Login နှစ်မျိုးနှင့် legacy data

## လက်ရှိအလုပ်လုပ်ပုံ

| Account | Login ပြီးနောက် |
|---|---|
| devadmin / Administrator / SuperAdmin | /settings မှ company ရွေးပြီး Dashboard ဝင်ရန် |
| CompanyAdmin နှင့် ပုံမှန် company user | သူ့ account ၏ company ထဲ Dashboard တိုက်ရိုက်ဝင်ရန် |

Settings ကို login ပြီးနောက် redirect သို့မဟုတ် header user menu → System Settings မှ
ဝင်နိုင်သည်။ Sidebar/User Management menu ထဲမထည့်ထားပါ။ ပုံမှန် user အတွက်လက်ရှိ company အမည်ကိုသာပြပြီး ပြောင်းခွင့်မပေးပါ။
Company ရွေးပြီးနောက် inventory page caches မကျန်ရန် Dashboard သို့သွားပြီး reload လုပ်သည်။
Existing Supplier format အတိုင်း constructor DI၊ FormBuilder၊ loadData/onSubmit၊
service local URL၊ subscribe next/error နှင့် PrimeNG controls ကိုသုံးထားသည်။

## Legacy database backfill

User အတည်ပြုချက်ဖြင့် 2026-10-20 တွင် business tables ၁၈ ခု၏ CompanyId=null rows
၁၂၀ ကို company 1 (TestCom) ဖြည့်ထားသည်။ ရှိပြီးသား non-null CompanyId ကိုမပြောင်းပါ။
AspNetUsers၊ user claims၊ passwords နှင့် roles ကိုမပြောင်းပါ။ devadmin သည် permanent
CompanyId=null အတိုင်းထားပြီး login ပြီးမှ company ရွေးရသည်။

Local-only script (မ commit လုပ်ထားပါ): scripts/data-fixes/20261020_assign_legacy_inventory_company_1.sql

Script က active company 1 ရှိကြောင်းစစ်ပြီး transaction တစ်ခုအတွင်းပြောင်းသည်။
SET XACT_ABORT ON ကြောင့် SQL error ဖြစ်လျှင် transaction rollback ဖြစ်သည်။
OUTPUT ဖြင့်ပြောင်းသော table/row ID များကိုပြန်ပေးသည်။ ပြောင်းပြီး read-only စစ်ရာ
CompanyId=null inventory rows 0၊ PO/GRN/Return header-item company mismatch 0 ရသည်။

Changed-row audit ကို development machine ၏ TEMP folder တွင်သိမ်းထားသည်။
ဖိုင်: inventory-company1-backfill-bd3d556ea4bf449990c403e2c45536ff.csv
ဒီ script ကို application startup မှ အလိုအလျောက်မခေါ်ပါ။ Schema migration အသစ်မလိုပါ။

## Backend login နှင့် JWT

IdentityService.LoginAsync တွင် roles ကိုစစ်သည်။ Platform administrator သည်
ရွေးထားသော company မပါသေးသော token ဖြင့်ဝင်သည်။ Company user သည် database account
company claim/CompanyId column မှသူ့ company ကိုယူပြီး active ဖြစ်ကြောင်းစစ်သည်။
Company မချိတ်ထားသော normal user ကို login error ပြသည်။

AuthResultDto၊ CurrentUserDto၊ login/refresh/me responses တွင် companyId၊ companyName၊
requiresCompanySelection ပါသည်။ AccessToken/RefreshToken ကို selection response မှ
JavaScript သို့မပို့ဘဲ မူလ HttpOnly cookies ထဲတွင်ပဲထားသည်။

```csharp
claims.AddRange(userClaims.Where(claim => claim.Type != "company_id"
    && claim.Type != "CompanyId" && claim.Type != "company_name"));
if (companyId.HasValue)
{
    claims.Add(new Claim("company_id", companyId.Value.ToString()));
    claims.Add(new Claim("company_name", companyName ?? string.Empty));
}
```

အဟောင်း company claims ကိုဖယ်ပြီး ယခု server အတည်ပြုထားသော company claim ကိုသာ
ထည့်သည်။ ပုံမှန် user ၏ company ကို client ကမရွေးနိုင်ပါ။ `new Claim` သည် token ထဲ
ပါမည့် claim object ဖန်တီးခြင်း၊ persistent AspNetUserClaims row ပြင်ခြင်းမဟုတ်ပါ။

## Company selection API

- GET /api/Auth/companies — active/non-deleted companies စာရင်း
- POST /api/Auth/select-company — companyId နှင့် rememberMe ပို့ရန်

နှစ်ခုလုံးမှာ [Authorize(Roles = "SuperAdmin,Administrator")] ရှိသည်။ ပုံမှန်
CompanyAdmin/user တိုက်ရိုက်ခေါ်လည်း 403 ဖြစ်သည်။ POST သည်ရှိပြီးသား CSRF filter
ကိုဖြတ်ရပြီး company သည် active ဖြစ်ကြောင်း IdentityService ကထပ်စစ်သည်။ မရလျှင်
error response ပြန်ပေးပြီး လက်ရှိ company token ကိုမပြောင်းပါ။

SelectCompanyCommandHandler ကလက်ရှိ authenticated user ID ကိုယူ၍ identity service
သို့ပို့သည်။ Account CompanyId သို့မဟုတ် user claims ကိုအမြဲတမ်းပြင်မည့်အစား ရွေးထားသော
company ကို session token generation သို့ပို့သည်။ Cookie token အသစ်ထွက်ပြီးမှ client
profile ကိုupdate လုပ်ကာ Dashboard သို့သွားသည်။

## Refresh တွင် company မပျောက်စေရန်

```csharp
await _userManager.SetAuthenticationTokenAsync(user,
    "InventoryManagementSystem", "SelectedCompanyId", companyId.Value.ToString());
```

မူလ refresh token သိမ်းထားသော AspNetUserTokens ကိုပဲပြန်သုံးသည်။ Refresh token
မှန်ကြောင်းစစ်ပြီးမှ SelectedCompanyId ကိုယူ၍ token အသစ်ထုတ်သည်။ Company active
ဖြစ်ကြောင်းပြန်စစ်သည်။ ပုံမှန် user ၏ company ကိုတော့ account မှသာပြန်ယူသည်။

Login အသစ်ဝင်သော platform admin အတွက် selection ကိုရှင်းပြီး ပြန်ရွေးခိုင်းသည်။
ဒီ project ၏မူလ refresh model က user တစ်ယောက်အတွက် refresh record တစ်ခုသာသိမ်းသည်။
ဤပြောင်းလဲမှုက device တစ်ခုစီ independent sessions စနစ်သစ်မထည့်ပါ။

## Angular guard နှင့် tab session

```typescript
return authService.requiresCompanySelection() && !state.url.startsWith('/settings')
  ? router.createUrlTree(['/settings']) : true;
```

Auth guard သည် login ရှိမရှိအပြင် admin company ရွေးပြီးပြီလားကိုစစ်သည်။ `&&` က
နှစ်ချက်လုံးမှန်မှ settings သို့ပြန်ညွှန်ရန်၊ `? ... : ...` က condition အပေါ်မူတည်၍
result ရွေးခြင်းဖြစ်သည်။ /settings ကိုမပိတ်ထားလို့ redirect loop မဖြစ်ပါ။
Normal company user သည် requiresCompanySelection=false ဖြစ်သောကြောင့် Dashboard
ကိုတိုက်ရိုက်ရသည်။ Route redirect သည် UX အတွက်သာဖြစ်ပြီး server filter/roles ကို
အစားထိုးမရပါ။

မူလ inventory.tabAuthenticated sessionStorage marker ကိုဆက်ထိန်းသည်။ Fresh tab
login gate၊ CSRF၊ CORS၊ theme/language နှင့် refresh flow မဖယ်ထားပါ။ Company context
ကို localStorage ထဲ token အဖြစ်မသိမ်းပါ။ Browser reload မှာ /Auth/me မှယူသည်။

## တခြား tab မှ company ပြောင်းထားလျှင်

Browser cookies သည် tabs အကြားမျှဝေထားသည်။ Tab A မှ company 1 form ဖြည့်နေစဉ်
Tab B တွင် company 2 ရွေးပါက A ၏ form ကို company 2 သို့မှားမသိမ်းစေရန် inventory
request အစမှာ current profile ၏ company ID ကို X-Company-Id header အဖြစ်ဖမ်းထားသည်။
CompanyContextMiddleware က header နှင့် signed JWT company မကိုက်လျှင် 409 ပြန်၍
request ရပ်သည်။ Error ရလျှင် page reload လုပ်ပြီး လက်ရှိ company ကိုပြန်စစ်ရမည်။
ဒီ header သည် authorization အတွက် client ကိုယုံခြင်းမဟုတ်ပါ။ Server JWT company
claim နှင့် DbContext filter ကပဲ အဓိကဖြစ်သည်။ Profile/selection/auth requests တွင်
ဒီ company header မထည့်ထားပါ။

## အသုံးပြုရန်နှင့် verification

API ကို rebuild/restart လုပ်ပြီး Logout → Login ပြန်ဝင်ပါ။ devadmin အတွက် Settings
မှ TestCom ရွေးပြီး Continue နှိပ်ပါ။ Company 1 ၏မူလ data ပြန်ပေါ်သင့်သည်။ Header
System Settings မှ company ပြန်ပြောင်းနိုင်သည်။ Company user ဖြင့်တိုက်ရိုက် login
နှင့် select-company 403၊ refresh/reload ပြီး company ဆက်ရှိခြင်းကို ဆက်စမ်းရန်လိုသည်။

Angular/API builds နှင့် scoped frontend format check အောင်သည်။ Legacy data backfill
ကို database တွင်အသုံးချပြီး read-only verification ပြီးသည်။ Live login/switch/refresh
browser scenarios ကိုဤအဆင့်တွင်မစမ်းထားပါ။ Automated test suite မရေးထားပါ။

ပြန်ရေးလေ့ကျင့်ရန်: devadmin login response မှ requiresCompanySelection ကိုဖတ်၍
/settings သို့ပို့ပြီး company ရွေးပြီးမှ Dashboard ဝင်ပုံကို ကိုယ်တိုင်ပြန်ရေးပါ။

## Company ရွေးသော standalone page design

/settings route ကို AppLayout.children ထဲမှဖယ်ပြီး top-level route အဖြစ်ထားသည်။
ဒါကြောင့် sidebar၊ header၊ breadcrumb မပါသော သီးခြား page ဖြစ်သည်။ Auth guard နှင့်
login → settings redirect URL မပြောင်းပါ။ Login ၏ login.scss ကို styleUrl မှပြန်သုံး၍
နောက်ခံပုံ၊ centered glass card၊ brand heading၊ theme toggle၊ form spacing နှင့်
Continue button ကို Login နှင့်တူစေသည်။ Company select သည် PrimeNG p-select ဖြစ်ပြီး
appendTo=body ကြောင့် dropdown panel သည် card overflow အောက်မှာမပိတ်ပါ။

Company ရွေးပြီးသားဆို Back to Dashboard link ပြပြီး Sign Out လည်းပါသည်။ ပုံမှန်
company user သည် readonly company name ကိုသာမြင်သည်။ Header System Settings action
ကလည်း အဲဒီ standalone route သို့ပင်သွားသည်။ Custom stylesheet အသစ်မဖန်တီးထားပါ။

2026-10-20 read-only check အရ run နေသော API တွင် /api/Auth/companies နှင့်
/api/Auth/select-company endpoints ရှိပြီး database မှ active company ၁ ခုရသည်။
Authenticated browser ၏ company-list response ကိုမစစ်နိုင်သေးပါ။ UI screenshot ထဲ
မြင်ရသော error အတွက် frontend error path တွင် 404 ဖြစ်လျှင် API restart လိုကြောင်း
ဖော်ပြပေးထားသည်။ Endpoint ရှိခြင်းတစ်ခုတည်းဖြင့် authenticated call အောင်သည်ဟု
မဆိုလိုပါ။ UI layout အတွက် Angular build အောင်သည်။
