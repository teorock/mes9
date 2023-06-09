using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace intranet.Models
{
    public class AssignRoleViewModel
    {
        //public IEnumerable<Microsoft.AspNetCore.Identity.IdentityUser> UserNames { get; set; }
        //public IEnumerable<Microsoft.AspNetCore.Identity.IdentityRole> Roles { get; set; }
        public string UserName { get; set; }
        public bool IsRoot { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsUser { get; set; }
        public bool IsPowerUser { get; set; }
    }
}