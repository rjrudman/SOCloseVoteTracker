namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UseTimestampForEnqueingQuestions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Questions", "NextQueueTime", c => c.DateTime());
            DropColumn("dbo.Questions", "IsQueued");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Questions", "IsQueued", c => c.Boolean(nullable: false));
            DropColumn("dbo.Questions", "NextQueueTime");
        }
    }
}
