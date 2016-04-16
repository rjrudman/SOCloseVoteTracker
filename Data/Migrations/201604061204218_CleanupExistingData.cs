namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CleanupExistingData : DbMigration
    {
        public override void Up()
        {
            Sql(@"
DELETE CloseVotes
FROM CloseVotes
INNER JOIN Questions on QuestionID = ID
WHERE Closed = 1

UPDATE Questions SET DeleteVotes = 0 WHERE Deleted = 1
UPDATE Questions Set UndeleteVotes = 0 WHERE Deleted = 0
UPDATE Questions SET ReopenVotes = 0 WHERE Closed = 0
");
        }
        
        public override void Down()
        {
        }
    }
}
