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

Open `Shop.sln` in Visual Studio and run `Shop.Presentation` as the startup project.