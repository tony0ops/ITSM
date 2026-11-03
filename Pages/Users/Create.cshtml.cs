using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.Users;

[Authorize(Roles = "Manager")]
public class CreateModel(
    UserManager<IdentityUser> userManager,
    ApplicationDbContext context) : PageModel
{
    [BindProperty] public StaffInput Input { get; set; } = new();
    public SelectList TeamList { get; set; } = default!;

    public async Task OnGetAsync() => await LoadTeamsAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Email = Input.Email.Trim();
        Input.DisplayName = Input.DisplayName.Trim();
        if (Input.Role is not ("Manager" or "Agent"))
            ModelState.AddModelError("Input.Role", "Choose Manager or Agent.");
        if (Input.TeamId.HasValue && !await context.Teams.AnyAsync(t => t.Id == Input.TeamId && t.IsActive))
            ModelState.AddModelError("Input.TeamId", "Choose an active team.");
        if (await userManager.FindByEmailAsync(Input.Email) != null)
            ModelState.AddModelError("Input.Email", "An account with this email already exists.");

        if (!ModelState.IsValid)
        {
            await LoadTeamsAsync();
            return Page();
        }

        var user = new IdentityUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(user, Input.Password);
        if (!createResult.Succeeded)
        {
            AddErrors(createResult);
            await LoadTeamsAsync();
            return Page();
        }

        var roleResult = await userManager.AddToRoleAsync(user, Input.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddErrors(roleResult);
            await LoadTeamsAsync();
            return Page();
        }

        var manager = await userManager.GetUserAsync(User);
        context.UserProfiles.Add(new UserProfile
        {
            UserId = user.Id,
            DisplayName = Input.DisplayName,
            Department = Input.Department,
            JobTitle = Input.JobTitle,
            TeamId = Input.TeamId,
            CreatedBy = manager?.Id ?? ""
        });
        try
        {
            await context.SaveChangesAsync();
        }
        catch
        {
            await userManager.DeleteAsync(user);
            throw;
        }

        TempData["SuccessMessage"] = $"{Input.Role} account for {Input.Email} created.";
        return RedirectToPage("./Index");
    }

    private async Task LoadTeamsAsync()
    {
        var teams = await context.Teams.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync();
        TeamList = new SelectList(teams, "Id", "Name");
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }

    public sealed class StaffInput
    {
        [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
        [Required, StringLength(100, MinimumLength = 1)] public string DisplayName { get; set; } = "";
        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)] public string Password { get; set; } = "";
        [Required] public string Role { get; set; } = "Agent";
        [StringLength(100)] public string? Department { get; set; }
        [StringLength(100)] public string? JobTitle { get; set; }
        public int? TeamId { get; set; }
    }
}
