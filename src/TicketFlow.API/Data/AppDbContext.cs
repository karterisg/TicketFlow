using TicketFlow.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace TicketFlow.API.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public new DbSet<User> Users { get; set; } = null!; //hiding-identitydbcontext has already this users
        public DbSet<Ticket> Tickets { get; set; } = null!;
        public DbSet<Agent> Agents { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectCategory> ProjectCategories => Set<ProjectCategory>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<ProjectTemplate> ProjectTemplates => Set<ProjectTemplate>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<ProjectTeam> ProjectTeams => Set<ProjectTeam>();
        public DbSet<Meeting> Meetings => Set<Meeting>();
        public DbSet<MeetingAttendee> MeetingAttendees => Set<MeetingAttendee>();
        public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<Rating> Ratings => Set<Rating>();
        public DbSet<Notification> Notifications => Set<Notification>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasSequence<int>("TicketNumberSeq").StartsAt(1).IncrementsBy(1);// for race condition prevention


            // Configure the Ticket entity
            modelBuilder.Entity<Ticket>(entity =>
            {
                // Primary Key
                entity.HasKey(t => t.Id);


                entity.Property(t => t.Title)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(t => t.Description)
                      .IsRequired()
                      .HasMaxLength(2000);

                entity.Property(t => t.Status)
                      .IsRequired()
                      .HasConversion<string>();

                entity.Property(t => t.Priority)
                      .HasConversion<string>();

                //entity.Property(t => t.Comment)
                //    .IsRequired()
                //    .HasMaxLength(2000);

                // Soft Delete
                entity.HasQueryFilter(t => !t.IsDeleted); // This ensures that any query on the Tickets DbSet will automatically filter out tickets that are marked as deleted.


                // Relationships
                entity.HasOne(t => t.User)
                      .WithMany(u => u.Tickets)
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade); //selects how to handle the deletion of related entities. In this case, when a User is deleted, all their associated Tickets will also be deleted.

                entity.HasOne(t => t.Agent)
                        .WithMany(a => a.AssignedTickets)
                        .HasForeignKey(t => t.AgentId)
                        .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.Category)
                      .WithMany(c => c.Tickets)
                      .HasForeignKey(t => t.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.Project)
                      .WithMany(p => p.Tickets)
                      .HasForeignKey(t => t.ProjectId)
                      .OnDelete(DeleteBehavior.SetNull);

            });


            modelBuilder.Entity<TimeEntry>(entity =>
            {
                entity.HasKey(te => te.Id);
                entity.Property(te => te.StartedAt)
                      .IsRequired()
                      .HasMaxLength(500);
                entity.Property(te => te.EndedAt)
                      .IsRequired();
                entity.HasOne(te => te.Ticket)
                      .WithMany(t => t.TimeEntries)
                      .HasForeignKey(te => te.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(te => te.Agent)
                      .WithMany(a => a.TimeEntries)
                      .HasForeignKey(te => te.AgentId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure the Comment entity
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Body)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.HasOne(c => c.Ticket)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);  

                entity.HasOne(c => c.Author)
                    .WithMany()
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            // Configure the User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(u => u.Email)
                    .IsUnique();
            });


            // Configure the Category entity
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(c => c.Name)
                      .IsUnique();
            });

            modelBuilder.Entity<Agent>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.FullName)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(a => a.Email)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.HasIndex(a => a.Email)
                      .IsUnique();
            });

            // Configure the Project entity
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(p => p.Status)
                      .IsRequired()
                      .HasConversion<string>();

                entity.Property(p => p.IconKey)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.HasIndex(p => p.Name)
                      .IsUnique();

                entity.HasQueryFilter(p => !p.IsDeleted);

                entity.HasOne(p => p.ProjectCategory)
                      .WithMany(c => c.Projects)
                      .HasForeignKey(p => p.ProjectCategoryId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure the ProjectCategory entity
            modelBuilder.Entity<ProjectCategory>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(c => c.Name)
                      .IsUnique();
            });

            // Configure the ProjectMember entity
            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Role)
                      .IsRequired()
                      .HasConversion<string>();

                entity.HasIndex(m => new { m.ProjectId, m.UserId })
                      .IsUnique()
                      .HasFilter("\"UserId\" IS NOT NULL");

                entity.HasIndex(m => new { m.ProjectId, m.AgentId })
                      .IsUnique()
                      .HasFilter("\"AgentId\" IS NOT NULL");

                entity.HasOne(m => m.Project)
                      .WithMany(p => p.Members)
                      .HasForeignKey(m => m.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.User)
                      .WithMany()
                      .HasForeignKey(m => m.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(m => m.Agent)
                      .WithMany()
                      .HasForeignKey(m => m.AgentId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure the ProjectTemplate entity
            modelBuilder.Entity<ProjectTemplate>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(t => t.DefaultIconKey)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.HasOne(t => t.DefaultProjectCategory)
                      .WithMany()
                      .HasForeignKey(t => t.DefaultProjectCategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(t => t.Timeline)
                       .HasMaxLength(30);
            });

            // Configure the Team entity
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(t => t.Id);

                entity.Property(t => t.Name)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.HasIndex(t => t.Name)
                      .IsUnique();

                entity.HasQueryFilter(t => !t.IsDeleted);
            });

            // Configure the TeamMember entity
            modelBuilder.Entity<TeamMember>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Role)
                      .IsRequired()
                      .HasConversion<string>();

                entity.HasIndex(m => new { m.TeamId, m.UserId })
                      .IsUnique()
                      .HasFilter("\"UserId\" IS NOT NULL");

                entity.HasIndex(m => new { m.TeamId, m.AgentId })
                      .IsUnique()
                      .HasFilter("\"AgentId\" IS NOT NULL");

                entity.HasOne(m => m.Team)
                      .WithMany(t => t.Members)
                      .HasForeignKey(m => m.TeamId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.User)
                      .WithMany()
                      .HasForeignKey(m => m.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(m => m.Agent)
                      .WithMany()
                      .HasForeignKey(m => m.AgentId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure the ProjectTeam entity
            modelBuilder.Entity<ProjectTeam>(entity =>
            {
                entity.HasKey(pt => pt.Id);

                entity.HasIndex(pt => new { pt.ProjectId, pt.TeamId })
                      .IsUnique();

                entity.HasOne(pt => pt.Project)
                      .WithMany(p => p.ProjectTeams)
                      .HasForeignKey(pt => pt.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pt => pt.Team)
                      .WithMany(t => t.ProjectTeams)
                      .HasForeignKey(pt => pt.TeamId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure the Meeting entity
            modelBuilder.Entity<Meeting>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.Property(m => m.Title)
                      .IsRequired()
                      .HasMaxLength(150);

                entity.Property(m => m.Status)
                      .IsRequired()
                      .HasConversion<string>();

                entity.HasQueryFilter(m => !m.IsDeleted);

                entity.HasOne(m => m.Project)
                      .WithMany()
                      .HasForeignKey(m => m.ProjectId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(m => m.Team)
                      .WithMany()
                      .HasForeignKey(m => m.TeamId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure the MeetingAttendee entity
            modelBuilder.Entity<MeetingAttendee>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Response)
                      .IsRequired()
                      .HasConversion<string>();

                entity.HasOne(a => a.Meeting)
                      .WithMany(m => m.Attendees)
                      .HasForeignKey(a => a.MeetingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(a => a.Agent)
                      .WithMany()
                      .HasForeignKey(a => a.AgentId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<TicketActivity>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.ActionType)
                      .IsRequired()
                      .HasConversion<string>();

                entity.Property(a => a.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(a => a.ActorName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(a => a.Ticket)
                    .WithMany()
                    .HasForeignKey(a => a.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure the Rating entity
            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Score)
                      .IsRequired();

                entity.Property(r => r.Comment)
                      .HasMaxLength(1000);

                entity.Property(r => r.CreatedAt)
                      .IsRequired();

                entity.HasIndex(r => r.TicketId)
                      .IsUnique();

                entity.HasOne(r => r.Ticket)
                      .WithMany()
                      .HasForeignKey(r => r.TicketId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure the Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.Message)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(n => n.Type)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.HasIndex(n => new { n.UserId, n.ReadAt });

                entity.HasOne(n => n.User)
                      .WithMany()
                      .HasForeignKey(n => n.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure the Permission entity
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.Key)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(p => p.Category)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(p => p.Description)
                      .IsRequired()
                      .HasMaxLength(300);

                entity.HasIndex(p => p.Key)
                      .IsUnique();
            });

            // Configure the UserPermission entity
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(up => up.Id);

                entity.Property(up => up.ApplicationUserId)
                      .IsRequired();

                entity.HasIndex(up => new { up.ApplicationUserId, up.PermissionId })
                      .IsUnique();

                entity.HasOne(up => up.Permission)
                      .WithMany(p => p.UserPermissions)
                      .HasForeignKey(up => up.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(up => up.ApplicationUserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
        // Override SaveChangesAsync to automatically update the UpdatedAt property for modified entities from DataBase
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                else if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
