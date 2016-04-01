namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StartTrackingMoreInfo : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CVPlsRequests",
                c => new
                    {
                        CVPlsRequestId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        QuestionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.CVPlsRequestId)
                .ForeignKey("dbo.Questions", t => t.QuestionId, cascadeDelete: true)
                .Index(t => t.QuestionId);
            
            AddColumn("dbo.Questions", "CloseVoteTypeId", c => c.Int());
            AddColumn("dbo.Questions", "DuplicateParentId", c => c.Int());
            CreateIndex("dbo.Questions", "CloseVoteTypeId");
            CreateIndex("dbo.Questions", "DuplicateParentId");
            AddForeignKey("dbo.Questions", "CloseVoteTypeId", "dbo.VoteTypes", "Id");
            AddForeignKey("dbo.Questions", "DuplicateParentId", "dbo.Questions", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Questions", "DuplicateParentId", "dbo.Questions");
            DropForeignKey("dbo.CVPlsRequests", "QuestionId", "dbo.Questions");
            DropForeignKey("dbo.Questions", "CloseVoteTypeId", "dbo.VoteTypes");
            DropIndex("dbo.CVPlsRequests", new[] { "QuestionId" });
            DropIndex("dbo.Questions", new[] { "DuplicateParentId" });
            DropIndex("dbo.Questions", new[] { "CloseVoteTypeId" });
            DropColumn("dbo.Questions", "DuplicateParentId");
            DropColumn("dbo.Questions", "CloseVoteTypeId");
            DropTable("dbo.CVPlsRequests");
        }
    }
}
