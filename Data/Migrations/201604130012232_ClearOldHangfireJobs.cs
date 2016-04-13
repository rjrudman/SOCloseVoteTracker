namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ClearOldHangfireJobs : DbMigration
    {
        public override void Up()
        {
            Sql(@"
DROP TABLE [HangFire].[AggregatedCounter]
DROP TABLE [HangFire].[Counter]
DROP TABLE [HangFire].[Hash]
DROP TABLE [HangFire].[JobParameter]
DROP TABLE [HangFire].[JobQueue]
DROP TABLE [HangFire].[List]
DROP TABLE [HangFire].[Schema]
DROP TABLE [HangFire].[Server]
DROP TABLE [HangFire].[Set]
DROP TABLE [HangFire].[State]
DROP TABLE [HangFire].[Job]
TRUNCATE TABLE [dbo].[QueuedQuestionQueries]
");
        }
        
        public override void Down()
        {
        }
    }
}
