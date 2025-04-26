
# HangScheduler

## Introduction
**HangScheduler** is a lightweight job scheduling API built with ASP.NET Core and Hangfire, backed by LiteDB for simple, file-based storage. It provides REST endpoints to schedule, manage, and monitor background jobs, with optional concurrency controls and retry policies.

## Table of Contents
- [Introduction](#introduction)
- [Installation](#installation)
- [Usage](#usage)
- [Features](#features)
- [Dependencies](#dependencies)
- [Configuration](#configuration)
- [Documentation](#documentation)
- [Examples](#examples)
- [Troubleshooting](#troubleshooting)
- [Contributors](#contributors)
- [License](#license)

## Installation
1. Clone the repository:
   ```bash
   git clone <repository-url>
   ```
2. Navigate to the project directory:
   ```bash
   cd HangScheduler
   ```
3. Restore packages and build:
   ```bash
   dotnet restore
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run
   ```

## Usage
Once running, the API exposes endpoints to manage background jobs:
- **POST** `/jobs/schedule/now`: Schedule an immediate job.
- **POST** `/jobs/schedule/now/max_retry`: Immediate job with retry on failure.
- **POST** `/jobs/schedule/future`: Schedule a job to run at a future date/time.
- **POST** `/jobs/schedule/future/max_retry`: Future job with maximum retries.
- **POST** `/jobs/schedule/recurring`: Create or update a recurring job.
- **POST** `/jobs/schedule/recurring/max_retry`: Create recurring job with retries.
- **DELETE** `/jobs/scheduled/{jobId}`: Delete a scheduled job.
- **DELETE** `/jobs/recurring/{jobId}`: Delete a recurring job.

Hangfire Dashboard is available at `/hangfire`.
Swagger UI (API docs) is available at `/swagger` (non-production environments).

## Features
- Schedule one-time or recurring jobs via REST API
- Immediate or future execution
- Optional concurrency control for API calls
- Automatic retry mechanisms
- Built-in API validation (TimeZone, Cron expressions)
- Integrated Hangfire Dashboard for job visualization
- Swagger/OpenAPI support
- Serilog structured logging

## Dependencies
- [.NET 8+](https://dotnet.microsoft.com/en-us/)
- [Hangfire](https://www.hangfire.io/)
- [Hangfire.LiteDB](https://github.com/Arch/Hangfire.LiteDB)
- [LiteDB](https://www.litedb.org/)
- [Serilog](https://serilog.net/)
- [NSwag](https://github.com/RicoSuter/NSwag) (for OpenAPI generation)

## Configuration
Configuration is handled via `appsettings.json` and environment variables.

Sample `appsettings.json` snippet:
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" }
    ]
  }
}
```

## Documentation
- Swagger UI: `/swagger`
- Hangfire Dashboard: `/hangfire`

## Examples

### 1. Schedule Immediate Job
```bash
curl -X POST "http://localhost:5000/jobs/schedule/now" -H "Content-Type: application/json" -d '{
  "ApiEndpoint": "https://your-api.com/execute",
  "Payload": {"key":"value"},
  "PreventConcurrentExecution": false
}'
```

### 2. Schedule Immediate Job with Max Retry
```bash
curl -X POST "http://localhost:5000/jobs/schedule/now/max_retry" -H "Content-Type: application/json" -d '{
  "ApiEndpoint": "https://your-api.com/execute",
  "Payload": {"key":"value"},
  "PreventConcurrentExecution": false
}'
```

### 3. Schedule Future Job
```bash
curl -X POST "http://localhost:5000/jobs/schedule/future" -H "Content-Type: application/json" -d '{
  "ApiEndpoint": "https://your-api.com/execute-later",
  "Payload": {"key":"value"},
  "ScheduledDateTime": "2025-05-01T15:00:00",
  "TimeZoneId": "UTC",
  "PreventConcurrentExecution": false
}'
```

### 4. Schedule Future Job with Max Retry
```bash
curl -X POST "http://localhost:5000/jobs/schedule/future/max_retry" -H "Content-Type: application/json" -d '{
  "ApiEndpoint": "https://your-api.com/execute-later",
  "Payload": {"key":"value"},
  "ScheduledDateTime": "2025-05-01T15:00:00",
  "TimeZoneId": "UTC",
  "PreventConcurrentExecution": false
}'
```

### 5. Schedule Recurring Job
```bash
curl -X POST "http://localhost:5000/jobs/schedule/recurring" -H "Content-Type: application/json" -d '{
  "JobId": "my-recurring-job",
  "ApiEndpoint": "https://your-api.com/recurring",
  "Payload": {"key":"value"},
  "CronExpression": "0 * * * *",
  "TimeZoneId": "UTC",
  "PreventConcurrentExecution": false
}'
```

### 6. Schedule Recurring Job with Max Retry
```bash
curl -X POST "http://localhost:5000/jobs/schedule/recurring/max_retry" -H "Content-Type: application/json" -d '{
  "JobId": "my-recurring-job-retry",
  "ApiEndpoint": "https://your-api.com/recurring-retry",
  "Payload": {"key":"value"},
  "CronExpression": "0 * * * *",
  "TimeZoneId": "UTC",
  "PreventConcurrentExecution": false
}'
```

### 7. Delete a Scheduled Job
```bash
curl -X DELETE "http://localhost:5000/jobs/scheduled/{jobId}"
```

### 8. Delete a Recurring Job
```bash
curl -X DELETE "http://localhost:5000/jobs/recurring/{jobId}"
```

Replace `{jobId}` with your actual job ID.

## Troubleshooting
- **Hangfire dashboard not accessible?**
  Ensure your environment allows `/hangfire` endpoint access.
- **Jobs not running?**
  Check the database file `hangfire_lite.db` and permissions.
- **Invalid Timezone error?**
  Verify your system supports the provided `TimeZoneId`.
- **Cron Expression Errors?**
  Validate your cron syntax with an online tool or the API's built-in validation.

## Contributors
- **Maintainer**: *Daniel Pollack*

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
