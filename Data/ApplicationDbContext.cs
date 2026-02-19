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
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Customer-Ticket relationship
        // When a customer is deleted, related tickets will have their CustomerId set to null
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Customer)
            .WithMany(c => c.Tickets)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Ticket-TicketActivity relationship
        // When a ticket is deleted, all related activities are deleted as well
        modelBuilder.Entity<TicketActivity>()
            .HasOne(a => a.Ticket)
            .WithMany(t => t.Activities)
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes for frequently queried columns (performance optimization)
        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.CustomerId)
            .HasDatabaseName("IX_Tickets_CustomerId");

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.Status)
            .HasDatabaseName("IX_Tickets_Status");

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_Tickets_CreatedAt");

        modelBuilder.Entity<TicketActivity>()
            .HasIndex(a => a.TicketId)
            .HasDatabaseName("IX_TicketActivities_TicketId");

        modelBuilder.Entity<TicketActivity>()
            .HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_TicketActivities_CreatedAt");
    }
}