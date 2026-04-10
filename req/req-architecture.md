# Required Architecture

```
┌─────────────┐     ┌─────────────┐     ┌────────────────────┐
│   Web App   │────>│   API App   │────>│     Database       │
│  (Razor UI) │     │  (Web API)  │     │ (SQLite/SQLServer) │
└─────────────┘     └─────────────┘     └────────────────────┘
```

Two web applications:

- **Web App** — User Interface (Razor). Calls the API App server-side using HttpClient. Never connects to the database directly. The browser only communicates with the Web App.
- **API App** — REST APIs. Provides the Database Context and migrations. Not directly accessed by browsers.

CORS is not required as the API App is only called server-side by the Web App.



## Environments

Support both local development and cloud deployment.

Local developer environent:

- `Development` (dev test on developer machines)

Cloud environments are:

- `Staging` (Staging)
- `Production` (Production)

## Database

Database: SQL Server

For environments: `Development` and `Staging` only - do database seeding with sample data (for environments: `Development` and `Staging`) — this is done by the API App on startup

---
