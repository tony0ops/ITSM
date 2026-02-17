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
    public DbSet<Customer> Customers => Set<Customer>();  // NEW LINE

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Customer-Ticket relationship (OPTIONAL for now - we'll add FK later)
        // This allows us to add Customers first without breaking existing tickets
    }
}