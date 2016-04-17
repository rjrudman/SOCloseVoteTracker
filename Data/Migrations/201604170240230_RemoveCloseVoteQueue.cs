namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RemoveCloseVoteQueue : DbMigration
    {
        public override void Up()
        {
            DropTable("dbo.QueuedQuestionCloseVoteQueries");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.QueuedQuestionCloseVoteQueries",
                c => new
                    {
                        QueuedQuestionCloseVoteQueryId = c.Int(nullable: false, identity: true),
                        QuestionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.QueuedQuestionCloseVoteQueryId);
            
        }
    }
}
