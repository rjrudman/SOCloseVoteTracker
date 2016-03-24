namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CheckLastUpdatedQuestionTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Questions", "LastUpdated", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Questions", "LastUpdated");
        }
    }
}
