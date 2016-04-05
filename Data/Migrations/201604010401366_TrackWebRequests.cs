namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class TrackWebRequests : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.WebRequests",
                c => new
                    {
                        WebRequestID = c.Int(nullable: false, identity: true),
                        DateExecuted = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.WebRequestID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.WebRequests");
        }
    }
}
