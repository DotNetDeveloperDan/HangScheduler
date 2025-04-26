HangScheduler API

A lightweight REST wrapper around Hangfire that lets you schedule background jobs over HTTP while keeping deployment simple thanks to LiteDB storage (no external database required).

✨ Features

Immediate, delayed, and recurring jobs via clean, versioned HTTP endpoints.

Optional concurrency control and infinite‑retry helpers.

Hangfire Dashboard exposed at /hangfire for live monitoring.

Serilog structured logging pre‑wired (console & file sinks via appsettings).

OpenAPI/Swagger UI (NSwag) with collapse‑all + alphabetic sorting in non‑prod.

Global exception middleware converts server errors into consistent responses.

Ships with a Sample IJobProcessor implementation that proxies external APIs.

🛠️ Tech Stack

Layer

Library / Tool

Notes

Runtime

.NET 8 + top‑level Program.cs

Simple startup, fast build times

Background jobs

Hangfire 1.8

Declarative API, retries, dashboard

Persistence

Hangfire.LiteDB storage

Stores state in hangfire_lite.db file

Logging

Serilog

JSON or compact formatting supported

API Docs

NSwag

Generates OpenAPI 3 & Swagger UI

🚀 Getting Started

Prerequisites

- .NET 8 SDK
- (Optional) Docker if you plan to containerise

Clone & Run

git clone https://github.com/your‑org/hangscheduler.git
cd hangscheduler
# restore & run
dotnet run --project HangScheduler.Api

The API listens on https://localhost:8081 and http://localhost:8080 by default (see Kestrel settings).

Configuration

Settings are loaded in this order (later wins):

1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables

Key sections you may want to tweak:

{
  "Hangfire": {
    "LiteDbConnectionString": "Filename=hangfire_lite.db;Mode=Exclusive;"
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [ { "Name": "Console" } ]
  }
}

Dev tip: In Development the HttpClient used by JobProcessor skips SSL validation so you can hit self‑signed HTTPS endpoints.

🗂️ API Reference

Verb

Path

Purpose

POST

/jobs/schedule/now

Enqueue a job immediately

POST

/jobs/schedule/now/max_retry

Same as above but with infinite retries

POST

/jobs/schedule/future

Schedule a one‑off job at a future date/time

POST

/jobs/schedule/future/max_retry

Future job with infinite retries

POST

/jobs/schedule/recurring

Create/update a CRON‑based recurring job

POST

/jobs/schedule/recurring/max_retry

Same with infinite retries

DELETE

/jobs/scheduled/{jobId}

Remove a queued or scheduled job

DELETE

/jobs/recurring/{jobId}

Remove a recurring job

Request Models

// JobRequest
{
  "apiEndpoint": "https://example.com/task",
  "payload": {
    "foo": "bar"
  },
  "preventConcurrentExecution": true,
  "continueAfterJobId": "optional‑id"
}

// JobFutureRequest extends JobRequest
{
  "scheduledDateTime": "2025‑04‑30T14:00:00",
  "timeZoneId": "America/New_York"
}

// RecurringJobRequest extends JobRequest
{
  "jobId": "daily‑report",
  "cronExpression": "0 12 * * MON‑FRI",
  "timeZoneId": "UTC"
}

Example: enqueue a job now

curl -X POST https://localhost:8081/jobs/schedule/now \
     -H "Content‑Type: application/json" \
     -d '{
           "apiEndpoint": "https://api.acme.com/report",
           "payload": {"date":"2025‑04‑26"},
           "preventConcurrentExecution": false
         }'

Successful response:

{ "jobId": "e6f5b0b1" }

Hangfire Dashboard

Browse to /hangfire for a real‑time view of queues, retries, and scheduled jobs (authorization filter currently allows all users – secure before production!).

📝 Versioning

Assembly version is auto‑generated in the format yyyy.MM.dd.build and trimmed to exclude the git hash (e.g. 2025.04.26.1).

🙋 Contributing

1. Fork the repo
2. Create a feature branch (git checkout -b feat/my‑feature)
3. Commit your changes (git commit -m "feat: add X")
4. Push and open a PR

We follow Conventional Commits for changelogs.

📄 License

Distributed under the MIT License. See LICENSE for more information.

