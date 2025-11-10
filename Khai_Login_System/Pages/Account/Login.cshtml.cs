using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Khai_Login_System.Data;
using Khai_Login_System.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Khai_Login_System.Pages.Account;

public class LoginModel(ApplicationDbContext dbContext, ILogger<LoginModel> logger) : PageModel
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<LoginModel> _logger = logger;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
        ReturnUrl ??= Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == Input.UsernameOrEmail || u.Email == Input.UsernameOrEmail);

        if (user is null || !PasswordService.VerifyPassword(Input.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = Input.RememberMe,
            AllowRefresh = Input.RememberMe
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {Username} signed in successfully.", user.Username);

        if (!Url.IsLocalUrl(ReturnUrl))
        {
            ReturnUrl = Url.Content("~/");
        }

        return LocalRedirect(ReturnUrl);
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email.")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
