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

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataContext, Configuration>());
            using (var c = new DataContext())
                c.Database.Initialize(true);
            
            GlobalConfiguration.Configuration.UseSqlServerStorage(DataContext.CONNECTION_STRING_NAME);

            RecurringJob.AddOrUpdate(() => Pollers.RecentlyClosed(), "*/5 * * * *"); //Every 5 minutes
            RecurringJob.AddOrUpdate(() => Pollers.QueryRecentCloseVotes(), "*/5 * * * *"); //Every 5 minutes
            RecurringJob.AddOrUpdate(() => Pollers.QueryMostCloseVotes(), "*/5 * * * *"); //Every 5 minutes

            Chat.JoinAndWatchRoom(Utils.Configuration.ChatRoomURL);

            app.UseErrorPage();

            app.UseHangfireServer();
            var options = new DashboardOptions
            {
                AppPath = VirtualPathUtility.ToAbsolute("~"),
                AuthorizationFilters = new[] {new RemoveAuthorizationFilter()}
            };
            app.UseHangfireDashboard("/hangfire", options);

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
