namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveForeignKeyForDuplicates : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Questions", "DuplicateParentId", "dbo.Questions");
            DropIndex("dbo.Questions", new[] { "DuplicateParentId" });
        }
        
        public override void Down()
        {
            CreateIndex("dbo.Questions", "DuplicateParentId");
            AddForeignKey("dbo.Questions", "DuplicateParentId", "dbo.Questions", "Id");
        }
    }
}
