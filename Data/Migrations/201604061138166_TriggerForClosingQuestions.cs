namespace Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TriggerForClosingQuestions : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.OrderStatusChange", newName: "OrderStatusChanges");

            Sql(@"
CREATE Trigger QuestionCloseChanged on Questions
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF UPDATE (Closed)
	BEGIN
	
	--Delete all close votes when a question is closed
	DELETE CloseVotes
	FROM CloseVotes
	INNER JOIN Inserted ON CloseVotes.QuestionId = Inserted.Id
	WHERE Inserted.Closed = 1

	--Reset reopen votes when a question is reopened
	UPDATE Questions
	SET ReopenVotes = 0
	FROM Questions
	INNER JOIN Inserted ON Questions.Id = Inserted.Id
	WHERE Inserted.Closed = 0

	--Track status change
	INSERT INTO OrderStatusChanges(QuestionId, TimeChanged, ChangeType)
		SELECT Inserted.Id, GETUTCDATE(), CASE WHEN Inserted.Closed = 1 THEN 1 ELSE 2 END 
		FROM Inserted
		INNER JOIN Deleted ON Deleted.Id = Inserted.Id
		WHERE Deleted.Closed <> Inserted.Closed
	END
END
GO
");
        }
        
        public override void Down()
        {
            Sql(@"DROP Trigger QuestionCloseChanged");

            RenameTable(name: "dbo.OrderStatusChanges", newName: "OrderStatusChange");
        }
    }
}
