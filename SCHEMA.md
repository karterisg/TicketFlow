# TicketFlow — Database Schema

Πίνακες (tables) και συσχετίσεις (relationships), όπως προκύπτουν από `TicketFlow.Shared/Domain` και το `AppDbContext.OnModelCreating`.

## Πίνακες

### Users (`AspNetUsers` extended)
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| FullName | string(100) | required |
| Email | string(200) | required, unique |

### Agents
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| FullName | string(100) | required |
| Email | string(200) | required, unique |
| IsAvailable | bool | |

### Categories
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string(100) | required |

### Tickets
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Title | string(100) | required |
| Description | string(2000) | required |
| Status | string (enum) | Open/InProgress/Resolved/Closed |
| Priority | string (enum) | |
| ResolvedAt | datetime? | |
| DueDate | datetime? | |
| TicketNumber | int | sequential, DB sequence `TicketNumberSeq` |
| UserId | int | FK → Users, cascade delete |
| AgentId | int? | FK → Agents, set null on delete |
| CategoryId | int | FK → Categories, cascade delete |
| ProjectId | int? | FK → Projects, set null on delete |
| IsDeleted / DeletedAt | soft delete | query filter: `!IsDeleted` |

### Comments
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Body | string(1000) | required |
| TicketId | int | FK → Tickets, cascade delete |
| AuthorId | int | FK → Users, no action |

### TimeEntries
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| TicketId | int | FK → Tickets |
| AgentId | int | FK → Agents |
| StartedAt | datetime | |
| EndedAt | datetime? | |
| ManualMinutes | int? | manual log override |
| Note | string? | |

### Projects
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string(100) | required, unique |
| Description | string? | |
| Status | string (enum) | Active/Archived |
| IconKey | string(50) | required |
| ProjectCategoryId | int? | FK → ProjectCategories, set null |
| OwnerUserId | int | FK → Users (via ProjectMember, not a hard FK on Project) |
| AllowCustomerTicketCreation | bool | |
| RequireApprovalForClose | bool | |
| NotificationEmail | string? | |
| IsDeleted / DeletedAt | soft delete | query filter: `!IsDeleted` |

### ProjectCategories
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string(100) | required |

### ProjectTemplates
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string(100) | required |
| Description | string? | |
| DefaultProjectCategoryId | int? | FK → ProjectCategories, set null |
| DefaultIconKey | string(50) | required |
| DefaultAllowCustomerTicketCreation | bool | |
| DefaultRequireApprovalForClose | bool | |
| Timeline | string(30) | |

### ProjectMembers
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| ProjectId | int | FK → Projects, cascade delete |
| UserId | int? | FK → Users, no action; unique with ProjectId when set |
| AgentId | int? | FK → Agents, no action; unique with ProjectId when set |
| Role | string (enum) | Viewer / Member / Owner |

### Teams *(new)*
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Name | string(100) | required, unique |
| Description | string? | |
| OwnerUserId | int | FK → Users (via TeamMember) |
| IsDeleted / DeletedAt | soft delete | query filter: `!IsDeleted` |

### TeamMembers *(new)*
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| TeamId | int | FK → Teams, cascade delete |
| UserId | int? | FK → Users, no action; unique with TeamId when set |
| AgentId | int? | FK → Agents, no action; unique with TeamId when set |
| Role | string (enum) | Member / Lead |

### ProjectTeams *(new — link table)*
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| ProjectId | int | FK → Projects, cascade delete |
| TeamId | int | FK → Teams, cascade delete |
| — | | unique index on (ProjectId, TeamId) |

### Meetings *(new)*
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| Title | string(150) | required |
| Description | string? | |
| ProjectId | int? | FK → Projects, set null |
| TeamId | int? | FK → Teams, set null |
| CreatedByUserId | int | FK → Users |
| ScheduledAt | datetime | required, must be future on create |
| DurationMinutes | int | default 30 |
| LocationOrLink | string? | room, URL, etc. |
| Status | string (enum) | Scheduled / Cancelled / Completed |
| IsDeleted / DeletedAt | soft delete | query filter: `!IsDeleted` |

### MeetingAttendees *(new)*
| Column | Type | Notes |
|---|---|---|
| Id | int | PK |
| MeetingId | int | FK → Meetings, cascade delete |
| UserId | int? | FK → Users, no action |
| AgentId | int? | FK → Agents, no action |
| Response | string (enum) | Pending / Accepted / Declined |

---

## Συσχετίσεις (Relationships)

```
User ──1───────*── Ticket            (User.Id = Ticket.UserId, cascade)
Agent ──1──────*── Ticket            (Agent.Id = Ticket.AgentId, set null)
Category ──1───*── Ticket            (Category.Id = Ticket.CategoryId, cascade)
Project ──1────*── Ticket            (Project.Id = Ticket.ProjectId, set null)
Ticket ──1─────*── Comment           (cascade)
User ──1───────*── Comment           (as Author, no action)
Ticket ──1─────*── TimeEntry
Agent ──1──────*── TimeEntry

ProjectCategory ──1───*── Project    (set null)
Project ──1───────────*── ProjectMember   (cascade)
User/Agent ──1────────*── ProjectMember   (no action)

Project ──*───────*── Team           via ProjectTeam (link table, both cascade)
Team ──1───────────*── TeamMember    (cascade)
User/Agent ──1─────*── TeamMember    (no action)

Project ──1────────*── Meeting       (set null, optional)
Team ──1───────────*── Meeting       (set null, optional)
User ──1───────────*── Meeting       (as organizer, CreatedByUserId)
Meeting ──1────────*── MeetingAttendee   (cascade)
User/Agent ──1─────*── MeetingAttendee   (no action)

ProjectTemplate ──*──1── ProjectCategory  (DefaultProjectCategoryId, set null)
```

### Σημειώσεις
- **Team ↔ Project**: πολλά-προς-πολλά μέσω `ProjectTeam` — μια ομάδα μπορεί να αναλάβει πολλά projects, ένα project μπορεί να έχει πολλές ομάδες.
- **Meeting** συνδέεται είτε με `Project` είτε με `Team` (και τα δύο nullable, τουλάχιστον ένα απαιτείται στο business layer, όχι στο DB constraint).
- Soft-deletable entities (`IsDeleted`/`DeletedAt`): Ticket, Project, Team, Meeting — έχουν EF query filter ώστε τα διαγραμμένα να μη γυρνάνε σε κανονικά queries.
- Οι πίνακες δημιουργούνται μέσω EF Core migrations (`src/TicketFlow.API/Migrations`), εφαρμόζονται με `dotnet ef database update`. Η βάση είναι PostgreSQL (μέχρι 2026-09 ήταν SQL Server).
