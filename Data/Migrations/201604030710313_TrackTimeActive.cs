namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class TrackTimeActive : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Questions", "LastTimeActive", c => c.DateTime());
            Sql(@"
UPDATE dbo.Questions 
SET LastTimeActive = LastUpdated");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Questions", "LastTimeActive");
        }
    }
}
