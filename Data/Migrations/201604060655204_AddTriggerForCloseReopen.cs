namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddTriggerForCloseReopen : DbMigration
    {
        public override void Up()
        {
            Sql(@"
CREATE Trigger QuestionCloseChanged on Questions
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF UPDATE (Closed)
	INSERT INTO OrderStatusChange(QuestionId, TimeChanged, ChangeType)
		SELECT Inserted.Id, GETUTCDATE(), CASE WHEN Inserted.Closed = 1 THEN 1 ELSE 2 END 
		FROM Inserted
		INNER JOIN Deleted ON Deleted.Id = Inserted.Id
		WHERE Deleted.Closed <> Inserted.Closed
END
GO
");
            Sql(@"
CREATE Trigger QuestionDeleteChanged on Questions
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF UPDATE (Closed)
	INSERT INTO OrderStatusChange(QuestionId, TimeChanged, ChangeType)
		SELECT Inserted.Id, GETUTCDATE(), CASE WHEN Inserted.Deleted = 1 THEN 3 ELSE 4 END 
		FROM Inserted
		INNER JOIN Deleted ON Deleted.Id = Inserted.Id
		WHERE Deleted.Deleted <> Inserted.Deleted
END
GO
");
        }
        
        public override void Down()
        {
            Sql(@"
DROP Trigger QuestionCloseChanged
");
            Sql(@"
DROP Trigger QuestionDeleteChanged
");
        }
    }
}
