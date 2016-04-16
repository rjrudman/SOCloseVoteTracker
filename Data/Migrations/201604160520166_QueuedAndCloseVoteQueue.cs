namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class QueuedAndCloseVoteQueue : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.QueuedQuestionCloseVoteQueries",
                c => new
                    {
                        QueuedQuestionCloseVoteQueryId = c.Int(nullable: false, identity: true),
                        QuestionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.QueuedQuestionCloseVoteQueryId);
            
            CreateTable(
                "dbo.QueuedQuestionQueries",
                c => new
                    {
                        QueuedQuestionQueriesId = c.Int(nullable: false, identity: true),
                        QuestionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.QueuedQuestionQueriesId);

            Sql(@"
CREATE NONCLUSTERED INDEX [IX_QuestionId] ON [dbo].[QueuedQuestionCloseVoteQueries]
(
	[QuestionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");

            Sql(@"
CREATE NONCLUSTERED INDEX [IX_QuestionId] ON [dbo].[QueuedQuestionQueries]
(
	[QuestionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");
        }
        
        public override void Down()
        {
            DropTable("dbo.QueuedQuestionQueries");
            DropTable("dbo.QueuedQuestionCloseVoteQueries");
        }
    }
}
