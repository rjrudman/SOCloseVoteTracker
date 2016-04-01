namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FlagQuestionsAsBeingQueued : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Questions", "IsQueued", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Questions", "IsQueued");
        }
    }
}
