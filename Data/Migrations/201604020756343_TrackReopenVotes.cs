namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class TrackReopenVotes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Questions", "ReopenVotes", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Questions", "ReopenVotes");
        }
    }
}
