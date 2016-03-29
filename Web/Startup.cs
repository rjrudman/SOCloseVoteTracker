using Core.Workers;
using Data;
using Hangfire;
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

            GlobalConfiguration.Configuration.UseSqlServerStorage(DataContext.CONNECTION_STRING_NAME);

            RecurringJob.AddOrUpdate(() => Pollers.RecentlyClosed(), "*/5 * * * *"); //Every 5 minutes
            RecurringJob.AddOrUpdate(() => Pollers.QueryRecentCloseVotes(), "*/5 * * * *"); //Every 5 minutes
            RecurringJob.AddOrUpdate(() => Pollers.QueryMostCloseVotes(), "*/5 * * * *"); //Every 5 minutes

            app.UseHangfireDashboard();
            app.UseHangfireServer();
        }
    }
}
