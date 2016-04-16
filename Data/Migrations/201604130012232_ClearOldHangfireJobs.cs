namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ClearOldHangfireJobs : DbMigration
    {
        public override void Up()
        {
            Sql(@"
IF OBJECT_ID('*AggregatedCounter*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[AggregatedCounter]

IF OBJECT_ID('*Counter*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[Counter]

IF OBJECT_ID('*Hash*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[Hash]

IF OBJECT_ID('*JobParameter*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[JobParameter]

IF OBJECT_ID('*JobQueue*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[JobQueue]

IF OBJECT_ID('*List*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[List]

IF OBJECT_ID('*Schema*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[Schema]

IF OBJECT_ID('*Server*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[Server]

IF OBJECT_ID('*Set*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[Set]

IF OBJECT_ID('*State*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[State]

IF OBJECT_ID('*Job*', 'U') IS NOT NULL 
    DROP TABLE [HangFire].[Job]

DROP TABLE [dbo].[QueuedQuestionQueries]
");
        }
        
        public override void Down()
        {
        }
    }
}
