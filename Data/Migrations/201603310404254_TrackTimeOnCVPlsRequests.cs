namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TrackTimeOnCVPlsRequests : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CVPlsRequests", "CreatedAt", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CVPlsRequests", "CreatedAt");
        }
    }
}
