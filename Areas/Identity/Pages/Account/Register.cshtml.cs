using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using mes.Models.Services.Infrastructures;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace intranet.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage="Il nome completo è richiesto")]
            [StringLength(100, MinimumLength=3, ErrorMessage="Il nome completo deve essere almeno {2} e di al massimo {1}  caratteri")]
            [Display(Name = "Nome completo")]
            public string FullName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "Le password non coincidono.")]
            public string ConfirmPassword { get; set; }
            public string Name { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ViewData["roles"] = _roleManager.Roles.ToList();
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            //qui si poteva scegliere il ruolo dal menù a tendina
            //var role = _roleManager.FindByIdAsync(Input.Name).Result;

            //invece impongo il ruolo "user" fisso per tutti, verrà cambiato dagli admin se necessario
            var role = _roleManager.FindByIdAsync("User").Result;

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                //imposto di default l'autenticazione a due fattori
                //user.TwoFactorEnabled = true;
                var result = await _userManager.CreateAsync(user, Input.Password);
            
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    
                    //gli utentu vengono creati tutti di default con il ruolo "User"
                    await _userManager.AddToRoleAsync(user, "User");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);
                    
                    await _emailSender.SendEmailAsync(Input.Email, "Gruppo GB Intranet",
                        $"Richiesta conferma email\nPrego confermare il vostro account <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>cliccando qui</a>.");

                        EmailSender sender = new EmailSender();
                        try
                        {
                        sender.SendEmail("Conferma email",
                                        $"Richiesta conferma email\nPrego confermare il vostro account <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>cliccando qui</a>."
                                        ,Input.Email,
                                        "automation@intranet.gb");
                        }
                        catch(Exception excp)
                        {
                            
                        }

                    //questo è il blocco if da modificare per spedire la mail di conferma
                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            ViewData["roles"] = _roleManager.Roles.ToList();
            // If we got this far, something failed, redisplay form
            return Page();
        }

    }
}
