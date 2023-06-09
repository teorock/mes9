using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace intranet.Controllers
{
    public class RoleController : Controller  
    {  
        RoleManager<IdentityRole> roleManager; 
        UserManager<IdentityUser> userManager;
    
        public RoleController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)  
        {  
            this.roleManager = roleManager;  
            this.userManager = userManager;
        }  
    
        [Authorize(Roles = "root")]
        public IActionResult Index()  
        {  
            //var user = userManager.Users.ToList();
            var roles = roleManager.Roles.ToList();  
            return View(roles);  
        }  
    
        [Authorize(Roles = "root")]
        public IActionResult Create()  
        {  
            return View(new IdentityRole());  
        }  
    
        [HttpPost]  
        public async Task<IActionResult> Create(IdentityRole role)  
        {  
            await roleManager.CreateAsync(role);  
            return RedirectToAction("Index");  
        }

        public IActionResult RemoveRole()  
        {              
            var roles = roleManager.Roles.ToList();
            //return View("Delete", roles);  
            return View(roles);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRole(IdentityRole role)  
        {  
            await roleManager.DeleteAsync(role); 
            return RedirectToAction("Index");  
        }            

        [Authorize(Roles = "root")]
        public IActionResult List()
        {
            var roles = roleManager.Roles.ToList();
            return View(roles);
        }

        [Authorize(Roles = "root")]
        public IActionResult Assign()
        {
            //IEnumerable<IdentityUser> users = userManager.Users;
            //IEnumerable<IdentityRole> roles = roleManager.Roles;
            //
            //ViewData["users"] = users;
            //ViewData["roles"] = roles;

            return Redirect("/Identity/Account/AssignRole");
        }

        [Authorize(Roles = "root")]
        public IActionResult Remove()
        {
            return Redirect("/Identity/Account/RemoveFromRole");
        }        
    }  
}