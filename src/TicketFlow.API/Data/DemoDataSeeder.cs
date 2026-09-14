using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketFlow.Shared.Domain;

namespace TicketFlow.API.Data;

// One-shot demo data generator: creates a fully cross-referenced dataset
// (people, teams, projects, tickets, meetings, permissions...) with working
// login credentials for every person. Safe to run multiple times — skips
// itself once the marker account already exists.
public static class DemoDataSeeder
{
    private const string Password = "Password123!";

    public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        if (await userManager.FindByEmailAsync("nikos@gmail.com") is not null)
            return;

        var catBug = await GetOrCreateCategoryAsync(db, "Bug");
        var catFeature = await GetOrCreateCategoryAsync(db, "Feature Request");
        var catSupport = await GetOrCreateCategoryAsync(db, "General Support");

        var pcClient = await GetOrCreateProjectCategoryAsync(db, "Client Work");
        var pcInternal = await GetOrCreateProjectCategoryAsync(db, "Internal");

        var (_, managerUser) = await CreateManagerAsync(db, userManager, "Nikos");

        var (_, giorgos) = await CreateAgentAsync(db, userManager, "Giorgos");
        var (_, maria) = await CreateAgentAsync(db, userManager, "Maria");
        var (_, kostas) = await CreateAgentAsync(db, userManager, "Kostas");

        var (_, eleni) = await CreateCustomerAsync(db, userManager, "Eleni");
        var (dimitrisIdentity, dimitris) = await CreateCustomerAsync(db, userManager, "Dimitris");
        var (_, sofia) = await CreateCustomerAsync(db, userManager, "Sofia");
        var (_, andreas) = await CreateCustomerAsync(db, userManager, "Andreas");

        var supportTeam = new Team("Support Team", "Frontline ticket support", managerUser.Id);
        var devTeam = new Team("Dev Team", "Feature development & bug fixes", managerUser.Id);
        db.Teams.AddRange(supportTeam, devTeam);
        await db.SaveChangesAsync();

        db.TeamMembers.AddRange(
            new TeamMember { TeamId = supportTeam.Id, AgentId = giorgos.Id, Role = TeamRole.Lead },
            new TeamMember { TeamId = supportTeam.Id, AgentId = maria.Id, Role = TeamRole.Member },
            new TeamMember { TeamId = devTeam.Id, AgentId = kostas.Id, Role = TeamRole.Lead },
            new TeamMember { TeamId = devTeam.Id, UserId = managerUser.Id, Role = TeamRole.Member });
        await db.SaveChangesAsync();

        var websiteProject = new Project("Website Revamp", "Redesign of the public marketing site", managerUser.Id, pcClient.Id, "globe");
        var internalToolsProject = new Project("Internal Tools", "Internal dashboards & tooling", managerUser.Id, pcInternal.Id, "wrench");
        db.Projects.AddRange(websiteProject, internalToolsProject);
        await db.SaveChangesAsync();

        db.ProjectMembers.AddRange(
            new ProjectMember { ProjectId = websiteProject.Id, AgentId = giorgos.Id, Role = ProjectRole.Owner },
            new ProjectMember { ProjectId = websiteProject.Id, AgentId = maria.Id, Role = ProjectRole.Member },
            new ProjectMember { ProjectId = websiteProject.Id, UserId = eleni.Id, Role = ProjectRole.Viewer },
            new ProjectMember { ProjectId = internalToolsProject.Id, AgentId = kostas.Id, Role = ProjectRole.Owner },
            new ProjectMember { ProjectId = internalToolsProject.Id, UserId = andreas.Id, Role = ProjectRole.Viewer });

        db.ProjectTeams.AddRange(
            new ProjectTeam { ProjectId = websiteProject.Id, TeamId = supportTeam.Id },
            new ProjectTeam { ProjectId = internalToolsProject.Id, TeamId = devTeam.Id });
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;

        var ticket1 = new Ticket("Login page throws 500 error",
            "Customers report a server error when submitting the login form on mobile Safari.",
            eleni.Id, catBug.Id, now.AddDays(3)) { ProjectId = websiteProject.Id, Priority = TicketPriority.High };

        var ticket2 = new Ticket("Add dark mode toggle",
            "Please add a dark mode switch to the settings page.",
            dimitris.Id, catFeature.Id, now.AddDays(10)) { ProjectId = websiteProject.Id, Priority = TicketPriority.Medium };

        var ticket3 = new Ticket("How do I export my invoices?",
            "Can't find the export button anywhere.",
            sofia.Id, catSupport.Id, null) { Priority = TicketPriority.Low };

        var ticket4 = new Ticket("Dashboard chart not loading",
            "The analytics chart on the internal dashboard spins forever.",
            andreas.Id, catBug.Id, now.AddDays(2)) { ProjectId = internalToolsProject.Id, Priority = TicketPriority.Urgent };

        foreach (var ticket in new[] { ticket1, ticket2, ticket3, ticket4 })
        {
            ticket.TicketNumber = await db.Tickets.IgnoreQueryFilters().CountAsync() + 1;
            db.Tickets.Add(ticket);
            await db.SaveChangesAsync();
        }

        ticket1.Assign(giorgos);
        ticket2.Assign(maria);
        ticket4.Assign(kostas);
        await db.SaveChangesAsync();

        db.Comments.AddRange(
            new Comment { TicketId = ticket1.Id, AuthorId = eleni.Id, Body = "This happens every time I try to log in from my phone." },
            new Comment { TicketId = ticket2.Id, AuthorId = dimitris.Id, Body = "Would be great to have this by next release." },
            new Comment { TicketId = ticket4.Id, AuthorId = andreas.Id, Body = "Still broken as of this morning." });

        db.TicketActivities.AddRange(
            new TicketActivity(ticket1.Id, TicketActivityType.Created, "Ticket created.", eleni.Id, eleni.FullName),
            new TicketActivity(ticket1.Id, TicketActivityType.Assigned, $"Assigned to {giorgos.FullName}.", managerUser.Id, managerUser.FullName),
            new TicketActivity(ticket2.Id, TicketActivityType.Created, "Ticket created.", dimitris.Id, dimitris.FullName),
            new TicketActivity(ticket2.Id, TicketActivityType.Assigned, $"Assigned to {maria.FullName}.", managerUser.Id, managerUser.FullName),
            new TicketActivity(ticket3.Id, TicketActivityType.Created, "Ticket created.", sofia.Id, sofia.FullName),
            new TicketActivity(ticket4.Id, TicketActivityType.Created, "Ticket created.", andreas.Id, andreas.FullName),
            new TicketActivity(ticket4.Id, TicketActivityType.Assigned, $"Assigned to {kostas.FullName}.", managerUser.Id, managerUser.FullName));

        db.TimeEntries.AddRange(
            new TimeEntry { TicketId = ticket1.Id, AgentId = giorgos.Id, StartedAt = now.AddHours(-3), EndedAt = now.AddHours(-2), Note = "Investigated stack trace." },
            new TimeEntry { TicketId = ticket4.Id, AgentId = kostas.Id, StartedAt = now.AddHours(-5), EndedAt = now.AddHours(-4).AddMinutes(30), Note = "Reproduced the bug locally." });

        await db.SaveChangesAsync();

        var meeting1 = new Meeting("Sprint planning", "Plan next sprint's scope.",
            managerUser.Id, now.AddDays(1).Date.AddHours(10), 45, websiteProject.Id, null, "https://meet.example.com/sprint");
        var meeting2 = new Meeting("Weekly support sync", "Review open tickets & blockers.",
            managerUser.Id, now.AddDays(2).Date.AddHours(9), 30, null, supportTeam.Id, "https://meet.example.com/support-sync");
        db.Meetings.AddRange(meeting1, meeting2);
        await db.SaveChangesAsync();

        db.MeetingAttendees.AddRange(
            new MeetingAttendee { MeetingId = meeting1.Id, AgentId = giorgos.Id },
            new MeetingAttendee { MeetingId = meeting1.Id, AgentId = maria.Id },
            new MeetingAttendee { MeetingId = meeting1.Id, UserId = eleni.Id },
            new MeetingAttendee { MeetingId = meeting2.Id, AgentId = giorgos.Id },
            new MeetingAttendee { MeetingId = meeting2.Id, AgentId = maria.Id });
        await db.SaveChangesAsync();

        // Give Dimitris (a plain Customer) extra rights to schedule meetings.
        var meetingsManage = await db.Permissions.FirstOrDefaultAsync(p => p.Key == "Meetings.Manage");
        if (meetingsManage is not null)
        {
            db.UserPermissions.Add(new UserPermission { ApplicationUserId = dimitrisIdentity.Id, PermissionId = meetingsManage.Id });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<Category> GetOrCreateCategoryAsync(AppDbContext db, string name)
    {
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (existing is not null) return existing;
        var created = new Category { Name = name };
        db.Categories.Add(created);
        await db.SaveChangesAsync();
        return created;
    }

    private static async Task<ProjectCategory> GetOrCreateProjectCategoryAsync(AppDbContext db, string name)
    {
        var existing = await db.ProjectCategories.FirstOrDefaultAsync(c => c.Name == name);
        if (existing is not null) return existing;
        var created = new ProjectCategory { Name = name };
        db.ProjectCategories.Add(created);
        await db.SaveChangesAsync();
        return created;
    }

    private static async Task<(ApplicationUser identity, User user)> CreateCustomerAsync(AppDbContext db, UserManager<ApplicationUser> userManager, string firstName)
    {
        var email = $"{firstName.ToLowerInvariant()}@gmail.com";
        var identity = new ApplicationUser { FullName = firstName, Email = email, UserName = email, Role = "Customer", EmailConfirmed = true };
        var result = await userManager.CreateAsync(identity, Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(identity, "Customer");

        var user = new User { FullName = firstName, Email = email };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (identity, user);
    }

    private static async Task<(ApplicationUser identity, User user)> CreateManagerAsync(AppDbContext db, UserManager<ApplicationUser> userManager, string firstName)
    {
        var email = $"{firstName.ToLowerInvariant()}@gmail.com";
        var identity = new ApplicationUser { FullName = firstName, Email = email, UserName = email, Role = "Manager", EmailConfirmed = true };
        var result = await userManager.CreateAsync(identity, Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(identity, "Manager");

        var user = new User { FullName = firstName, Email = email };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (identity, user);
    }

    private static async Task<(ApplicationUser identity, Agent agent)> CreateAgentAsync(AppDbContext db, UserManager<ApplicationUser> userManager, string firstName)
    {
        var email = $"{firstName.ToLowerInvariant()}@gmail.com";
        var identity = new ApplicationUser { FullName = firstName, Email = email, UserName = email, Role = "Agent", EmailConfirmed = true };
        var result = await userManager.CreateAsync(identity, Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(identity, "Agent");

        var agent = new Agent { FullName = firstName, Email = email };
        db.Agents.Add(agent);
        await db.SaveChangesAsync();
        return (identity, agent);
    }
}
