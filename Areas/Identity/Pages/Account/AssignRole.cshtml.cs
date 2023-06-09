using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using mes.Models.Services.Infrastructures;

namespace intranet.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class AssignRoleModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AssignRoleModel(
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
           [Required]
           //[EmailAddress]
           //[Display(Name = "Email")]
            public string RoleName { get; set; }

            [Required]
            //[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            //[DataType(DataType.Password)]
            //[Display(Name = "Password")]
            public string UserName { get; set; }
            public string Email { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ViewData["roles"] = _roleManager.Roles.ToList();
            ViewData["users"] = _userManager.Users.ToList();
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            AssignRolesService assignRoleServ = new AssignRolesService(_userManager, _roleManager);

            //=================================
            //List<string> usersRolesList = new List<string>();
//
            //foreach(var user in _userManager.Users)
            //{
            //    var role = _userManager.GetRolesAsync(user);
            //    var rolesName = role.GetAwaiter().GetResult();
            //    string allRoles="";
            //    foreach(string oneRole in rolesName)
            //    {
            //        allRoles+=oneRole +", ";
            //    }
            //    usersRolesList.Add($"{user.ToString()} --> {allRoles}");
            //}
            
            ViewData["usersRoles"] = assignRoleServ.GetUserRolesList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {            
            returnUrl ??= Url.Content("~/");
            var role = _roleManager.FindByIdAsync(Input.RoleName).Result;
            var user = await _userManager.FindByIdAsync(Input.UserName);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            IdentityResult roleResult;

            if (ModelState.IsValid)
            {
                
                bool roleExists = await _roleManager.RoleExistsAsync(role.Name);                

                if(roleExists)
                {                                    
                    roleResult = await _userManager.AddToRoleAsync(user, role.Name);

                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation($"User {user} added to role {role}");
                    }
                }
                                
            }
            ViewData["roles"] = _roleManager.Roles.ToList();
            ViewData["users"] = _userManager.Users.ToList();

            AssignRolesService assignRoles = new AssignRolesService(_userManager, _roleManager);
            ViewData["usersRoles"] = assignRoles.GetUserRolesList();
            

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
