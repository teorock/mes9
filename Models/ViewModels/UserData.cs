using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.ViewModels
{
    public class UserData
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserRoles { get; set; }
        public string UserIpAddress { get; set; }
        public string UserIpPort { get; set; }        
    }
}