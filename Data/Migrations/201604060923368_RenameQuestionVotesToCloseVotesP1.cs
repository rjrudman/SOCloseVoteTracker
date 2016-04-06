namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RenameQuestionVotesToCloseVotesP1 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CloseVotes",
                c => new
                    {
                        CloseVoteId = c.Int(nullable: false, identity: true),
                        QuestionId = c.Int(nullable: false),
                        VoteTypeId = c.Int(nullable: false),
                        FirstTimeSeen = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.CloseVoteId)
                .ForeignKey("dbo.Questions", t => t.QuestionId, cascadeDelete: true)
                .ForeignKey("dbo.VoteTypes", t => t.VoteTypeId, cascadeDelete: true)
                .Index(t => t.QuestionId)
                .Index(t => t.VoteTypeId);

            Sql(@"
INSERT INTO CloseVotes
SELECT QuestionId, VoteTypeId, FirstTimeSeen FROM QuestionVotes
");
            
        }
        
        public override void Down()
        {
            Sql(@"
INSERT INTO QuestionVotes
SELECT QuestionId, VoteTypeId, FirstTimeSeen FROM CloseVotes
");

            DropForeignKey("dbo.CloseVotes", "VoteTypeId", "dbo.VoteTypes");
            DropForeignKey("dbo.CloseVotes", "QuestionId", "dbo.Questions");
            DropIndex("dbo.CloseVotes", new[] { "VoteTypeId" });
            DropIndex("dbo.CloseVotes", new[] { "QuestionId" });
            DropTable("dbo.CloseVotes");
        }
    }
}
