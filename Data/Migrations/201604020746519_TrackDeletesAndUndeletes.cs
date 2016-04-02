namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TrackDeletesAndUndeletes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Questions", "DeleteVotes", c => c.Int(nullable: false));
            AddColumn("dbo.Questions", "UndeleteVotes", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Questions", "UndeleteVotes");
            DropColumn("dbo.Questions", "DeleteVotes");
        }
    }
}
