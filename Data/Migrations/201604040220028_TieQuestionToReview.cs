namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TieQuestionToReview : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Questions", "ReviewID", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Questions", "ReviewID");
        }
    }
}
