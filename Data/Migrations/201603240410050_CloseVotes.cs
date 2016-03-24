namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CloseVotes : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.QuestionVotes", "QuestionId", "dbo.Questions");
            DropForeignKey("dbo.QuestionVotes", "VoteTypeId", "dbo.VoteTypes");

            DropTable("dbo.QuestionVotes");
            CreateTable(
                "dbo.QuestionVotes",
                c => new
                    {
                        QuestionId = c.Int(nullable: false),
                        VoteTypeId = c.Int(nullable: false),
                        FirstTimeSeen = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => new { t.QuestionId, t.VoteTypeId })
                .ForeignKey("dbo.VoteTypes", t => t.VoteTypeId, cascadeDelete: true)
                .ForeignKey("dbo.Questions", t => t.QuestionId, cascadeDelete: true);
            
            DropColumn("dbo.Questions", "VoteCount");
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.QuestionVotes", "QuestionId", "dbo.Questions");
            DropForeignKey("dbo.QuestionVotes", "VoteTypeId", "dbo.VoteTypes");
            DropTable("dbo.QuestionVotes");

            CreateTable(
                "dbo.QuestionVotes",
                c => new
                    {
                        QuestionId = c.Int(nullable: false),
                        VoteTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.QuestionId, t.VoteTypeId });
            
            AddColumn("dbo.Questions", "VoteCount", c => c.Int(nullable: false));
            
            AddForeignKey("dbo.QuestionVotes", "VoteTypeId", "dbo.VoteTypes", "Id", cascadeDelete: true);
            AddForeignKey("dbo.QuestionVotes", "QuestionId", "dbo.Questions", "Id", cascadeDelete: true);
        }
    }
}
