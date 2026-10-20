# Department master — Supplier format

လက်ရှိ Department entity ၏ Id၊ DeptName၊ IsActive နှင့် audit fields ကိုသာသုံးသည်။
DeptCode သို့မဟုတ် schema အသစ်မထည့်ပါ။ Supplier reference files မပြင်ထားပါ။

## API structure

`Application/Master/Departments` အောက်တွင် DTO၊ Create/Update/Delete commands၊
GetDepartments/GetDepartmentById queries နှင့် handlers ကို Supplier ကဲ့သို့ခွဲထားသည်။
`Api/Controllers/Master/DepartmentsController.cs` က MediatR သို့ပို့ပြီး
DefaultResponseModel ပြန်ပေးသည်။ URL သည် `/api/master/departments` ဖြစ်သည်။

| Method | URL | လုပ်ဆောင်ချက် |
|---|---|---|
| GET | /api/master/departments | DeletedOn မရှိသော rows အားလုံး၊ အသစ်အပေါ်ဆုံး |
| GET | /api/master/departments/{id} | ID အလိုက်ဖတ်ရန် |
| POST | /api/master/departments | Department ဖန်တီးရန် |
| PUT | /api/master/departments/{id} | Department ပြင်ရန် |
| DELETE | /api/master/departments/{id} | Soft delete |

List သည် IsActive=false rows ကိုပါပြသည်။ DeletedOn မရှိသော rows ကိုသာယူသည်။
CompanyId ကို request မှမယူပါ။ DbContext global filter က လက်ရှိ company ကိုစစ်ပြီး
SaveChangesAsync က အသစ်သိမ်းသော row တွင်လက်ရှိ CompanyId ကိုဖြည့်သည်။ Department
handlers တွင် ICurrentUserService နှင့် CompanyId condition ထပ်မထည့်ထားပါ။ Validation သည် Name required/max 200 နှင့်
Update ID > 0 ဖြစ်ရန်သာဖြစ်သည်။ Name ကို save မတိုင်မီ Trim လုပ်သည်။

```csharp
_context.Departments.Remove(entity);
await _context.SaveChangesAsync(cancellationToken);
```

Remove ကိုခေါ်သော်လည်း project ၏ DbContext.ApplyAuditValues က Deleted state ကို
Modified ပြောင်းပြီး DeletedOn = DateTime.Now၊ DeletedBy = current user ကိုဖြည့်သည်။
Created/Updated audit fields ကိုလည်းရှိပြီးသား audit flow ကဖြည့်သည်။

## Client structure

- Model: core/models/master/departments.model.ts
- HTTP service: core/services/master/departments.service.ts
- Component/template: pages/master/departments/departments.ts နှင့် departments.html
- Route: /master/departments
- Menu: NAV.DEPARTMENTS (English/Burmese dictionaries နှစ်ခုလုံး)

Supplier ကဲ့သို့ constructor injection၊ FormBuilder၊ loadData/create/onSubmit/
update/delete/excel method အစီအစဉ်နှင့် subscribe next/error callbacks ကိုသုံးသည်။
Service တွင် local const url၊ RootModel response နှင့် JSON.stringify request ပုံစံ
ကိုလိုက်ထားသည်။ Global content-type interceptor က JSON Content-Type ထည့်ပေးသည်။

```typescript
public departmentForm = this.formBuilder.group({
  id: [0],
  deptName: ['', [Validators.required, Validators.maxLength(200)]],
  isActive: [true],
});
```

FormBuilder.group က reactive form controls အစုတည်ဆောက်သည်။ `[value, validators]`
ပုံစံဖြစ်သည်။ Create ဖွင့်တိုင်း status=true ပြန်ထားသည်။ update() ကရွေးထားသော row
ကို patchValue ဖြင့် form ထဲထည့်ပြီး အဲဒီ dialog ကိုပဲဖွင့်သည်။ onSubmit() တွင်
isEdit ပေါ်မူတည်၍ service.create သို့မဟုတ် service.update ကိုခေါ်သည်။ Request
ပြီးမှ loading ပြန်ပိတ်ထားသည်။ Row မရွေးဘဲ Edit/Delete နှိပ်လျှင် message ပြသည်။

Table ၏ [(selection)] သည် selectedDepartment နှင့်ချိတ်ပြီး [pSelectableRow] က
row တစ်ကြောင်းရွေးနိုင်စေသည်။ Create splitbutton dropdown တွင် Edit/Delete/Excel
ရှိသည်။ filterGlobal က deptName/createdBy/updatedBy ကိုရှာသည်။ Pagination က
Supplier ကဲ့သို့ browser-side ဖြစ်ပြီး default sort သည် CreatedOn descending ဖြစ်သည်။

Custom SCSS နှင့် form control fixed heights မထည့်ပါ။ PrimeNG theme နှင့်
လက်ရှိ utility classes ကိုသုံးသည်။

## ကိုယ်တိုင်စမ်းရန်

API rebuild/restart ပြီး Master → Departments မှာ Create၊ Active/Inactive ပြောင်း၍
Update၊ selection၊ search/pagination၊ Delete confirmation နှင့် Excel စမ်းနိုင်သည်။
Schema အသစ်မပြောင်းသောကြောင့် ယခု API/client အတွက် migration အသစ်မလိုပါ။ မူလ
AddDepartmentTable migration ကို development database မှာ apply ပြီးသားဖြစ်ရမည်။
ဒီလုပ်ဆောင်မှုအတွင်း database migration သို့မဟုတ် data-changing API tests မလုပ်ပါ။

ပြန်ရေးလေ့ကျင့်ရန်: update() က selectedDepartment ကို form ထဲဖြည့်ပြီး onSubmit()
က PUT request ပို့သွားပုံကို code မကြည့်ဘဲ ပြန်ရေးပါ။

## လက်ရှိ company တစ်ခုတည်းကိုသာမြင်ရခြင်း

```csharp
.HasQueryFilter(e => CurrentCompanyId.HasValue && e.CompanyId == CurrentCompanyId);
```

HasValue က လက်ရှိ company ရှိမရှိစစ်သည်။ `&&` ဆိုသည်မှာ စည်းကမ်းနှစ်ခုလုံးမှန်မှ
data ကိုယူခြင်းဖြစ်သည်။ Company A user သည် A data ပဲရမည်။ SuperAdmin ဖြစ်လည်း
အလားတူဖြစ်သည်။ CompanyId မရှိလျှင် null-company records အပါအဝင်ဘာမှမရပါ။
Department handlers တွင် CompanyId ထပ်စစ်ခြင်းနှင့် current user service injection
မလိုတော့ပါ။ SaveChangesAsync မှာ company မရှိလျှင် error ပြ၊ ရှိလျှင် entity ၏
CompanyId ကိုဖြည့်ပေးသည်။ တခြား company ID ထည့်၍ inventory data အသစ်သိမ်းရန်လည်း
ပိတ်ထားသည်။ ဒီပြောင်းလဲမှုက schema မပြောင်းလို့ migration အသစ်မလိုပါ။

Company ရွေးရန် Settings page ထည့်ပြီးဖြစ်သည်။ လက်ရှိ login token ၏ company_id claim
ကိုဆက်သုံးသည်။ Company မချိတ်ထားသော account ဖြင့် login ဝင်လျှင် inventory list
အလွတ်ရမည်။ Legacy CompanyId=null records ကို user အတည်ပြုချက်ဖြင့် company 1 သို့ သီးခြား backfill လုပ်ထားသည်။
