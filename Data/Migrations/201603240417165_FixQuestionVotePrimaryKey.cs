namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixQuestionVotePrimaryKey : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.QuestionVotes");
            AddColumn("dbo.QuestionVotes", "QuestionVoteId", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.QuestionVotes", "QuestionVoteId");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.QuestionVotes");
            DropColumn("dbo.QuestionVotes", "QuestionVoteId");
            AddPrimaryKey("dbo.QuestionVotes", new[] { "QuestionId", "VoteTypeId" });
        }
    }
}
