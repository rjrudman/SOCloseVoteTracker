namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class StartLogging : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Logs",
                c => new
                    {
                        LogId = c.Int(nullable: false, identity: true),
                        DateLogged = c.DateTime(nullable: false),
                        Message = c.String(),
                        Level = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.LogId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Logs");
        }
    }
}
