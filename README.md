# TicketFlow

Ένα full-stack ticketing / helpdesk σύστημα με project & team management, χτισμένο με ASP.NET Core (API) + Blazor Server (UI), πάνω σε PostgreSQL.

## Στόχος

Ένα σύστημα διαχείρισης tickets υποστήριξης που δεν μένει μόνο σε "άνοιξε ticket / κλείσε ticket", αλλά οργανώνει τη δουλειά γύρω από **Projects** και **Teams** — ώστε ένας οργανισμός να μπορεί να ομαδοποιεί tickets ανά πελάτη/έργο, να αναθέτει ομάδες agents σε projects, και να έχει λεπτομερή εικόνα (χρόνος εργασίας, ικανοποίηση πελάτη, meetings) πάνω από την απλή ουρά αιτημάτων. Ρόλοι (Customer / Agent / Manager) συνδυάζονται με granular permissions ανά χρήστη, ώστε τα δικαιώματα να μπορούν να προσαρμοστούν χωρίς να χρειάζεται νέος ρόλος για κάθε ειδική περίπτωση.

## Βασικά features

- **Tickets** — δημιουργία, ανάθεση σε agent, status/priority, due dates, αυτόματος αύξων αριθμός (`TicketNumberSeq`), σχόλια, ιστορικό ενεργειών (audit trail), soft delete, export σε Excel (ClosedXML)
- **Projects** — projects με κατηγορίες, templates, μέλη με ρόλους (Viewer/Member/Owner), ρυθμίσεις όπως "customer can create ticket", "require approval to close"
- **Teams** — ομάδες με μέλη (users ή agents) και ρόλους (Member/Lead), ανάθεση σε πολλαπλά projects (many-to-many μέσω `ProjectTeam`)
- **Meetings** — προγραμματισμός meetings συνδεδεμένων με project ή team, attendees με response status (Pending/Accepted/Declined)
- **Time tracking** — καταγραφή χρόνου agent ανά ticket (`TimeEntry`), με manual override σε λεπτά
- **Ratings / CSAT** — βαθμολόγηση ικανοποίησης πελάτη ανά ticket
- **Real-time notifications** — μέσω SignalR (`NotificationHub`, per-user groups) + αυτόματο cleanup παλιών notifications μέσω Hangfire background job
- **Email** — ειδοποιήσεις email κατά τη δημιουργία ticket (background job μέσω Hangfire), μέσω [Resend](https://resend.com)
- **Reports** — στατιστικά / analytics πάνω στα tickets
- **Granular permissions** — πέρα από τους 3 βασικούς ρόλους, υπάρχει σύστημα permissions (`Tickets.ViewAll`, `Tickets.Assign`, `Users.Manage`, `Reports.View`, κ.ά. — δες `PermissionCatalog.cs`) που ανατίθενται ανά χρήστη

## Tech stack

| Layer | Τεχνολογία |
|---|---|
| Backend API | ASP.NET Core 10 Web API |
| Frontend | Blazor Server + [MudBlazor](https://mudblazor.com) |
| Database | PostgreSQL 16 (EF Core 9 / Npgsql, Code-First migrations) |
| Auth | ASP.NET Identity + JWT bearer tokens |
| Real-time | SignalR |
| Background jobs | Hangfire (in-memory storage σε dev) |
| Validation | FluentValidation |
| Logging | Serilog (console + file) |
| Excel export | ClosedXML |
| Email | Resend API |
| Testing | xUnit (EF Core InMemory provider) |

## Πώς είναι χτισμένο (αρχιτεκτονική)

Το project είναι χτισμένο **bottom-up**, layer πάνω σε layer, ξεκινώντας από το domain model:

1. **Domain (`TicketFlow.Shared/Domain`)** — τα βασικά entities (`Ticket`, `User`, `Agent`, `Project`, `Team`, `Meeting`, `Comment`, `TimeEntry`, `Rating`, `Permission`, κ.ο.κ.), χωρίς καμία εξάρτηση από EF Core ή HTTP. Κοινά behaviors μοντελοποιούνται με interfaces:
   - `BaseEntity` — `Id`, `CreatedAt`, `UpdatedAt`
   - `IAuditable` — entities που παρακολουθούν πότε δημιουργήθηκαν/ενημερώθηκαν
   - `ISoftDeletable` — entities με soft-delete (`IsDeleted`, `DeletedAt`) αντί για πραγματικό delete
2. **DTOs (`TicketFlow.Shared/DTOs`)** — ξεχωριστά αντικείμενα μεταφοράς δεδομένων για κάθε entity, ώστε το API να μη διαρρέει ποτέ τα raw domain entities (ή το EF Core change tracking) προς τα έξω.
3. **EF Core `AppDbContext` + Migrations (`TicketFlow.API/Data`, `TicketFlow.API/Migrations`)** — Code-First: ο κώδικας (`OnModelCreating`) ορίζει σχέσεις, indexes, constraints· τα migrations παράγονται από αυτόν και χτίζουν το πραγματικό PostgreSQL schema.
4. **Repositories (`TicketFlow.API/Repositories` + `Interfaces`)** — ένα repository ανά aggregate (`TicketRepository`, `ProjectRepository`, `TeamRepository`, ...), αποκλειστικά query/persistence λογική πάνω στο `AppDbContext`.
5. **Services (`TicketFlow.API/Services`)** — business rules πάνω από τα repositories (π.χ. "μόνο Manager μπορεί να κλείσει ticket χωρίς έγκριση", ανάθεση default permissions κατά το registration).
6. **Mapping (`TicketFlow.API/Mapping`)** — χειροκίνητοι mappers entity ↔ DTO (π.χ. `TicketMapper`, `ProjectMapper`) αντί για reflection-based auto-mapper, για προβλέψιμη, γρήγορη μετατροπή.
7. **Validation (`TicketFlow.API/Validators`)** — FluentValidation validators ανά incoming request (`CreateTicketValidator`, `CreateProjectValidator`, ...).
8. **Controllers (`TicketFlow.API/Controllers`)** — το πραγματικό REST API, ένα controller ανά resource, λεπτό layer πάνω από τα services.
9. **Authorization (`TicketFlow.API/Authorization`)** — custom `IAuthorizationPolicyProvider`/`IAuthorizationHandler` που κάνει resolve permission-based policies δυναμικά (χωρίς να χρειάζεται να δηλωθεί κάθε permission ξεχωριστά στο `Program.cs`).
10. **Jobs (`TicketFlow.API/Jobs`)** — background εργασίες μέσω Hangfire (email κατά τη δημιουργία ticket, cleanup παλιών notifications).
11. **Web UI (`TicketFlow.Web`)** — Blazor Server + MudBlazor, μιλάει με το API αποκλειστικά μέσω `HttpClient` (`ApiService`), ποτέ απευθείας με τη βάση.

Αυτή η σειρά (domain → DTOs → DbContext/migrations → repositories → services → controllers → UI) είναι και η φυσική σειρά με την οποία θα πρόσθετε κανείς ένα νέο feature στο project.

## Δομή του project

```
TicketFlow.slnx
├── src/
│   ├── TicketFlow.Shared/          # Domain entities + DTOs — καμία εξάρτηση, το χρησιμοποιούν και τα δύο άλλα projects
│   │   ├── Domain/
│   │   └── DTOs/
│   ├── TicketFlow.API/             # ASP.NET Core Web API
│   │   ├── Controllers/            # REST endpoints
│   │   ├── Services/                # business logic
│   │   ├── Repositories/            # data access πάνω στο EF Core
│   │   ├── Interfaces/              # contracts για repositories/services (DI)
│   │   ├── Data/                    # AppDbContext
│   │   ├── Migrations/              # EF Core migrations (PostgreSQL)
│   │   ├── Mapping/                  # entity ↔ DTO mappers
│   │   ├── Validators/               # FluentValidation
│   │   ├── Authorization/            # permission-based auth policies
│   │   ├── Jobs/                     # Hangfire background jobs
│   │   ├── Hubs/                     # SignalR (NotificationHub)
│   │   ├── Reporting/                 # Excel export
│   │   ├── Middleware/                # global exception handling
│   │   └── Program.cs                 # DI container, pipeline, seeding
│   ├── TicketFlow.Web/              # Blazor Server UI (MudBlazor)
│   │   ├── Components/Pages/         # μία σελίδα/φάκελος ανά resource (Tickets, Projects, Teams, Meetings, Users, ...)
│   │   └── Services/                  # ApiService (HttpClient wrapper), AuthService, NotificationState
│   └── TicketFlow.Tests/            # xUnit unit tests
├── docker-compose.yml               # Προαιρετικό — Postgres + Redis + API + Web σε containers
├── run.sh                           # Τρέχει API + Web μαζί τοπικά (χωρίς Docker/IDE)
└── SCHEMA.md                        # Αναλυτικό schema βάσης (πίνακες, σχέσεις)
```

## Database & Migrations

- Η βάση είναι **PostgreSQL**, το EF Core provider είναι **Npgsql**.
- Το schema **δεν** γράφεται ποτέ απευθείας σε SQL — ορίζεται στον κώδικα (entities + `AppDbContext.OnModelCreating`) και τα migrations παράγονται αυτόματα από εκεί.
- Τα migration αρχεία ζουν στο `src/TicketFlow.API/Migrations/`.

Για να δεις τι θα άλλαζε το schema χωρίς να εφαρμοστεί κάτι:
```bash
cd src/TicketFlow.API
dotnet ef migrations list
```

Αν αλλάξεις κάποιο entity (π.χ. προσθέσεις property σε `Ticket`), δημιούργησε νέο migration και εφάρμοσέ το:
```bash
cd src/TicketFlow.API
dotnet ef migrations add PerigrafiTisAllagis
dotnet ef database update
```

## Getting Started

Προϋποθέσεις (και τα τρία, ανεξαρτήτως αν θα τρέξεις από terminal, Visual Studio ή VS Code):
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 16 (τοπικά — π.χ. `brew install postgresql@16` σε Mac, ή ο επίσημος installer σε Windows)
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

### 1. Clone & database setup (μία φορά)

```bash
git clone <repo-url>
cd ticket

# Δημιούργησε dedicated DB role + database
psql postgres -c "CREATE ROLE ticketflow_user WITH LOGIN PASSWORD 'your_password';"
psql postgres -c "CREATE DATABASE ticketflow_dev OWNER ticketflow_user;"
```

### 2. Secrets (μία φορά, ποτέ στο `appsettings.json` / git)

```bash
cd src/TicketFlow.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=ticketflow_dev;Username=ticketflow_user;Password=your_password"
dotnet user-secrets set "JwtSettings:SecretKey" "$(openssl rand -base64 48)"
cd ../..
```

### 3. Εφάρμοσε τα migrations (μία φορά, ή κάθε φορά που προστίθεται νέο migration)

```bash
cd src/TicketFlow.API
dotnet ef database update
cd ../..
```

### 4. Τρέξε το εφαρμογή

Τρεις ισοδύναμοι τρόποι — διάλεξε ό,τι σου βολεύει:

#### Α. Terminal (πιο απλό, δουλεύει παντού)

```bash
./run.sh
```
Σηκώνει API + Web μαζί σε ένα terminal (με `[api]`/`[web]` prefix στα logs), Ctrl+C σταματάει και τα δύο.

#### Β. Visual Studio

1. Άνοιξε το `TicketFlow.slnx`
2. Right-click στο project `TicketFlow.API` → **Manage User Secrets** — επιβεβαίωσε ότι το `ConnectionStrings:DefaultConnection` και το `JwtSettings:SecretKey` είναι εκεί (τα έβαλες ήδη στο βήμα 2 παραπάνω, οπότε θα τα δεις κατευθείαν)
3. Το solution έχει ήδη ρυθμισμένα **multiple startup projects** (`TicketFlow.slnLaunch.user`) — API + Web ξεκινάνε μαζί με το https profile
4. Πάτα **▶ https** (πράσινο κουμπί) — ανοίγουν και τα δύο μαζί

#### Γ. VS Code

1. Άνοιξε τον φάκελο του project (`code .`)
2. Πρότεινε την επέκταση **C# Dev Kit** (Microsoft) αν δεν την έχεις, για syntax highlighting/IntelliSense
3. Από το integrated terminal, ίδια εντολή με το terminal setup:
   ```bash
   ./run.sh
   ```
   ή, αν θες να τρέξεις ένα project τη φορά σε ξεχωριστά terminals:
   ```bash
   cd src/TicketFlow.API && dotnet run --launch-profile https
   cd src/TicketFlow.Web && dotnet run --launch-profile https
   ```

Και στα τρία σενάρια, το αποτέλεσμα είναι το ίδιο:

- API: `https://localhost:7216` (Swagger στο `/swagger`, μόνο σε Development)
- Web UI: `https://localhost:7075`

Πρώτη φορά; Δεν υπάρχει προκαθορισμένος λογαριασμός — πήγαινε στο `/register`, διάλεξε ρόλο `Manager` για πλήρη πρόσβαση.

### Με Docker (προαιρετικό — εναλλακτικό του βήματος 1-4)

```bash
cp .env.example .env   # συμπλήρωσε πραγματικά passwords
docker compose up
```
Σηκώνει Postgres + Redis + API + Web σε containers, χωρίς να χρειάζεται τοπική εγκατάσταση .NET/Postgres.

## Authentication & Authorization

- Register/Login επιστρέφουν JWT token (`AuthController`)
- 3 βασικοί ρόλοι: `Customer`, `Agent`, `Manager` (ASP.NET Identity roles)
- Πάνω από αυτό, granular permissions per user (`UserPermission` πίνακας) που ελέγχονται μέσω custom `IAuthorizationPolicyProvider`/`IAuthorizationHandler` (`PermissionPolicyProvider`, `PermissionAuthorizationHandler`) — επιτρέπει π.χ. σε συγκεκριμένο Agent να έχει `Tickets.Export` χωρίς να είναι Manager
- Το Hangfire dashboard (`/hangfire`) απαιτεί authenticated χρήστη με ρόλο `Manager`
- Τα πραγματικά secrets (JWT signing key, connection string, API keys) δεν βρίσκονται ποτέ στον κώδικα — ζουν μόνο σε `dotnet user-secrets` τοπικά, ή σε `.env` (gitignored) όταν τρέχει μέσω Docker
