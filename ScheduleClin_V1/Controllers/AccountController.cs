using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScheduleClin.Models;
using ScheduleClin.ViewModels;
using ScheduleClin.Services.Audit;

namespace ScheduleClin.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly IAuditLogger _auditLogger;

    public AccountController(SignInManager<User> signInManager, UserManager<User> userManager, IAuditLogger auditLogger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLogger = auditLogger;
    }

    // RF01 — tela de login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]               // RNF06 — proteção CSRF
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null || !user.Active)
        {
            await _auditLogger.LogAsync(new AuditEntry
            {
                Action = "LoginFailed",
                UserName = model.Email,
                Path = HttpContext.Request.Path,
                Method = HttpContext.Request.Method,
                Details = "Usuário inexistente ou inativo"
            });
            ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            await _auditLogger.LogAsync(new AuditEntry
            {
                Action = "LoginLockedOut",
                UserId = user.Id.ToString(),
                UserName = user.UserName,
                Path = HttpContext.Request.Path,
                Method = HttpContext.Request.Method,
                Details = "Conta bloqueada por excesso de tentativas"
            });
            ModelState.AddModelError(string.Empty, "Conta bloqueada temporariamente por excesso de tentativas.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            await _auditLogger.LogAsync(new AuditEntry
            {
                Action = "LoginFailed",
                UserId = user.Id.ToString(),
                UserName = user.UserName,
                Path = HttpContext.Request.Path,
                Method = HttpContext.Request.Method,
                Details = "Senha inválida"
            });
            ModelState.AddModelError(string.Empty, "Credenciais inválidas.");
            return View(model);
        }

        await _auditLogger.LogAsync(new AuditEntry
        {
            Action = "LoginSuccess",
            UserId = user.Id.ToString(),
            UserName = user.UserName,
            Path = HttpContext.Request.Path,
            Method = HttpContext.Request.Method,
            Details = "Login efetuado com sucesso"
        });

        // RF02 — primeiro acesso obriga troca de senha
        if (user.MustChangePassword)
            return RedirectToAction(nameof(ChangePassword));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // RF03 — logout
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        await _signInManager.SignOutAsync();
        await _auditLogger.LogAsync(new AuditEntry
        {
            Action = "Logout",
            UserId = user?.Id.ToString(),
            UserName = user?.UserName,
            Path = HttpContext.Request.Path,
            Method = HttpContext.Request.Method,
            Details = "Logout efetuado"
        });

        return RedirectToAction(nameof(Login));
    }

    // RF02 — troca de senha
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            await _auditLogger.LogAsync(new AuditEntry
            {
                Action = "ChangePasswordFailed",
                UserId = user.Id.ToString(),
                UserName = user.UserName,
                Path = HttpContext.Request.Path,
                Method = HttpContext.Request.Method,
                Details = string.Join(";", result.Errors.Select(e => e.Description))
            });
            foreach (var e in result.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        await _auditLogger.LogAsync(new AuditEntry
        {
            Action = "ChangePasswordSuccess",
            UserId = user.Id.ToString(),
            UserName = user.UserName,
            Path = HttpContext.Request.Path,
            Method = HttpContext.Request.Method,
            Details = "Senha alterada com sucesso"
        });

        TempData["Msg"] = "Senha alterada com sucesso.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
