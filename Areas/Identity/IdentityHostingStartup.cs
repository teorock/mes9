using System;
using intranet.Areas.Identity.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(intranet.Areas.Identity.IdentityHostingStartup))]
namespace intranet.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
        //    builder.ConfigureServices((context, services) => {
        //        services.AddDbContext<intranetIdentityDbContext>(options =>
        //            options.UseSqlite(
        //                context.Configuration.GetConnectionString("intranetIdentityDbContextConnection")));
//
        //        services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
        //            .AddEntityFrameworkStores<intranetIdentityDbContext>();
        //    });
        }
    }
}