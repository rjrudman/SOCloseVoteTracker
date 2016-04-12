using System;
using System.Collections.Generic;
using System.Web;
using Core.Workers;
using Hangfire;
using Hangfire.Common;
using Hangfire.Dashboard;
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
