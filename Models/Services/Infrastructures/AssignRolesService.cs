using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace mes.Models.Services.Infrastructures
{
    public class AssignRolesService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AssignRolesService(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this._userManager = userManager;
            this._roleManager = roleManager;
        }

        public List<string> GetUserRolesList()
        {
            List<string> usersRolesList = new List<string>();

            foreach(var user in _userManager.Users)
            {
                var role = _userManager.GetRolesAsync(user);
                var rolesName = role.GetAwaiter().GetResult();
                string allRoles="";
                foreach(string oneRole in rolesName)
                {
                    allRoles+=oneRole +", ";
                }
                usersRolesList.Add($"{user.ToString()} --> {allRoles}");
            }  

            return usersRolesList;          
        }
    }
}