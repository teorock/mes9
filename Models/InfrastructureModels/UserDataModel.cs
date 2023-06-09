using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace mes.Models.InfrastructureModels
{
    public class UserDataModel
    {
        public string Id { get; set; }
        public long AccessFailedCount { get; set; }
        public string ConcurrencyStamp { get; set; }
        public string Email { get; set; }
        public long EmailConfirmed { get; set; }
        public long LockoutEnabled { get; set; }
        //public string? LockoutEnd { get; set; }
        public string NormalizedEmail { get; set; }
        public string NormalizedUserName { get; set; }        
        //public string PasswordHash { get; set; }
        //public string PhoneNumber { get; set; }
        //public long PhoneNumberConfirmed { get; set; }
        //public string SecurityStamp { get; set; }
        //public long TwoFactorEnabled { get; set; }
        public string UserName { get; set; }
    }
}