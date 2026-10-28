# MeetHub — Meeting Room Booking System

A full-stack meeting room booking platform. Employees search, book, and manage meeting rooms across multiple buildings; admins manage the organization's structure, reservations, and system-wide rules — with an AI chatbot, live calendar updates, and email notifications on top.

**🌐 Live app → [meethub-mrbsystem.netlify.app](https://meethub-mrbsystem.netlify.app)**

[![Live Demo](https://img.shields.io/badge/Live_Demo-Online-brightgreen?logo=netlify&logoColor=white)](https://meethub-mrbsystem.netlify.app)

![.NET 8](https://img.shields.io/badge/.NET_8-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-5C2D91?logo=dotnet&logoColor=white)
![Entity Framework Core](https://img.shields.io/badge/EF_Core-512BD4)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?logo=microsoftsqlserver&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)
![Docker Compose](https://img.shields.io/badge/Docker_Compose-2496ED?logo=docker&logoColor=white)
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
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)
![Google Gemini](https://img.shields.io/badge/Google_Gemini-8E75B2?logo=googlegemini&logoColor=white)

**Deployment:**

![Netlify](https://img.shields.io/badge/Netlify-00C7B7?logo=netlify&logoColor=white)
![Render](https://img.shields.io/badge/Render-000000?logo=render&logoColor=white)
![Neon](https://img.shields.io/badge/Neon_Postgres-00E599?logo=postgresql&logoColor=white)
![Brevo](https://img.shields.io/badge/Brevo-0B996E?logo=brevo&logoColor=white)
![Auto Deploy](https://img.shields.io/badge/CD-Auto_Deploy_on_Push-2088FF)

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

---

## Deployment

MeetHub runs as a fully live, **zero-cost** production deployment with continuous delivery — every push to `master` automatically redeploys the affected tier.

**Live app:** https://meethub-mrbsystem.netlify.app

| Tier | Platform | Details |
|------|----------|---------|
| **Frontend** | [Netlify](https://www.netlify.com/) | Static `Frontend/` published directly (no build step); auto-deploys on every push. |
| **Backend API** | [Render](https://render.com/) | ASP.NET Core WebAPI shipped as a Docker image from `Backend/MRBSystem`; auto-deploys on every push. The free instance sleeps after ~15 min idle, so the first request after idle has a ~30–50 s cold start. |
| **Database** | [Neon](https://neon.tech/) | Serverless **PostgreSQL** — the project was migrated from SQL Server to PostgreSQL for a managed, cloud-native, free database. EF Core migrations apply automatically on backend startup. |
| **Email** | [Brevo](https://www.brevo.com/) | Transactional email (email confirmation, 2FA, password reset) over Brevo's HTTPS API, since the free backend host blocks outbound SMTP. |

The frontend selects its API base URL by hostname (`localhost` → local backend, otherwise the Render URL), so the same codebase runs locally and in production without edits.

**Try the live demo** — seeded with professional room names, ~2 months of reservations, and blackout periods:

| Role | Email | Password |
|------|-------|----------|
| User | `neontest@example.com` | `Salam123!` |

> The admin panel has its own separate, 2FA-protected login.
>
> ⏳ The first request after idle can take ~30–50 s while the free Render backend wakes from sleep.

---

## Installation & Setup

### Option 1: Local Development (Recommended for Development)

**Prerequisites**: 
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL (local instance or Docker)
- Brevo API key (or an SMTP account) for email
- Gemini API key

**Steps:**

1. **Database Setup** (PostgreSQL)
   ```bash
   cd Backend/MRBSystem
   ```

2. **Configure Secrets** (User Secrets)
   ```bash
   dotnet user-secrets init --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "ConnectionStrings:MRBSystemConnectionString" "Host=localhost;Port=5432;Database=MRBSystemDb;Username=postgres;Password=YOUR_PASSWORD" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "Jwt:Key" "your-super-secret-key-minimum-32-characters-long" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "Jwt:Issuer" "MRBSystem" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "Jwt:Audience" "MRBSystemUsers" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "SeedAdmin:Password" "Admin@123456" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "Smtp:Host" "smtp.gmail.com" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "Smtp:Port" "587" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "Smtp:Email" "your-email@gmail.com" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "Smtp:Password" "your-app-password" --project presentation/MRBSystem.WebAPI
   dotnet user-secrets set "Gemini:ApiKey" "your-gemini-api-key" --project presentation/MRBSystem.WebAPI
   ```

3. **Database Migration**
   ```bash
   dotnet ef database update --project infrasturucture/MRBSystem.Persistence --startup-project presentation/MRBSystem.WebAPI
   ```

4. **Run Backend**
   ```bash
   dotnet run --project presentation/MRBSystem.WebAPI --launch-profile https
   ```
   API: `https://localhost:7114` (Swagger: `/swagger`)

5. **Run Frontend**
   - Use VS Code Live Server or similar static server on `Frontend/` directory
   - Update `Frontend/javascripts/api/apiClient.js`: `API_BASE_URL = "https://localhost:7114/api"`
   - Open `http://localhost:5500/pages/landing.html` (or your server's URL)

### Option 2: Docker Compose (Recommended for Deployment)

**Prerequisites**:
- Docker & Docker Compose

**Steps:**

1. **Create `.env` from template**
   ```bash
   cp .env.example .env
   ```

2. **Update `.env` with your values**
   ```env
   DB_USER=postgres
   DB_PASSWORD=your_password
   DB_NAME=MRBSystemDb
   DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=MRBSystemDb;Username=postgres;Password=your_password
   JWT_KEY=your-super-secret-key-minimum-32-characters-long
   JWT_ISSUER=MRBSystem
   JWT_AUDIENCE=MRBSystemUsers
   ADMIN_EMAIL=admin@example.com
   ADMIN_PASSWORD=Admin@123456
   SMTP_HOST=smtp.gmail.com
   SMTP_PORT=587
   SMTP_USERNAME=your-email@gmail.com
   SMTP_PASSWORD=your-app-password
   SMTP_FROMEMAIL=your-email@gmail.com
   SMTP_FROMNAME=Meeting Room Booking System
   SMTP_ENABLESSL=true
   GEMINI_API_KEY=your-gemini-api-key
   BREVO_API_KEY=your-brevo-api-key
   FRONTEND_URL=http://localhost:80
   TIMEZONE=Asia/Baku
   ```

3. **Start Containers**
   ```bash
   docker compose up -d
   ```

4. **Database Migration** — automatic. On startup the backend applies all EF Core migrations and waits for PostgreSQL to become ready (retry loop), so no manual migration step is needed.

5. **Access Application**
   - Frontend: `http://localhost`
   - Backend API: `http://localhost:8080`
   - PostgreSQL: `localhost:5432`

6. **Stop Containers**
   ```bash
   docker compose down
   ```

---

## Docker Services

| Service | Port | Container | Purpose |
|---------|------|-----------|---------|
| Frontend | 80 | mrb-frontend | Static HTML/JS/CSS server |
| Backend API | 8080 | mrb-backend | ASP.NET Core WebAPI |
| PostgreSQL | 5432 | mrb-postgres | Database |

**Volume:** `postgres-data` — persists database across container restarts

**Network:** `mrb-network` — internal communication between services

---

## Configuration

### Local (User Secrets)
Secrets stored in: `%APPDATA%\Microsoft\UserSecrets\aebb6b86-05bf-4d0d-a3ed-436c2b5fc710\` (Windows) or `~/.microsoft/usersecrets/...` (Linux/Mac)

### Docker (Environment Variables)
Secrets stored in `.env` file (**git-ignored**, never committed)

| Key | Purpose |
|---|---|
| `DB_USER` / `DB_PASSWORD` / `DB_NAME` | PostgreSQL user, password & database name |
| `DB_CONNECTION_STRING` | Database connection (Npgsql) |
| `JWT_KEY` | JWT signing key (min 32 chars) |
| `JWT_ISSUER` / `JWT_AUDIENCE` | Token issuer/audience |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | First-run admin account |
| `SMTP_*` | Email configuration |
| `GEMINI_API_KEY` | AI chat API key |
| `BREVO_API_KEY` | Brevo email API key |
| `FRONTEND_URL` | Frontend origin for CORS |
| `TIMEZONE` | `Asia/Baku` |

---

## API Documentation (Swagger)

**Development Only**: `https://localhost:7114/swagger`

- Bearer auth scheme available
- Click "Authorize" and paste: `Bearer {token}`

---

## Default Admin Credentials

- **Email**: `admin@meethub.local` (or configured in `.env` / User Secrets)
- **Password**: `Admin@123456` (or configured in `.env` / User Secrets)

Login at: `http://localhost/pages/adminLogin.html`

---

Every feature above was built, run, and re-checked against a live backend and browser before being called done — not just written once and assumed to work.
