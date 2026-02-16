namespace WebApplication1.Models;

public enum TicketStatus
{
    New,
    Assigned,
    InProgress,
    WaitingOnCustomer,   
    WaitingOnVendor,      
    Resolved,
    Closed,
    Cancelled            
}