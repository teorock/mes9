using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
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
    public class EmergencyRegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmergencyRegisterModel(
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
            //[Required(ErrorMessage="Il nome completo è richiesto")]
            //[StringLength(100, MinimumLength=3, ErrorMessage="Il nome completo deve essere almeno {2} e di al massimo {1}  caratteri")]
            [Display(Name = "Nome completo")]
            public string FullName { get; set; }

            //[Required]
            //[EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            //[Required]
            //[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            //[Compare("Password", ErrorMessage = "Le password non coincidono.")]
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

            var role = _roleManager.FindByIdAsync("root").Result;

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = "master@this.app", Email = "master@this.app" };
                var result = await _userManager.CreateAsync(user, "H4lOpenSh4ft01!");
            
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created emergency account with password.");
                    
                    await _userManager.AddToRoleAsync(user, "root");

                    var baseCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(baseCode));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);
                    
                    var test = await _userManager.ConfirmEmailAsync(user,baseCode);          
       
               }
            }
            ViewData["roles"] = _roleManager.Roles.ToList();
            // If we got this far, something failed, redisplay form            
            returnUrl = "/Account/Login";
            return Page();            
        }
    }
}
