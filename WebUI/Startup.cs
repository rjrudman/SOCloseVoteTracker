using System.Data.Entity;
using Core.Workers;
using Data;
using Data.Migrations;
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

            Pollers.StartPolling();
        }
    }
}
