# MeetHub — Meeting Room Booking System

A full-stack meeting room booking platform. Employees search, book, and manage meeting rooms across multiple buildings; admins manage the organization's structure, reservations, and system-wide rules — with an AI chatbot, live calendar updates, and email notifications on top.

![.NET 8](https://img.shields.io/badge/.NET_8-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-5C2D91?logo=dotnet&logoColor=white)

![Entity Framework Core](https://img.shields.io/badge/EF_Core-512BD4)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?logo=microsoftsqlserver&logoColor=white)

![Swagger](https://img.shields.io/badge/Swagger-85EA2D?logo=swagger&logoColor=black)
![JWT](https://img.shields.io/badge/JWT-black?logo=jsonwebtokens&logoColor=white)
![2FA](https://img.shields.io/badge/2FA-17A2B8)

![AutoMapper](https://img.shields.io/badge/AutoMapper-B33A3A)
![Serilog](https://img.shields.io/badge/Serilog-C73E1D)
![SignalR](https://img.shields.io/badge/SignalR-0078D4)

![xUnit](https://img.shields.io/badge/xUnit-5C2D91?logo=dotnet&logoColor=white)
![Moq / FluentAssertions](https://img.shields.io/badge/Moq_%2F_FluentAssertions-6A4C93)

![JavaScript](https://img.shields.io/badge/JavaScript-F7DF1E?logo=javascript&logoColor=black)
![HTML5](https://img.shields.io/badge/HTML5-E34F26?logo=html5&logoColor=white)
![CSS3](https://img.shields.io/badge/CSS3-1572B6?logo=css3&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-7952B3?logo=bootstrap&logoColor=white)

![i18n](https://img.shields.io/badge/i18n-AZ%2FEN-4F7DF3)
![Chart.js](https://img.shields.io/badge/Chart.js-FF6384?logo=chartdotjs&logoColor=white)
![Dark/Light Mode](https://img.shields.io/badge/Dark%2FLight_Mode-0B1626)


---

## Project Overview

MeetHub coordinates shared meeting rooms across several office buildings without double-bookings or manual spreadsheets:

- **Self-service booking** — search by date/time/capacity/equipment, book instantly, manage own reservations.
- **Full admin back office** — buildings, floors, rooms, equipment, working hours, system-wide rules, users, all reservations.
- **AI assistant** (Gemini function-calling) — answers questions and searches rooms conversationally.
- **Live calendar** — SignalR pushes booking changes to every open calendar view instantly.

Built iteratively through multiple rounds of self-driven security/architecture audits and live manual testing, not written once and left untouched.

## Features

**Users** — email-confirmed registration & login, room search/booking with guest participants, edit/cancel, per-room calendar with blackout periods, "My Bookings", in-app notification bell, AI chat, forgot/reset password, optional "remember me" persistence, dark/light theme toggle (persisted per browser), Azerbaijani/English UI language toggle (applies app-wide, including the admin panel).

**Admins** — fully isolated admin login (2FA), CRUD for buildings/floors/rooms/equipment, room blackouts with conflict checking, per-building working hours, **global + per-building system settings with reset-to-global**, user/role management, org-wide reservation oversight, dashboard analytics.

## Architecture & Project Structure

Clean Architecture, strict inward-pointing dependencies:

```
MeetingRoomBookingSystem/
├── Backend/MRBSystem/
│   ├── core/
│   │   ├── MRBSystem.Domain/           # Entities, Enums, IGenericRepository, IUnitOfWork
│   │   ├── MRBSystem.Contracts/        # DTOs (per feature area) + service interfaces
│   │   ├── MRBSystem.Application/      # Services, AutoMapper profile, ExceptionMiddleware
│   │   └── MRBSystem.Shared/           # Custom exceptions
│   ├── infrasturucture/
│   │   ├── MRBSystem.Persistence/      # DbContext, Fluent API configs, migrations, repository
│   │   └── MRBSystem.Infrasturucture/  # EmailService, GeminiChatService, NotificationDeliveryService
│   ├── presentation/MRBSystem.WebAPI/  # Controllers, Program.cs, SignalR Hub
│   └── MRBSystem.Testing/              # xUnit test suite
└── Frontend/
    ├── pages/            # One HTML file per screen
    ├── javascripts/      # api/ (fetch wrappers) · controllers/ (per page) · components/ (shared widgets)
    └── css/              # Design tokens + styles
```

13 controllers: `Auths`, `Buildings`, `Floors`, `Rooms`, `Equipments`, `RoomEquipments`, `RoomBlackouts`, `Reservations`, `Participants`, `Notifications`, `WorkingHours`, `SystemSettings`, `Chat`.

## Design Patterns & SOLID Principles

- **Repository + Unit of Work** — `IGenericRepository<T>` backs every entity; `IUnitOfWork.ExecuteInTransactionAsync` wraps atomic multi-step operations (booking conflict checks, cascading deletes, registration + role assignment).
- **Dependency Injection** everywhere — every service is registered/consumed via an interface.
- **DTO + AutoMapper layer** — entities never leave the Application layer; `Create*Dto`/`Update*Dto`/`Response*Dto` per resource.
- **Middleware pipeline** — one `ExceptionMiddleware` centralizes exception → HTTP status mapping.
- **Single Responsibility** — `AuthService` and `RoleService` were split apart once the original class outgrew "login/register" (found via self-audit, refactored after the fact).
- **Open/Closed** — the per-building settings override extended `ISystemSettingsService`'s contract without changing its consumers.
- **Interface Segregation** — narrow per-feature interfaces (`IWorkingHourService`, `IRoomBlackoutService`, …) instead of one large admin service.
- **Dependency Inversion** — `Application` depends on `IEmailService`/`IChatService` abstractions; concrete SMTP/Gemini implementations live in the outer `Infrasturucture` layer.

## Authentication & Authorization

- ASP.NET Core Identity (`AppUser`/`AppRole`, Guid keys) + JWT Bearer access tokens (60 min) with server-side, rotated **refresh tokens** (30 days).
- **Instant revocation via `SecurityStamp`** — every access token carries it as a claim, checked against the live DB value on every request, so a password/role change invalidates existing tokens immediately.
- Two fully isolated login flows: `/auths/user/login` vs. `/auths/admin/login` (+ mandatory 6-digit email 2FA). Admin pages never link to/from user pages.
- Email confirmation required to log in; anti-enumeration forgot/reset password via a 6-digit emailed code.
- Account lockout (5 attempts / 15 min); rate limiting on all auth endpoints (5/min/IP) and AI chat (10/min/user).
- `[Authorize(Roles = "Admin")]` plus per-resource ownership checks (`EnsureCanModify`, `EnsureCanRespond`).
- Frontend: tokens live in `sessionStorage` by default; "Remember me" switches to `localStorage` so the session survives a browser restart — a client-side-only decision, nothing changes server-side.

## Database & EF Core

- **Code-First**, 13 migrations. **Fluent API only** (13 `IEntityTypeConfiguration<T>` classes) — entities carry no EF attributes.
- Delete behavior chosen per relationship, not by default: `Restrict` where a child has independent meaning (`Floor→Building`, `Room→Floor`, `Reservation→Room`), `Cascade` only for truly dependent rows (`RoomEquipment`, `RoomBlackout`, per-building `SystemSettings`/`BuildingWorkingHour`).
- **Global UTC conversion** via a single `ValueConverter`; local (Asia/Baku) time is computed transiently in the service layer through `ITimeZoneService`.
- `.AsNoTracking()` on every read path; explicit `_dbSet.Update(...)` on every write.
- Double-booking is prevented by wrapping reservation create/update in a `Serializable`-isolation transaction.
- 11 entities: `Building`, `Floor`, `Room`, `Equipment`, `RoomEquipment`, `RoomBlackout`, `Reservation`, `Participant`, `Notification`, `RefreshToken`, `BuildingWorkingHour`, `SystemSettings` + Identity's `AppUser`/`AppRole`.

## Business Logic & Validation

1. **Field-level** — DataAnnotations on every DTO (lengths matched to EF `HasMaxLength`), `IValidatableObject` for cross-field checks (`StartDate < EndDate`).
2. **Business rules** (service layer, needs DB state):
   - Room must be active; duration within `Min`/`MaxReservationMinutes`/`Hours`; a configurable **buffer** between adjacent bookings.
   - Must fall inside the building's **working hours** (one shared `IWorkingHourService` check used by both search and create/update).
   - Non-admins capped by a **booking horizon**; optional **active-reservation limit** per user.
   - **Blackout periods** block overlapping bookings (and vice versa).
   - **Per-building overrides**: `SystemSettings.BuildingId` is nullable — `null` is the global default, a building's own row silently takes priority for its rooms while other buildings keep following global.
   - Ownership checks on modify/cancel/participant access.

## API & DTO Structure

- Resource controllers under `/api/{resource}` (e.g. `/api/rooms/search-available`, `/api/system-settings/{buildingId}`).
- **54 DTOs**, one folder per feature area, split into `Create`/`Update`/`Response` variants.
- `CreateMap<CreateXDto, Entity>(MemberList.Source)` for partial write DTOs, since `Id`/`CreatedAt`/nav-props are intentionally left to EF.
- Response DTOs flatten read-only context (`ResponseRoomDto.BuildingName`/`BuildingId`) computed at mapping time — never duplicated in the schema.

## Exception Handling & Logging

- One `ExceptionMiddleware` maps exception types to HTTP status (`NotFoundException`→404, `ForbiddenException`→403, `ArgumentException`→400, `InvalidOperationException`/`DbUpdateException`→409, else 500) and returns `{ statusCode, message }`.
- Custom exceptions live in `MRBSystem.Shared`, reachable from every layer.
- **User-facing messages stay Azerbaijani** (shown directly in the UI); **log output stays English** (`"{StatusCode}: {ExceptionType}"`) — a deliberate split so logs stay consistent regardless of what language a given rule's message is in.
- Serilog → Console + rolling daily file.

## Background & Real-Time Features

- **`RoomCalendarHub`** (SignalR, JWT-authenticated, `/hubs/room-calendar`) — per-room groups; `SignalRRoomCalendarBroadcastService` pushes a live update on every reservation create/update/cancel. Frontend shows a banner on disconnect/reconnect.
- **`NotificationDeliveryService`** (`IHostedService`) — background email outbox, retries the "sent" write up to 3×.
- **AI chat** (`GeminiChatService`) — Gemini function-calling triggers real backend lookups (room search, buildings) instead of only generating text.

## Admin Panel

Fully separate frontend section (`admin*.html`, own sidebar, own `requireAdmin()` guard): Dashboard, Buildings/Rooms/Equipment CRUD (with usage checks before delete), Blackouts, Working Hours, global/per-building Settings (with reset-to-global), Users/Roles, org-wide Reservations.

## Testing

xUnit + Moq + FluentAssertions, backed by **EF Core over in-memory SQLite** (a real relational DB per test, not a mocked `DbContext`). **14 test classes, 84 tests** — one per service plus `AutoMapperConfigurationTests` (`AssertConfigurationIsValid()`, which has caught real mapping bugs before runtime).

```bash
dotnet test Backend/MRBSystem/MRBSystem.Testing/MRBSystem.Testing.csproj
```

## Installation & Setup

**Prerequisites**: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), SQL Server, an SMTP account, a Gemini API key (for AI chat).

```bash
cd Backend/MRBSystem
dotnet restore && dotnet build

dotnet user-secrets init --project presentation/MRBSystem.WebAPI
dotnet user-secrets set "ConnectionStrings:MRBSystemConnectionString" "Server=.;Database=MRBSystemDb;Trusted_Connection=True;TrustServerCertificate=True" --project presentation/MRBSystem.WebAPI
# ...set the remaining keys from Configuration below

dotnet ef database update --project infrasturucture/MRBSystem.Persistence --startup-project presentation/MRBSystem.WebAPI

dotnet run --project presentation/MRBSystem.WebAPI --launch-profile https
```

API: `https://localhost:7114` (Swagger at `/swagger`); an Admin account is auto-seeded from `SeedAdmin:*`.

**Frontend** is static (no build step) — serve `Frontend/` with a static server (e.g. VS Code Live Server), don't open via `file://`. The backend must run on **HTTPS**: `Program.cs` calls `UseHttpsRedirection()` before `UseCors()`, so an HTTP-origin CORS preflight gets redirected and rejected by the browser. Open `Frontend/pages/landing.html`, `login.html`, or `adminLogin.html`.

## Configuration

Secrets via **.NET User Secrets** (never committed):

| Key | Purpose |
|---|---|
| `ConnectionStrings:MRBSystemConnectionString` | SQL Server connection |
| `Jwt:Key` / `Issuer` / `Audience` / `AccessTokenExpirationMinutes` / `RefreshTokenExpirationDays` | JWT signing & lifetimes (60 min / 30 days) |
| `SeedAdmin:Email` / `Password` / `FirstName` / `LastName` | First-run admin bootstrap |
| `Smtp:*` | Outgoing email (confirmation, 2FA, reset) |
| `Gemini:ApiKey` | AI chat |
| `Cors:AllowedOrigins` | Allowed frontend origins |
| `Localization:TimeZoneId` | `Asia/Baku` |

`Frontend/javascripts/api/apiClient.js`'s `API_BASE_URL` must match the backend's origin.

## API Documentation (Swagger)

`https://localhost:7114/swagger` (Development only) — includes a Bearer auth scheme; click "Authorize" and paste `Bearer {token}`.

---

Every feature above was built, run, and re-checked against a live backend and browser before being called done — not just written once and assumed to work.
