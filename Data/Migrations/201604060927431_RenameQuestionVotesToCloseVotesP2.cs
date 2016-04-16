namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RenameQuestionVotesToCloseVotesP2 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.QuestionVotes", "VoteTypeId", "dbo.VoteTypes");
            DropForeignKey("dbo.QuestionVotes", "QuestionId", "dbo.Questions");
            DropIndex("dbo.QuestionVotes", new[] { "QuestionId" });
            DropIndex("dbo.QuestionVotes", new[] { "VoteTypeId" });
            DropTable("dbo.QuestionVotes");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.QuestionVotes",
                c => new
                    {
                        QuestionVoteId = c.Int(nullable: false, identity: true),
                        QuestionId = c.Int(nullable: false),
                        VoteTypeId = c.Int(nullable: false),
                        FirstTimeSeen = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.QuestionVoteId);
            
            CreateIndex("dbo.QuestionVotes", "VoteTypeId");
            CreateIndex("dbo.QuestionVotes", "QuestionId");
            AddForeignKey("dbo.QuestionVotes", "QuestionId", "dbo.Questions", "Id", cascadeDelete: true);
            AddForeignKey("dbo.QuestionVotes", "VoteTypeId", "dbo.VoteTypes", "Id", cascadeDelete: true);
        }
    }
}
