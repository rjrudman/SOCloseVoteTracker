namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TrackFullCVSPlsRequest : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CVPlsRequests", "FullMessage", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.CVPlsRequests", "FullMessage");
        }
    }
}
