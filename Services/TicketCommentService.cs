using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services;

/// <summary>
/// Service for managing ticket comments
/// </summary>
public class TicketCommentService
{
    private readonly ApplicationDbContext _context;

    public TicketCommentService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all comments for a ticket (visible to agents - includes internal notes)
    /// </summary>
    public async Task<List<TicketComment>> GetAgentCommentsAsync(int ticketId)
    {
        return await _context.TicketComments
            .Where(c => c.TicketId == ticketId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get only customer-visible comments for a ticket
    /// </summary>
    public async Task<List<TicketComment>> GetCustomerCommentsAsync(int ticketId)
    {
        return await _context.TicketComments
            .Where(c => c.TicketId == ticketId && c.IsCustomerVisible && !c.IsInternal)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Add a comment to a ticket
    /// </summary>
    public async Task<TicketComment> AddCommentAsync(
        int ticketId,
        string userId,
        string userName,
        string userRole,
        string commentText,
        bool isInternal = false)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null)
            throw new ArgumentException("Ticket not found", nameof(ticketId));

        var comment = new TicketComment
        {
            TicketId = ticketId,
            UserId = userId,
            UserName = userName,
            UserRole = userRole,
            Comment = commentText,
            IsInternal = isInternal,
            IsCustomerVisible = !isInternal
        };

        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync();

        return comment;
    }

    /// <summary>
    /// Delete a comment
    /// </summary>
    public async Task DeleteCommentAsync(int commentId)
    {
        var comment = await _context.TicketComments.FindAsync(commentId);
        if (comment != null)
        {
            _context.TicketComments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Count of unread comments for a ticket
    /// </summary>
    public async Task<int> GetCommentCountAsync(int ticketId)
    {
        return await _context.TicketComments
            .Where(c => c.TicketId == ticketId)
            .CountAsync();
    }

    /// <summary>
    /// Check if user can delete a comment
    /// </summary>
    public bool CanDeleteComment(TicketComment comment, string userId, string userRole)
    {
        // Comment author can delete their own comments
        if (comment.UserId == userId) return true;

        // Admins/Managers can delete any comment
        if (userRole == "Admin" || userRole == "Manager") return true;

        return false;
    }
}