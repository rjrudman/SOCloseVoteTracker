using System.Collections.Generic;
using System.Data.Entity;
using System.Web;
using Core.Workers;
using Data;
using Data.Migrations;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Web.Startup))]
namespace Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            
            app.UseErrorPage();
            app.UseHangfireServer();

            var options = new DashboardOptions
            {
                AppPath = VirtualPathUtility.ToAbsolute("~"),
                AuthorizationFilters = new[] {new RemoveAuthorizationFilter()}
            };
            app.UseHangfireDashboard("/hangfire", options);

            Pollers.Start();
        }

        public class RemoveAuthorizationFilter : IAuthorizationFilter
        {
            public bool Authorize(IDictionary<string, object> owinEnvironment)
            { 
                return true;
            }
        }
    }
}
