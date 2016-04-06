namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class TrackStatusChanges : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.OrderStatusChange",
                c => new
                    {
                        OrderStatusChangeId = c.Int(nullable: false, identity: true),
                        QuestionId = c.Int(nullable: false),
                        TimeChanged = c.DateTime(nullable: false),
                        ChangeType = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.OrderStatusChangeId)
                .ForeignKey("dbo.Questions", t => t.QuestionId, cascadeDelete: true)
                .Index(t => t.QuestionId);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OrderStatusChange", "QuestionId", "dbo.Questions");
            DropIndex("dbo.OrderStatusChange", new[] { "QuestionId" });
            DropTable("dbo.OrderStatusChange");
        }
    }
}
