using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.InfrastructureModels
{
    public class  UsersRolesNotify
    {
        public string Username { get; set; }
        public object UserRoles { get; set; }
        public string UserEmail { get; set; }
        public string UserNotifyAddress { get; set; }
    }
}