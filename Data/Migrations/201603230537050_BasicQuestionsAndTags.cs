namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class BasicQuestionsAndTags : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Questions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Body = c.String(),
                        VoteCount = c.Int(nullable: false),
                        Closed = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Tags",
                c => new
                    {
                        TagName = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.TagName);
            
            CreateTable(
                "dbo.QuestionTags",
                c => new
                    {
                        QuestionId = c.Int(nullable: false),
                        TagId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.QuestionId, t.TagId })
                .ForeignKey("dbo.Questions", t => t.QuestionId, cascadeDelete: true)
                .ForeignKey("dbo.Tags", t => t.TagId, cascadeDelete: true)
                .Index(t => t.QuestionId)
                .Index(t => t.TagId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.QuestionTags", "TagId", "dbo.Tags");
            DropForeignKey("dbo.QuestionTags", "QuestionId", "dbo.Questions");
            DropIndex("dbo.QuestionTags", new[] { "TagId" });
            DropIndex("dbo.QuestionTags", new[] { "QuestionId" });
            DropTable("dbo.QuestionTags");
            DropTable("dbo.Tags");
            DropTable("dbo.Questions");
        }
    }
}
