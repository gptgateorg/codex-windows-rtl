<div align="center">
  <img src="CodexRtlManager/Resources/gptgate-logo.png" alt="GptGate RTL" width="112">
  <h1>GptGate RTL</h1>
  <p><strong>راست‌چینِ سبک و متن‌باز برای اپ رسمی ChatGPT در ویندوز</strong></p>
  <p>متن فارسی و عربی راست‌چین؛ کد، ترمینال و ورودی‌ها همچنان چپ‌چین.</p>
</div>

<p align="center">
  <a href="https://github.com/gptgate/gptgate-rtl/actions/workflows/ci.yml"><img src="https://github.com/gptgate/gptgate-rtl/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/gptgate/gptgate-rtl/releases/latest"><img src="https://img.shields.io/github/v/release/gptgate/gptgate-rtl?label=release" alt="Latest release"></a>
  <a href="https://github.com/gptgate/gptgate-rtl/releases"><img src="https://img.shields.io/github/downloads/gptgate/gptgate-rtl/total?label=downloads" alt="Downloads"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-22c55e.svg" alt="MIT License"></a>
  <img src="https://img.shields.io/badge/platform-Windows-38bdf8.svg" alt="Windows">
</p>

<p align="center">
  <a href="https://github.com/gptgate/gptgate-rtl/releases/latest"><strong>دانلود برای ویندوز</strong></a> ·
  <a href="#نصب-سریع">نصب</a> ·
  <a href="#این-برنامه-چطور-کار-میکند">نحوه کار</a> ·
  <a href="#توسعه-و-مشارکت">مشارکت</a> ·
  <a href="https://gptgate.ai/">وب‌سایت</a> ·
  <a href="https://t.me/GptGateAI">تلگرام</a>
</p>

<p align="center"><sub><b>برنامه‌نویس‌ها و ایجنت‌ها:</b> برای شناخت سریع پروژه، فایل <a href="llms.txt"><code>llms.txt</code></a> و سپس <a href="AGENTS.md"><code>AGENTS.md</code></a> را بخوانید.</sub></p>

---

<div dir="rtl">

## چرا GptGate RTL؟

اپ رسمی ChatGPT هنوز تجربه یکدستی برای متن‌های راست‌به‌چپ ندارد. GptGate RTL بدون دست‌کاری فایل‌های Microsoft Store، برنامه را اجرا می‌کند و یک اصلاح کوچک را در زمان اجرا اعمال می‌کند:

- متن فارسی و عربی به‌صورت زنده راست‌چین می‌شود.
- بلوک‌های کد، ترمینال و فیلدهای قابل ویرایش چپ‌چین باقی می‌مانند.
- نسخه Microsoft Store و نصب معمولی به‌صورت خودکار شناسایی می‌شوند.
- هیچ فایل سیستمی یا بسته نصب برنامه patch نمی‌شود.
- رابط فارسی، دارک و هماهنگ با زبان طراحی GptGate است.
- فونت آزاد Arad با وزن‌های درست داخل برنامه قرار دارد.
- کاربر می‌تواند فونت دلخواه TTF یا OTF را انتخاب کند و هر زمان به Arad برگردد.

## نصب سریع

1. وارد صفحه [آخرین نسخه](https://github.com/gptgate/gptgate-rtl/releases/latest) شوید.
2. یکی از این دو فایل را دانلود و از حالت فشرده خارج کنید:
   - **Standalone** — پیشنهاد ما؛ بدون نیاز به نصب .NET.
   - **Light** — کم‌حجم‌تر؛ نیازمند [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).
3. فایل `GptGateRTL.exe` را اجرا کنید و دکمه فعال‌سازی را بزنید.

### انتخاب فونت

فونت پیش‌فرض متن فارسی داخل ChatGPT، **Arad** است. از بخش «فونت متن فارسی» می‌توانید یک فایل معتبر `TTF` یا `OTF` تا حجم ۵ مگابایت انتخاب کنید. فونت در فضای محلی تنظیمات GptGate RTL ذخیره و در اجرای بعدی ChatGPT اعمال می‌شود. گزینه «بازگشت به Arad» تنظیم پیش‌فرض را برمی‌گرداند.

### نیازمندی‌ها

- ویندوز 10 نسخه 1809 یا جدیدتر
- اپ رسمی ChatGPT برای ویندوز

## این برنامه چطور کار می‌کند؟

```text
GptGate RTL
    │
    ├── نصب ChatGPT را پیدا می‌کند
    ├── برنامه را با یک درگاه محلی اجرا می‌کند
    ├── فقط به DevTools روی loopback متصل می‌شود
    └── جهت متن را هنگام نمایش پیام‌ها اصلاح می‌کند
          ├── فارسی و عربی → RTL
          └── کد، ترمینال و ورودی → LTR
```

همه‌چیز روی دستگاه شما انجام می‌شود. GptGate RTL محتوای گفتگو را به سرور دیگری ارسال نمی‌کند و فایل‌های نصب ChatGPT را تغییر نمی‌دهد.

## توسعه و مشارکت

پروژه برای همکاری برنامه‌نویس‌ها و ایجنت‌های کدنویسی آماده شده است. برای شروع:

```powershell
git clone https://github.com/gptgate/gptgate-rtl.git
cd gptgate-rtl
dotnet build .\CodexRtlManager\CodexRtlManager.csproj -c Release
dotnet run --project .\CodexRtlManager.Tests\CodexRtlManager.Tests.csproj -c Release
```

قبل از ارسال تغییرات، [راهنمای مشارکت](CONTRIBUTING.md) را بخوانید. گزارش‌های امنیتی باید مطابق [سیاست امنیت](SECURITY.md) ارسال شوند.

</div>

<details>
<summary><strong>English overview</strong></summary>

## GptGate RTL

GptGate RTL is a lightweight, open-source Persian and Arabic RTL companion for the official ChatGPT desktop app on Windows.

It launches the installed app with a local debugging port and injects a small runtime helper. It does **not** modify Microsoft Store files. Persian and Arabic prose becomes right-to-left while code, terminals, and editable fields remain left-to-right.

### Highlights

- Live Persian and Arabic RTL support
- Code blocks and terminals remain LTR
- Microsoft Store and regular desktop installation detection
- No installer or application-file patching
- Framework-dependent lightweight and self-contained releases
- Arad open-source Persian typeface under SIL OFL 1.1
- Custom local TTF/OTF selection with Arad as the default

### Build

```powershell
dotnet build .\CodexRtlManager\CodexRtlManager.csproj -c Release
dotnet run --project .\CodexRtlManager.Tests\CodexRtlManager.Tests.csproj -c Release
```

### Architecture

- `CodexLocator` detects supported ChatGPT desktop installations.
- `RuntimeRtlService` starts ChatGPT and communicates only with its loopback DevTools endpoint.
- `RuntimeRtlPayload` observes the DOM and applies direction only where needed.
- `CodexManagerService` coordinates discovery, launch, and safe restart operations.
- `MainWindow` contains the WPF presentation layer.

</details>

## Open-source licenses

GptGate RTL is released under the [MIT License](LICENSE). The bundled [Arad typeface](https://github.com/MDarvishi5124/Arad) is distributed under SIL Open Font License 1.1; see [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Disclaimer

GptGate RTL is an independent community project and is not affiliated with or endorsed by OpenAI. ChatGPT, Codex, and OpenAI are trademarks of their respective owners.
