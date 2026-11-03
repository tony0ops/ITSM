using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn,
    ApplicationDbContext db, IConfiguration configuration) : PageModel
{
    public bool LabMode => configuration.GetValue<bool>("LabMode:Enabled");
    [BindProperty] public RegistrationInput Input { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var email = Input.Email.Trim();
        if (await users.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError("Input.Email", "An account with this email already exists. Use Forgot Password to recover it.");
            return Page();
        }
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = LabMode };
        var result = await users.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return Page();
        }
        await transaction.CommitAsync();
        await signIn.SignInAsync(user, isPersistent: false);
        return LocalRedirect(LabMode ? "/AccessSetup" : "/Identity/Account/Manage");
    }
    public class RegistrationInput
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = "";
        [Required, DataType(DataType.Password), Compare("Password")]
        [Display(Name = "Confirm password")] public string ConfirmPassword { get; set; } = "";
    }
}
