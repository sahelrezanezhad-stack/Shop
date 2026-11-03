این تحویل، کل سورس یکپارچه Shop برای بیلد Debug و تست است. راهنمای فعلی: [SALLY_TEST_PACKAGE_FA.md](SALLY_TEST_PACKAGE_FA.md). نسخه نهایی انتشار نیست؛ وضعیت آزمون‌ها در [00_WORKSPACE_STATUS_FA.md](00_WORKSPACE_STATUS_FA.md) آمده است. راهنماهای قدیمی مجوز اجرای خودکار Migration یا Seed نیستند.

# Shop

An ASP.NET Core MVC project built with Clean Architecture.

## Project Structure

- **Shop.Presentation**: Handles the user interface and MVC components.
- **Shop.Application**: Contains application logic and use cases.
- **Shop.Domain**: Contains core business entities and rules.
- **Shop.Infrastructure**: Implements database access and external services.

## Architecture

The project follows Clean Architecture principles, keeping the Domain independent from other layers and organizing project references to respect dependency rules.

## Technologies

- C#
- ASP.NET Core MVC
- .NET
- Clean Architecture

## Getting Started

Open `Shop.sln` in Visual Studio and build the Debug configuration first. The solution includes the four application projects, security tests, and the local administrator password-hash tool. For SQL integration tests, follow `SALLY_TEST_PACKAGE_FA.md` and use the `SALLY_TEST` launch profile after preparing its separate test database. No database migration or seed runs at startup.
