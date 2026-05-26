using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<CustomerSla> CustomerSlas => Set<CustomerSla>();
    public DbSet<CustomerUser> CustomerUsers => Set<CustomerUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BaseEntity column mappings
        modelBuilder.Entity<Ticket>().Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        modelBuilder.Entity<TicketComment>().Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        modelBuilder.Entity<Team>().Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        modelBuilder.Entity<UserProfile>().Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        modelBuilder.Entity<CustomerSla>().Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        modelBuilder.Entity<CustomerUser>().Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        modelBuilder.Entity<Customer>().Property(e => e.CreatedAt).HasColumnName("CreatedAt");
        modelBuilder.Entity<TicketActivity>().Property(e => e.CreatedAt).HasColumnName("CreatedAt");

        // Ticket-specific column aliases
        modelBuilder.Entity<Ticket>().Property(t => t.ClosedAt).HasColumnName("ClosedAt");
        modelBuilder.Entity<Ticket>().Property(t => t.SlaDueAt).HasColumnName("SlaDueAt");

        // Relationships
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Customer).WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CustomerId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TicketActivity>()
            .HasOne(a => a.Ticket).WithMany(t => t.Activities)
            .HasForeignKey(a => a.TicketId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketComment>()
            .HasOne(c => c.Ticket).WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProfile>()
            .HasOne(up => up.User).WithMany()
            .HasForeignKey(up => up.UserId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProfile>()
            .HasOne(up => up.Team).WithMany(t => t.Members)
            .HasForeignKey(up => up.TeamId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CustomerSla>()
            .HasOne(cs => cs.Customer).WithMany()
            .HasForeignKey(cs => cs.CustomerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerUser>()
            .HasOne(cu => cu.Customer).WithMany()
            .HasForeignKey(cu => cu.CustomerId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerUser>()
            .HasOne(cu => cu.User).WithMany()
            .HasForeignKey(cu => cu.UserId).OnDelete(DeleteBehavior.Cascade);

        // Global query filters
        modelBuilder.Entity<Ticket>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<TicketActivity>().HasQueryFilter(a => !a.Ticket.IsDeleted);
        modelBuilder.Entity<TicketComment>().HasQueryFilter(c => !c.Ticket.IsDeleted);
        modelBuilder.Entity<CustomerSla>().HasQueryFilter(cs => !cs.Customer.IsDeleted);
        modelBuilder.Entity<CustomerUser>().HasQueryFilter(cu => !cu.Customer.IsDeleted);

        // Indexes
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.Number).IsUnique().HasDatabaseName("IX_Tickets_Number_Unique");
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.CustomerId).HasDatabaseName("IX_Tickets_CustomerId");
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.Status).HasDatabaseName("IX_Tickets_Status");
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.CreatedAt).HasDatabaseName("IX_Tickets_CreatedAt");
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.AssignedGroup).HasDatabaseName("IX_Tickets_AssignedGroup");
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.Severity).HasDatabaseName("IX_Tickets_Severity");
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.IsDeleted).HasDatabaseName("IX_Tickets_IsDeleted");

        modelBuilder.Entity<TicketActivity>()
            .HasIndex(a => a.TicketId).HasDatabaseName("IX_TicketActivities_TicketId");
        modelBuilder.Entity<TicketActivity>()
            .HasIndex(a => a.CreatedAt).HasDatabaseName("IX_TicketActivities_CreatedAt");

        modelBuilder.Entity<TicketComment>()
            .HasIndex(c => c.TicketId).HasDatabaseName("IX_TicketComments_TicketId");
        modelBuilder.Entity<TicketComment>()
            .HasIndex(c => c.IsInternal).HasDatabaseName("IX_TicketComments_IsInternal");
        modelBuilder.Entity<TicketComment>()
            .HasIndex(c => c.CreatedAt).HasDatabaseName("IX_TicketComments_CreatedAt");

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.IsDeleted).HasDatabaseName("IX_Customers_IsDeleted");

        modelBuilder.Entity<UserProfile>()
            .HasIndex(up => up.UserId).IsUnique().HasDatabaseName("IX_UserProfiles_UserId_Unique");
        modelBuilder.Entity<UserProfile>()
            .HasIndex(up => up.TeamId).HasDatabaseName("IX_UserProfiles_TeamId");

        modelBuilder.Entity<Team>()
            .HasIndex(t => t.Group).HasDatabaseName("IX_Teams_Group");
        modelBuilder.Entity<Team>()
            .HasIndex(t => t.Name).IsUnique().HasDatabaseName("IX_Teams_Name_Unique");

        modelBuilder.Entity<CustomerSla>()
            .HasIndex(cs => cs.CustomerId).IsUnique().HasDatabaseName("IX_CustomerSlas_CustomerId_Unique");
        modelBuilder.Entity<CustomerSla>()
            .HasIndex(cs => cs.IsActive).HasDatabaseName("IX_CustomerSlas_IsActive");

        modelBuilder.Entity<CustomerUser>()
            .HasIndex(cu => cu.CustomerId).HasDatabaseName("IX_CustomerUsers_CustomerId");
        modelBuilder.Entity<CustomerUser>()
            .HasIndex(cu => cu.UserId).IsUnique().HasDatabaseName("IX_CustomerUsers_UserId_Unique");
        modelBuilder.Entity<CustomerUser>()
            .HasIndex(cu => cu.IsActive).HasDatabaseName("IX_CustomerUsers_IsActive");
    }
}