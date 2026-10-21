# Supplier Portal

Company နဲ့ Supplier က header/sidebar layout တစ်ခုတည်းကို သုံးပြီး route နဲ့ permission ခွဲထားတယ်။

## Login ကနေ page ရောက်တဲ့အထိ

```text
Login
  → backend က user / role / company / supplier ကို စစ်
  → HttpOnly cookies + profile ပြန်ပေး
  → AuthService က profile ကို memory ထဲထား
  → getLandingRoute()
      → Supplier: /supplier/dashboard
      → Company ရွေးရန်လိုသော platform admin: /settings
      → Company user: /dashboard
```

Browser profile ထဲမှာ Roles၊ CompanyId၊ SupplierId ပါပေမယ့် token ကို browser storage ထဲ မသိမ်းဘူး။
ရှိပြီးသား HttpOnly cookie နဲ့ tab-session flow ကို ဆက်သုံးတယ်။

`AuthService.getLandingRoute()` က အောက်ပါအတိုင်း ဆုံးဖြတ်တယ်။

```typescript
getLandingRoute(): string {
  if (this.isSupplierAccount()) return '/supplier/dashboard';
  return this.requiresCompanySelection() ? '/settings' : '/dashboard';
}
```

`isSupplierAccount()` က server profile ရဲ့ roles ထဲမှာ Supplier ပါလား စစ်တယ်။
`? :` က condition မှန်ရင် ပထမ value၊ မှားရင် ဒုတိယ value ကို ရွေးပေးတယ်။
SuperAdmin / Administrator က company ရွေးနိုင်ပြီး CompanyAdmin နဲ့ ပုံမှန် company user က ကိုယ့် company ဆီ တိုက်ရိုက်ဝင်တယ်။

## Layout တစ်ခုတည်း၊ route မတူပုံ

`app.routes.ts` ရဲ့ အဓိကပုံစံကို ချုံ့ပြထားတာပါ။

```typescript
{
  path: '',
  loadComponent: () => import('./layout/app-layout').then(m => m.AppLayout),
  canActivate: [authGuard],
  canActivateChild: [authGuard],
  children: [
    {
      path: 'supplier',
      loadChildren: () => import('./pages/supplier/supplier.routes').then(m => m.default),
      canActivate: [authGuard]
    },
    // company dashboard, master, process, user-manage, reports ...
  ]
}
```

- `path: ''` က parent URL segment အသစ်မထည့်ဘဲ shared layout တင်ပေးတယ်။
- `AppLayout` ရဲ့ header/sidebar ကို တစ်ခါတည်းတင်ပြီး `router-outlet` ထဲမှာ child page ပြတယ်။
- `children` ထဲက `supplier` + supplier routes ထဲက `dashboard` = `/supplier/dashboard` ဖြစ်တယ်။
- `loadComponent` က standalone component ကို လိုမှ download/load လုပ်တယ်။
- `loadChildren` က child route configuration ကို လိုမှ load လုပ်တယ်။
- `() =>` က callback function၊ `import(...)` က dynamic import၊ `.then(m => m.default)` က ဖိုင်ရဲ့ default export ကို ယူတာပါ။
- `canActivate` က route စဝင်တဲ့အခါ စစ်တယ်။ `canActivateChild` က child route ပြောင်းတဲ့အခါလည်း စစ်စေတယ်။

Supplier အတွက် header/sidebar အသစ် မရေးထားဘူး။ အရင် `/supplier-portal` URL ကို `/supplier/dashboard` ဆီ redirect လုပ်ပေးထားတယ်။
`pathMatch: 'full'` က URL segment အပြည့်တူမှ redirect လုပ်ဖို့ သတ်မှတ်တာပါ။

## Guard က permission ဘယ်လိုခွဲသလဲ

`auth.guard.ts` ရဲ့ permission အပိုင်းကို ချုံ့ပြထားတယ်။

```typescript
const supplierRoute = path === '/supplier' || path.startsWith('/supplier/');

if (authService.isSupplierAccount()) {
  return supplierRoute
    ? true
    : router.createUrlTree(['/supplier/dashboard']);
}

if (supplierRoute) {
  return router.createUrlTree([authService.getLandingRoute()]);
}
```

Supplier account က supplier route ကိုပဲ ဝင်နိုင်တယ်။ `/dashboard`၊ `/master/...`၊ `/process/...`၊
`/user-manage/...`၊ `/settings` ကို URL ရိုက်ဝင်လည်း supplier dashboard ပြန်ပို့တယ်။
Company user က supplier route ကို ဝင်ရင် သူ့ landing route ပြန်ပို့တယ်။

`true` ဆို route ကို ဆက်သွားခွင့်ပြုတယ်။ `createUrlTree()` က သွားရမယ့် URL ကို Router ဆီ ပြန်ပေးတာဖြစ်လို့
guard ထဲမှာ `navigate()` ကို သီးခြားခေါ်ပြီး redirect နှစ်ခါ မဖြစ်စေဘူး။

Reload မှာ profile မရောက်သေးရင် `authInitialized$` ကို စောင့်တယ်။
`filter(initialized => initialized)` က initialization ပြီးမှ ဆက်လုပ်စေတယ်။
`take(1)` က result တစ်ခါယူပြီး subscription ပိတ်တယ်။ `map(...)` က permission result ကို ပြန်ပေးတယ်။

Guard က UI ကို ထိန်းတာပါ။ API permission က server မှာ သီးခြားရှိတယ်။

## Sidebar နဲ့ header

`app.menu.ts` မှာ menu data နှစ်စုရှိတယ်။

```typescript
get navGroups(): NavGroup[] {
  return this.authService.isSupplierAccount()
    ? this.supplierGroups
    : this.companyGroups;
}
```

Supplier ဆို `SUPPLIER_NAVIGATION_MENU` က Dashboard / PO / GRN / Return ကို ပြတယ်။
Company user ဆို အရင် `NAVIGATION_MENU` ကို ပြတယ်။ Group arrays ကို ကြိုတည်ဆောက်ထားလို့
UI update တိုင်း sidebar DOM အကုန် ပြန်မတည်ဆောက်ရဘူး။

Header breadcrumb ကလည်း တူညီတဲ့ menu source ကိုပဲ ရွေးဖတ်တယ်။
Supplier အတွက် Scan button၊ Ctrl+B scanner shortcut နဲ့ System Settings menu ကို မပေးထားဘူး။
Theme၊ language၊ sidebar collapse နဲ့ logout ကိုတော့ shared header/sidebar ကပဲ ဆက်သုံးတယ်။

## Supplier record နဲ့ login account

Supplier အချက်အလက်နဲ့ login account က တစ်ခုတည်း မဟုတ်ဘူး။

```text
Supplier record: SupplierCode / CompanyName / Contact / PaymentTerms
Login account: UserName / Email / PasswordHash / Role / CompanyId / SupplierId
```

UI ကို `User Management → Suppliers` တစ်နေရာတည်းမှာ ပေါင်းထားတယ်။
`Create login account` ဖွင့်ထားမှ account fields ဖြည့်ရတယ်။ Existing supplier ကို Edit ဖွင့်ပြီး account အသစ် ထပ်ချိတ်လို့လည်း ရတယ်။
Supplier တစ်ခုမှာ login account အများကြီး ချိတ်နိုင်တယ်။

Save က API request နှစ်ခုဖြစ်တယ်။

1. Supplier အချက်အလက်ကို အရင်သိမ်းတယ်။
2. ရလာတဲ့ SupplierId နဲ့ Admin-only account API ကို ခေါ်တယ်။

ဒုတိယ request မအောင်မြင်ရင် Supplier record က သိမ်းပြီးသားဖြစ်တယ်။
UI က `savedSupplierId` ကို မှတ်ထားပြီး `Retry Login Account` နှိပ်ရင် account အပိုင်းကိုပဲ ပြန်ခေါ်တယ်။
ဒါကြောင့် Supplier ထပ်မပွားဘူး။ Request နှစ်ခုလုံးကို ပေါင်းထားတဲ့ database transaction မဟုတ်ဘူး။

Account creation method ထဲမှာတော့ transaction ရှိတယ်။

```text
Begin transaction
  → UserManager.CreateAsync: password hash လုပ်ပြီး user ဖန်တီး
  → AddToRoleAsync: Supplier role ပေး
  → AddClaimAsync: company_id ထည့်
  → AspNetUsers.CompanyId / SupplierId ချိတ်
  → Commit
```

Role ပေးတာ/claim ထည့်တာ/ချိတ်တာ မအောင်မြင်ရင် အဲဒီ account creation transaction ကို rollback လုပ်တယ်။
Login အတွက် `IdentityUser` သုံးထားပြီး custom columns တွေကို inventory ရဲ့ `AspNetUser` က map လုပ်ထားတာကြောင့်
CompanyId / SupplierId ကို parameterized SQL နဲ့ update လုပ်ထားတယ်။
`ExecuteSqlInterpolatedAsync()` ထဲက interpolated values တွေက SQL parameters ဖြစ်တယ်။
Password ကို trim မလုပ်ဘဲ Identity ကိုပဲ hash လုပ်ခိုင်းတယ်။

Public registration က request ရဲ့ Role ကို မယူဘဲ User role ပဲ ပေးတယ်။
Supplier account API ကို SuperAdmin / Administrator / CompanyAdmin roles ပဲ သုံးနိုင်တယ်။
Client company နဲ့ admin account အသစ် register လုပ်တဲ့ action ကို SuperAdmin / Administrator ပဲ ခွင့်ပြုထားတယ်။

## Login account ဖျက်ခြင်း

```text
Supplier ရွေး → dropdown ထဲက Login Accounts
  → account ရွေး → Delete Account → confirmation
```

Backend က selected company ပိုင် account ဟုတ်လား၊ SupplierId ရှိလား၊ Supplier role တစ်ခုပဲရှိလား စစ်ပြီး
`UserManager.DeleteAsync()` နဲ့ Identity account ကို ဖျက်တယ်။ Supplier နဲ့ business documents ကို မဖျက်ဘူး။
Identity ဆိုင်ရာ claims/roles/refresh-token records တွေက user နဲ့အတူ ဖျက်သွားတယ်။

JWT access token က expiry မကုန်သေးရင် ပုံမှန် signature validation တစ်ခုတည်းနဲ့ မပိတ်နိုင်ဘူး။
ဒါကြောင့် Supplier requests အတွက် `OnTokenValidated` ထဲမှာ account၊ role၊ CompanyId၊ SupplierId နဲ့ active status ကို database နဲ့ ပြန်စစ်တယ်။
ဖျက်ပြီးသား/inactive Supplier account က နောက် protected request မှာ authentication မအောင်မြင်တော့ဘူး။
Login / refresh ကလည်း active supplier mapping မရှိရင် token အသစ် မထုတ်ပေးဘူး။

## Server permission အလွှာ

| နေရာ | စစ်ချက် |
|---|---|
| Supplier page | route guard |
| Company business APIs | `InternalUser` policy |
| Supplier document APIs | `[Authorize(Roles = "Supplier")]` |
| Account create/list/delete APIs | `InternalUser` + admin roles |
| Company data scope | DbContext CompanyId filter |
| Supplier data scope | feature query SupplierId filter |
| Detail / Print source | ownership + status + document Id |

Menu ဖျောက်ထားရုံနဲ့ API ကို ပိတ်ထားတာ မဟုတ်လို့ ဒီ server checks တွေကို သီးခြားထားတာပါ။

## Routes နဲ့ APIs

| Page | Read-only API |
|---|---|
| `/supplier/dashboard` | `/api/supplier/dashboard` |
| `/supplier/purchase-orders` | `/api/supplier/purchase-orders` |
| `/supplier/goods-receipts` | `/api/supplier/goods-receipts` |
| `/supplier/purchase-returns` | `/api/supplier/purchase-returns` |

စာရင်း API တစ်ခုချင်းစီရဲ့ `/{id}` က Detail နဲ့ Print အတွက် data ပြန်ပေးတယ်။
Frontend မှာ Print နှိပ်ရင် preview ဖွင့်ပြီး Print Voucher နှိပ်မှ browser print dialog ပွင့်မယ်။

## ကိုယ့် data ပဲ မြင်ရပုံ

JWT ထဲက `company_id` ကို `DbContext` global filter က သုံးတယ်။
`supplier_id` ကို `CurrentUserService.SupplierId` က ဖတ်ပြီး feature query ထဲမှာ စစ်တယ်။

```csharp
var supplierId = _currentUser.SupplierId.Value;
var query = _context.PurchaseOrders.AsNoTracking()
    .Where(po => po.SupplierId == supplierId
        && !po.DeletedOn.HasValue
        && po.ApprovalStatus == PurchaseOrderApprovalStatus.Approved);
```

- CompanyId / SupplierId မရှိရင် query မလုပ်ဘဲ ပိတ်တယ်။
- Frontend က SupplierId ရွေးပို့ဖို့ မလိုဘူး။ Login identity ကိုပဲ သုံးတယ်။
- Detail query မှာလည်း ownership နဲ့ status filter စစ်ပြီးမှ `Id` ကို ရှာတယ်။
- တခြား supplier/company ရဲ့ ID ပို့လာရင် Detail API က `404` ပြန်တယ်။
- Controller တွေက `[Authorize(Roles = "Supplier")]` သုံးတယ်။
- Company-side API တွေက `InternalUser` policy ကို ဆက်သုံးတယ်။
- `X-Company-Id` နဲ့ `X-Supplier-Id` header တွေက tab ထဲက account context ပြောင်းမပြောင်း စစ်ဖို့ပါ။
  Data filter အတွက် header ကို မယုံဘဲ JWT identity ကိုပဲ သုံးတယ်။ အခြား tab က account ပြောင်းထားရင် `409` ပြန်တယ်။

## ဘယ်စာရင်းတွေပြသလဲ

- PO: Approved စာရင်းပဲ ပြတယ်။ Approved ဖြစ်ပြီးမှ Cancelled ဖြစ်သွားရင် status နဲ့ cancellation reason ဆက်ပြတယ်။
- GRN: `Status == true` ဖြစ်တဲ့ received စာရင်းပဲ ပြတယ်။
- Return: Posted စာရင်းပဲ ပြတယ်။
- Soft-deleted header နဲ့ item တွေကို မပြဘူး။
- Supplier DTO ထဲမှာ password၊ token၊ idempotency hash နဲ့ internal audit fields မပါဘူး။

Backend query တွေက `Application/SupplierPortal` အောက်မှာ သီးခြားရှိတယ်။
`Supplier` လို့ namespace မပေးတာက ရှိပြီးသား Supplier entity နဲ့ နာမည်တိုက်တာကို ရှောင်ဖို့ပါ။

## Pagination နဲ့ UI

List query က filter → count → order → Skip/Take → DTO → PagedResult ပုံစံကို သုံးတယ်။
Default sorting က CreatedOn descending ဖြစ်ပြီး အစီအစဉ်တည်ငြိမ်ဖို့ Id နဲ့ ထပ်စီတယ်။
Page size ကို 1–100 အတွင်းထားတယ်။ Search က document number ကို ရှာပြီး date filter က document date ကို စစ်တယ်။
End date ကို အဲဒီနေ့အကုန် ပါအောင် `.Date <= endDate.Date` နဲ့ စစ်တယ်။

Frontend `SupplierDocuments` က route data ထဲက `kind` နဲ့ `titleKey` ကို ဖတ်တယ်။
Table ပုံစံတူတဲ့ page သုံးခုအတွက် component တစ်ခုကို ပြန်သုံးတာဖြစ်ပြီး backend query တွေကို မပေါင်းထားဘူး။
Route ပြောင်းရင် filter/detail state ရှင်းပြီး အရင် pending request ကို cancel လုပ်တယ်။
Create၊ Update၊ Delete၊ Approve၊ Post actions မပါဘူး။

## Amount

- PO amount က ordered value ဖြစ်တယ်။ Dashboard PO summary မှာ cancelled order မပါဘူး။
- GRN နဲ့ Return က backend မှာ သိမ်းထားပြီးသား UnitPrice / LineAmount / TotalAmount ကို သုံးတယ်။
- PO line amount က Quantity × UnitPrice ကို decimal နှစ်နေရာ round လုပ်ထားတာ ဖြစ်တယ်။
- Frontend မှာ `number:'1.0-0'` နဲ့ `MMK` နောက်က ထည့်ပြတယ်။ Quantity က `number:'1.0-4'` ဖြစ်တယ်။
- Dashboard ရဲ့ Net Received Amount = selected period ရဲ့ Received Amount − Posted Return Amount။
- ဒီ value က payment balance မဟုတ်ဘူး။ Invoice / Payment feature မပါသေးဘူး။
- List နဲ့ Dashboard date filter တွေက သီးခြားဖြစ်လို့ date range တူတူရွေးပြီးမှ totals နှိုင်းယှဉ်ပါ။

## Print

Angular က escape လုပ်ပြီးသား voucher content ကို hidden iframe ထဲ ထည့်ပြီး print ထုတ်တယ်။
စာရင်းကနေ Print နှိပ်တဲ့အခါ Detail API ကို ပြန်ခေါ်လို့ permission/status ကို ပြန်စစ်တယ်။
PO / GRN က A4 portrait၊ Return က source GRN/PO/warehouse columns အတွက် A4 landscape ဖြစ်တယ်။
Company header၊ Supplier၊ document number၊ date၊ items နဲ့ Total Amount ပါတယ်။

## ကိုယ်တိုင်စမ်းရန်

1. Supplier login နဲ့ menu သုံးခု ဖွင့်ပြီး search၊ date range၊ pagination နဲ့ sorting စမ်းပါ။
2. ကိုယ့် document ကို Detail / Print Preview ဖွင့်ပါ။ Browser print dialog မှာ A4 preview စစ်ပါ။
3. တခြား supplier/company ရဲ့ document ID နဲ့ detail ခေါ်ရင် `404` ဖြစ်ရမယ်။
4. Draft PO၊ မလက်ခံရသေးတဲ့ GRN နဲ့ Draft Return မပေါ်ရဘူး။
5. Company account နဲ့ Supplier API ခေါ်ရင် `403` ဖြစ်ရမယ်။
6. Dashboard date range တူတူရွေးပြီး received/returned totals နဲ့ net amount ကို နှိုင်းယှဉ်ပါ။

Schema မပြောင်းထားလို့ migration မလိုဘူး။ API rebuild/restart လိုတယ်။
