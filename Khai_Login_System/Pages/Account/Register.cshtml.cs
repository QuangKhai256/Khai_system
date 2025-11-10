using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Khai_Login_System.Data;
using Khai_Login_System.Models;
using Khai_Login_System.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Khai_Login_System.Pages.Account;

public class RegisterModel(ApplicationDbContext dbContext, ILogger<RegisterModel> logger) : PageModel
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<RegisterModel> _logger = logger;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var normalizedUsername = Input.Username.Trim();
        var normalizedEmail = Input.Email.Trim().ToLowerInvariant();

        if (await _dbContext.Users.AnyAsync(u => u.Username == normalizedUsername))
        {
            ModelState.AddModelError(nameof(Input.Username), "Tên đăng nhập đã tồn tại.");
            return Page();
        }

        if (await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            ModelState.AddModelError(nameof(Input.Email), "Email đã được sử dụng.");
            return Page();
        }

        var user = new UserAccount
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            PasswordHash = PasswordService.HashPassword(Input.Password)
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        await SignInUserAsync(user);

        _logger.LogInformation("New user {Username} registered successfully.", user.Username);

        return RedirectToPage("/Index");
    }

    private async Task SignInUserAsync(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [Display(Name = "Tên đăng nhập")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
