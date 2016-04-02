namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixQuestionTagsColumnName : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.QuestionTags", name: "TagId", newName: "TagName");
            RenameIndex(table: "dbo.QuestionTags", name: "IX_TagId", newName: "IX_TagName");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.QuestionTags", name: "IX_TagName", newName: "IX_TagId");
            RenameColumn(table: "dbo.QuestionTags", name: "TagName", newName: "TagId");
        }
    }
}
