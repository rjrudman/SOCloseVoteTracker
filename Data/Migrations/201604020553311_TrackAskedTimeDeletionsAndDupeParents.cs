namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TrackAskedTimeDeletionsAndDupeParents : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Questions", "Deleted", c => c.Boolean(nullable: false));
            AddColumn("dbo.Questions", "Asked", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Questions", "Asked");
            DropColumn("dbo.Questions", "Deleted");
        }
    }
}
