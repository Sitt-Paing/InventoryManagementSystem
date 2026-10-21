# GRN / Purchase Return Amounts

ဒီ amount က tax/discount မပါသေးတဲ့ ပစ္စည်းတန်ဖိုးဖြစ်တယ်။ Supplier payment due amount ကို တွက်ပေးတာ မဟုတ်သေးဘူး။

## GRN

`CreateGoodReceiptCommandHandler` က မူလ PO item ရဲ့ UnitPrice ကို GRN item မှာကူးသိမ်းတယ်။

```csharp
UnitPrice = poItem.UnitPrice,
LineAmount = decimal.Round(item.ReceivedQuantity * poItem.UnitPrice, 2, MidpointRounding.AwayFromZero),
```

ReceivedQuantity က PO ဈေးသတ်မှတ်ထားတဲ့ UOM နဲ့တူတာကို ရှိပြီးသား validation က စစ်တယ်။
Stock အတွက်ပြောင်းထားတဲ့ ReceivedBaseQuantity ကို amount တွက်ဖို့ မသုံးဘူး။
ဥပမာ 10 အိတ်မှာထားပေမယ့် 4 အိတ်သာလက်ခံ၊ တစ်အိတ် 50,000 ဆို GRN amount က 200,000 ဖြစ်တယ်။

Header TotalAmount က မဖျက်ထားတဲ့ item LineAmount အားလုံးပေါင်းဖြစ်တယ်။
GRN update က header metadata ကိုပဲ ပြောင်းခွင့်ပေးတာကြောင့် သိမ်းထားတဲ့ UnitPrice ကို PO ကနေပြန်မယူဘူး။

## Purchase Return

- Draft အသစ်မှာ source GRN item ရဲ့ UnitPrice ကို ကူးသိမ်းတယ်။
- `LineAmount = ReturnQuantity × UnitPrice` ကို decimal 2 နေရာနဲ့ round လုပ်တယ်။
- Draft ပြင်ရင် ရှိပြီးသား line ဈေးကိုထိန်းပြီး quantity အသစ်နဲ့ပြန်တွက်တယ်။ Line အသစ်ထည့်မှ GRN ဈေးကိုယူတယ်။
- ဖြုတ်ထားတဲ့ lines တွေကို Header TotalAmount ထဲမထည့်ဘူး။
- Post လုပ်ရင် GRN ဈေးနဲ့ amount ကို အတည်ပြုတွက်ပြီး stock/status နဲ့ transaction တစ်ခုထဲ သိမ်းတယ်။
- Posted request ထပ်လာရင် ရှိပြီးသား result ကိုပဲပေးတယ်။ Posted amount ကို ပြန်မတွက်ဘူး။

Backend က amount တွက်တာဖြစ်လို့ create/update request ထဲမှာ UnitPrice၊ LineAmount၊ TotalAmount မလိုဘူး။
List/detail နဲ့ command response DTO တွေမှာတော့ ဒီ fields တွေ ပြန်ပေးတယ်။

## Client မှာပြသပုံ

- GRN နဲ့ Purchase Return list မှာ Total Amount ပြတယ်။
- Detail နဲ့ Print voucher မှာ Unit Price၊ Line Amount နဲ့ Total Amount ပါတယ်။
- Client မှာ `number:'1.0-0'` နဲ့ `MMK` suffix သုံးပြီး `1,000,000 MMK` ပုံစံနဲ့ပြတယ်။ သိမ်းထားတဲ့ decimal amount ကိုတော့ မပြောင်းဘူး။
- Backend က သိမ်းပြီးပြန်ပေးတဲ့ amounts ကိုပြတာဖြစ်ပြီး browser မှာ ပြန်မတွက်ဘူး။

## Rounding

`decimal` သုံးပြီး item တစ်ကြောင်းစီကို round လုပ်ပြီးမှ header မှာပေါင်းတယ်။
`MidpointRounding.AwayFromZero` ကြောင့် positive amount `1.005` ကို `1.01` ဖြစ်စေပြီး SQL `ROUND(..., 2)` နဲ့ကိုက်တယ်။

## Data အဟောင်း

`AddReceiptAndReturnAmounts` က columns ထည့်တယ်။
`BackfillReceiptAndReturnAmounts` က PO → GRN → Return အစဉ်နဲ့ data ဖြည့်တယ်။

- Company၊ Product၊ UOM နဲ့ source document ချိတ်ဆက်မှု မကိုက်ရင် migration ကိုရပ်တယ်။
- Price နဲ့ LineAmount နှစ်ခုလုံး 0 ဖြစ်တဲ့ legacy lines ကို linked source price နဲ့ဖြည့်တယ်။
- Header totals မှာ soft-deleted items မပါဘူး။
- လက်ရှိ linked PO ထဲကဈေးနဲ့ ပြန်တည်ဆောက်တာဖြစ်လို့၊ အဲဒီ PO ဈေးကို အရင်ကပြင်ဖျက်ထားပြီးသားဆို historical price အစစ်ကို ဒီ migration က ပြန်မရနိုင်ဘူး။
- Backfill ကို rollback လုပ်တဲ့အခါ ရှိပြီးသား reconstructed values တွေကို မဖျက်ဘူး။ Column များကိုတော့ schema migration rollback က ဖယ်ပေးမယ်။

## ကိုယ်တိုင်စစ်ရန်

1. Partial GRN quantity နဲ့ total ကိုစစ်ပါ။ PO တစ်စောင်လုံး amount မဖြစ်ရဘူး။
2. Return Draft quantity ပြင်တာ၊ line ဖြုတ်တာနဲ့ total ပြန်တွက်တာကိုစစ်ပါ။
3. GRN metadata ပြင်ပြီး UnitPrice မပြောင်းတာကိုစစ်ပါ။
4. Post အောင်တဲ့ Return မှာ amount နဲ့ stock change နှစ်ခုလုံးသိမ်းပြီး Post ထပ်လုပ်ရင်မပြောင်းတာကိုစစ်ပါ။
5. Product ရဲ့ လက်ရှိ CostPrice ပြောင်းတာက voucher အဟောင်း amount ကိုမပြောင်းရဘူး။
