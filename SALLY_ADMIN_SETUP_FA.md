# تنظیم حساب مستقل مالک فروشگاه

این دستورالعمل برای نسخه کاری است؛ تا تکمیل آزمون‌ها، آن را جایگزین پروژه فعلی نکنید.

مدیر از SallyAccounts خوانده نمی‌شود. مسیر عمومی ثبت‌نام مدیر وجود ندارد. Credential مدیر فقط از تنظیمات امن میزبان خوانده می‌شود. مقدار پیش‌فرض Email و PasswordHash خالی است؛ در این حالت ورود مدیر بسته می‌ماند.

روی سیستم توسعه دارای .NET 8، ابزار محلی تولید هش را اجرا کنید:

```powershell
dotnet run --project tools/Sally.AdminHash/Sally.AdminHash.csproj
```

رمز فقط در ترمینال خودتان و به‌صورت مخفی وارد می‌شود. مقدار خروجی، هش سازگار با PasswordHasher برنامه است. رمز خام را در چت، سورس یا appsettings.json ذخیره نکنید.

این تنظیمات روی میزبان استقرار لازم‌اند:

| کلید محیطی | مقدار |
|---|---|
| `SallyAdmin__Email` | ایمیل ورود مالک |
| `SallyAdmin__PasswordHash` | هش تولیدشده با ابزار محلی |
| `ConnectionStrings__DefaultConnection` | اتصال دیتابیس موجودِ بررسی‌شده |
| `SallyMail__Host` و `SallyMail__Port` | SMTP دارای STARTTLS |
| `SallyMail__Username` و `SallyMail__Password` | Credential SMTP |
| `SallyMail__FromEmail` و `SallyMail__FromName` | فرستنده |
| `SallyMail__PublicBaseUrl` | نشانی HTTPS واقعی فروشگاه |
| `SallySecurity__DataProtectionKeysPath` | در صورت نیاز، پوشه پایدار کلیدهای برنامه با دسترسی محدود |
| `AllowedHosts` | دامنه‌های واقعی و تأییدشده استقرار |

کلیدهای Data Protection باید در راه‌اندازی مجدد و بین نمونه‌های همان برنامه پایدار و محافظت‌شده باشند؛ تغییر کلیدها می‌تواند Cookie و لینک‌های ایمیل قبلی را نامعتبر کند. تغییر هش یا ایمیل مدیر، Cookie قبلی مدیر را رد می‌کند. مشتری و مدیر Cookie و Logout مستقل دارند.

ورود مدیر: `/admin/login`؛ پنل: `/admin`؛ محصولات: `/Products`؛ موجودی سایز: `/admin/inventory`؛ سفارش‌ها: `/admin/orders`.

Hangfire در حالت پیش‌فرض خاموش است و Worker داخلی، پیام‌های Outbox را در صورت تنظیم معتبر SMTP بررسی می‌کند. روشن‌کردن Hangfire هیچ اجازه‌ای برای ساخت Schema نیست؛ Schema آن باید قبلاً به‌صورت جداگانه بررسی و تأیید شده باشد.
