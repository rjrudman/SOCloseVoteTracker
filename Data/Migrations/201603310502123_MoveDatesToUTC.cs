namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class MoveDatesToUTC : DbMigration
    {
        public override void Up()
        {
            Sql(@"
UPDATE CVPlsRequests SET CreatedAt = DATEADD(hh, DATEDIFF(hh, GETDATE(), GETUTCDATE()), CreatedAt)
UPDATE Questions SET LastUpdated = DATEADD(hh, DATEDIFF(hh, GETDATE(), GETUTCDATE()), LastUpdated)
UPDATE QuestionVotes SET FirstTimeSeen = DATEADD(hh, DATEDIFF(hh, GETDATE(), GETUTCDATE()), FirstTimeSeen)
");
        }
        
        public override void Down()
        {
        }
    }
}
