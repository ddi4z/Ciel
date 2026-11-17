using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuizCiel.Web.Domain.Exceptions;
using QuizCiel.Web.Models;
using QuizCiel.Web.Services.Accounts;

namespace QuizCiel.Web.Controllers;

/// <summary>
/// Player registration and sign in / sign out for players and the administrator (ASP.NET Core Identity).
/// </summary>
[Route("account")]
public class AccountController : AppController
{
    private readonly AccountService accountService;
    private readonly SignInManager<IdentityUser> signInManager;
    private readonly ILogger<AccountController> logger;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="accountService">AccountService: player registration.</param>
    /// <param name="signInManager">SignInManager&lt;IdentityUser&gt;: Identity sign in.</param>
    /// <param name="logger">ILogger&lt;AccountController&gt;: records sign-in attempts with the login name masked.</param>
    public AccountController(AccountService accountService, SignInManager<IdentityUser> signInManager, ILogger<AccountController> logger)
    {
        this.accountService = accountService;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    /// <summary>
    /// Shows the registration form.
    /// </summary>
    /// <returns>IActionResult: the registration view.</returns>
    [HttpGet("register")]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    /// <summary>
    /// Registers a player and signs them in.
    /// </summary>
    /// <param name="registerForm">RegisterViewModel: posted form.</param>
    /// <param name="cancellationToken">CancellationToken: aborted when the client disconnects.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect home, or the form with errors.</returns>
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel registerForm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(registerForm);

        try
        {
            var playerAccount = await accountService.RegisterPlayerAsync(
                registerForm.DocumentNumber, registerForm.FullName, registerForm.Password, cancellationToken);
            await signInManager.SignInAsync(playerAccount, isPersistent: false);
        }
        catch (DomainException ruleViolation)
        {
            AddRuleViolationToForm(ruleViolation);
            return View(registerForm);
        }

        ShowSuccess("¡Registro exitoso! Ya puedes participar.");
        return Redirect("/");
    }

    /// <summary>
    /// Shows the login form.
    /// </summary>
    /// <param name="returnUrl">string?: local page to open after signing in.</param>
    /// <returns>IActionResult: the login view.</returns>
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Signs in with cédula (players) or username (administrator). Repeated failures lock the account for a while.
    /// </summary>
    /// <param name="loginForm">LoginViewModel: posted credentials.</param>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the requested page, or the form with an error.</returns>
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel loginForm)
    {
        if (!ModelState.IsValid)
            return View(loginForm);

        var userName = AccountService.NormalizeLoginName(loginForm.Username);
        var signInResult = await signInManager.PasswordSignInAsync(userName, loginForm.Password, isPersistent: false, lockoutOnFailure: true);
        LogSignInResult(userName, signInResult);
        if (!signInResult.Succeeded)
        {
            var failureMessage = signInResult.IsLockedOut
                ? "Demasiados intentos fallidos. Intenta de nuevo en unos minutos."
                : "Cédula, usuario o contraseña incorrectos.";
            ModelState.AddModelError(string.Empty, failureMessage);
            return View(loginForm);
        }

        if (Url.IsLocalUrl(loginForm.ReturnUrl))
            return LocalRedirect(loginForm.ReturnUrl);
        return LocalRedirect(await GetLandingPageAsync(userName));
    }

    /// <summary>
    /// Records the outcome of a sign-in attempt. The login name is masked because for players it is their cédula.
    /// </summary>
    /// <param name="userName">string: normalized login name that was tried.</param>
    /// <param name="signInResult">SignInResult: outcome returned by Identity.</param>
    private void LogSignInResult(string userName, Microsoft.AspNetCore.Identity.SignInResult signInResult)
    {
        var maskedLoginName = DisplayFormat.MaskedDocument(userName);
        if (signInResult.Succeeded)
            logger.LogInformation("Sign-in succeeded for {LoginName}.", maskedLoginName);
        else if (signInResult.IsLockedOut)
            logger.LogWarning("Sign-in rejected for {LoginName}: account locked out after repeated failures.", maskedLoginName);
        else
            logger.LogWarning("Sign-in failed for {LoginName}: wrong credentials.", maskedLoginName);
    }

    /// <summary>
    /// Chooses where a user lands after signing in without a requested page: the administration panel for
    /// administrators, the home page (their participation) for players.
    /// </summary>
    /// <param name="userName">string: username that just signed in.</param>
    /// <returns>Task&lt;string&gt;: local path to redirect to.</returns>
    private async Task<string> GetLandingPageAsync(string userName)
    {
        var signedInAccount = await signInManager.UserManager.FindByNameAsync(userName);
        var isAdministrator = signedInAccount is not null
            && await signInManager.UserManager.IsInRoleAsync(signedInAccount, AppRoles.Admin);
        return isAdministrator ? "/admin" : "/";
    }

    /// <summary>
    /// Signs out.
    /// </summary>
    /// <returns>Task&lt;IActionResult&gt;: redirect to the home page.</returns>
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return Redirect("/");
    }
}
