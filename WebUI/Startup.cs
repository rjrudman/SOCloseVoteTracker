using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Web;
using Core.Workers;
using Data;
using Data.Migrations;
using Hangfire;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebUI.Startup))]
namespace WebUI
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ReadWriteDataContext, Configuration>());
            using (var c = new ReadWriteDataContext())
                c.Database.Initialize(true);

            GlobalConfiguration.Configuration.UseSqlServerStorage(ReadWriteDataContext.READ_WRITE_CONNECTION_STRING_NAME, new SqlServerStorageOptions
            {
                JobExpirationCheckInterval = TimeSpan.FromMinutes(5)
            });


            if (Utils.GlobalConfiguration.EnableHangfire)
            {
                Pollers.StartPolling();

                app.UseHangfireServer();

                var options = new DashboardOptions
                {
                    AppPath = VirtualPathUtility.ToAbsolute("~"),
                    AuthorizationFilters = new[] { new RemoveAuthorizationFilter() }
                };
                app.UseHangfireDashboard("/hangfire", options);
                GlobalJobFilters.Filters.Add(new ImmediatelyExpireSuccessfulJobs());
            }
        }

        public class RemoveAuthorizationFilter : IAuthorizationFilter
        {
            public bool Authorize(IDictionary<string, object> owinEnvironment)
            {
                return true;
            }
        }

        public class ImmediatelyExpireSuccessfulJobs : JobFilterAttribute, IApplyStateFilter
        {
            public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                context.JobExpirationTimeout = TimeSpan.Zero;
            }

            public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
            {
                context.JobExpirationTimeout = TimeSpan.Zero;
            }
        }
    }
}
