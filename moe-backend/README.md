# MOE Portal - Backend

This repository contains the .NET 10 Web API for the MOE e-Service Portal.

## Architecture
- **Framework**: ASP.NET Core 10 Web API
- **Database**: Entity Framework Core with SQLite (`MoePortal.Infrastructure`)
- **Authentication**: JWT Bearer configured via `Singpass` mock.
- **AI Integration**: Semantic Kernel `IAiAssistantService` using OpenAI/GitHub Models.

## Getting Started
1. Create a `.env.agent` file two levels up (`../../.env.agent`) with your `OPENAI_API_KEY`.
2. Open `MoePortal.slnx` or use `dotnet run --project MoePortal.Api`
3. The API will run on `http://localhost:5001`.
4. The database is automatically seeded at startup with alpha tester accounts.
