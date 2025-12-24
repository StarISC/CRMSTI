# Copilot Instructions for CRMSTI Codebase

## Project Overview
- This is a classic ASP.NET Web Forms application (C#), with each `.aspx` page paired with a `.aspx.cs` code-behind file.
- Core business logic and data access are centralized in `App_Code/DataAccess.cs`.
- The application appears to manage bookings and customers, with API endpoints exposed via `*Api.aspx` and `*DetailApi.aspx` files.

## Key Architectural Patterns
- **Page/API Split:** Each major entity (e.g., Bookings, Customers) has both a UI page (`.aspx`) and a corresponding API endpoint (`Api.aspx`/`DetailApi.aspx`).
- **Data Access:** All database operations are funneled through `App_Code/DataAccess.cs`. Reuse or extend this for new data operations.
- **Master Page:** `Site.Master` provides shared layout and logic for all pages.
- **Exports:** Data export logic is handled in dedicated pages like `CustomersExport.aspx`.

## Developer Workflows
- **Build/Run:** Use Visual Studio (Windows) to build and debug. Open the solution and run with IIS Express or your configured web server.
- **No explicit test suite** is present; manual testing via the web UI is standard.
- **Configuration:** Main settings are in `Web.config`.

## Project-Specific Conventions
- **Naming:**
  - UI pages: `Entity.aspx`/`Entity.aspx.cs`
  - API endpoints: `EntityApi.aspx`/`EntityApi.aspx.cs`, `EntityDetailApi.aspx`/`EntityDetailApi.aspx.cs`
  - Data access: `App_Code/DataAccess.cs`
- **No modern frameworks** (e.g., MVC, Razor, SPA) are used; stick to Web Forms patterns.
- **Session and authentication** logic is likely handled in `Login.aspx` and `Site.Master`.

## Integration Points
- **Database:** All DB access via `DataAccess.cs`.
- **Exports:** Data export endpoints are separate from main UI/API.
- **No external service integrations** are visible in the codebase structure.

## Examples
- To add a new entity (e.g., Orders):
  1. Create `Orders.aspx`/`Orders.aspx.cs` for UI.
  2. Create `OrdersApi.aspx`/`OrdersApi.aspx.cs` for API.
  3. Add DB logic to `DataAccess.cs`.

## Key Files
- `App_Code/DataAccess.cs`: Central data access logic
- `Site.Master`: Shared layout and logic
- `Web.config`: Application configuration
- `*Api.aspx`, `*DetailApi.aspx`: API endpoints

---

**For AI agents:**
- Follow the file naming and architectural patterns above.
- Reuse `DataAccess.cs` for all DB operations.
- Do not introduce new frameworks or patterns unless explicitly requested.
- Reference this file for project-specific guidance before generating code.
