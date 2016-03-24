namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DontStoreQuestionBody : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Questions", "Body");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Questions", "Body", c => c.String());
        }
    }
}
