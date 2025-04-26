# HangScheduler API

*A lightweight REST wrapper around [Hangfire](https://www.hangfire.io/) that lets you schedule background jobs over HTTP.  
It uses **LiteDB** for storage—no external database required.*

---

## ✨ Features

- **Immediate, delayed, and recurring jobs** via clean, versioned HTTP endpoints  
- **Concurrency-control & infinite-retry helpers** (opt-in per request)  
- **Hangfire Dashboard** exposed at `/hangfire` for live monitoring  
- **Serilog** structured logging pre-wired (console & file sinks via *appsettings*)  
- **OpenAPI / Swagger UI (NSwag)** – all sections collapsed & alphabetized in non-prod  
- **Global exception middleware** for consistent error payloads  
- Ships with a **sample `IJobProcessor`** that proxies external APIs

---

## 🛠 Tech Stack

| Layer              | Library / Tool          | Notes                                      |
|--------------------|-------------------------|--------------------------------------------|
| **Runtime**        | .NET 9 (top-level `Program.cs`) | Simple startup, fast build times |
| **Background jobs**| Hangfire 1.8            | Declarative API, retries, dashboard        |
| **Persistence**    | Hangfire.LiteDB         | Stores state in `hangfire_lite.db`         |
| **Logging**        | Serilog                 | JSON or compact formatting supported       |
| **API Docs**       | NSwag                  | Generates OpenAPI 3 & Swagger UI           |

---

## 🚀 Getting Started

### Prerequisites
- [.NET9 SDK](https://dotnet.microsoft.com/en-us/download)
- *(Optional)* Docker if you plan to containerize

### Clone & Run
```bash
git clone https://github.com/your-org/hangscheduler.git
cd hangscheduler
dotnet run --project HangScheduler.Api
