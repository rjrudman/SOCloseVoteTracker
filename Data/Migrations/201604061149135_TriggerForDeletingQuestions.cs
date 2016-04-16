namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class TriggerForDeletingQuestions : DbMigration
    {
        public override void Up()
        {
            Sql(@"
CREATE Trigger QuestionDeleteChanged on Questions
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF UPDATE (Deleted)
	BEGIN

	--Reset undelete votes when a question is undeleted
	UPDATE Questions
	SET UndeleteVotes = 0
	FROM Questions
	INNER JOIN Inserted ON Questions.Id = Inserted.Id
	WHERE Inserted.Deleted = 0

	--Reset delete votes when a question is deleted
	UPDATE Questions
	SET DeleteVotes = 0
	FROM Questions
	INNER JOIN Inserted ON Questions.Id = Inserted.Id
	WHERE Inserted.Deleted = 1


	--Track status change
	INSERT INTO OrderStatusChanges(QuestionId, TimeChanged, ChangeType)
		SELECT Inserted.Id, GETUTCDATE(), CASE WHEN Inserted.Deleted = 1 THEN 3 ELSE 4 END 
		FROM Inserted
		INNER JOIN Deleted ON Deleted.Id = Inserted.Id
		WHERE Deleted.Deleted <> Inserted.Deleted
	END
END
GO
");
        }
        
        public override void Down()
        {
            Sql(@"DROP Trigger QuestionDeleteChanged");
        }
    }
}
