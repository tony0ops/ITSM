using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? StatusMessage { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal user doesn't exist
                StatusMessage = "If the account exists, the password has been reset.";
                return Page();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);

            if (result.Succeeded)
            {
                StatusMessage = "Password has been reset successfully. You can now log in with your new password.";
                Input = new InputModel();
                return Page();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email Address")]
            public string Email { get; set; } = "";

            [Required]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = "";

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = "";
        }
    }
}