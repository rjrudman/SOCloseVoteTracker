namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class TrackQuestionQueriesProperly : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Questions", "NextQueueTime");
            Sql(@"
CREATE TABLE [dbo].[QueuedQuestionQueries](
	[QuestionId] [int] NOT NULL,
	[ProcessTime] [datetime] NOT NULL
) ON [PRIMARY]

CREATE CLUSTERED INDEX [IX_QuestionId] ON [dbo].[QueuedQuestionQueries]
(
	[QuestionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

CREATE NONCLUSTERED INDEX [IX_ProcessTime] ON [dbo].[QueuedQuestionQueries]
(
	[ProcessTime] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO
");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Questions", "NextQueueTime", c => c.DateTime());

            Sql(@"
DROP TABLE [dbo].[QueuedQuestionQueries]");
        }
    }
}
