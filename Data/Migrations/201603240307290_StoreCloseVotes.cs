namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class StoreCloseVotes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.VoteTypes",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Description = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.QuestionVotes",
                c => new
                    {
                        QuestionId = c.Int(nullable: false),
                        VoteTypeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.QuestionId, t.VoteTypeId })
                .ForeignKey("dbo.Questions", t => t.QuestionId, cascadeDelete: true)
                .ForeignKey("dbo.VoteTypes", t => t.VoteTypeId, cascadeDelete: true)
                .Index(t => t.QuestionId)
                .Index(t => t.VoteTypeId);

            Sql(@"
INSERT INTO VoteTypes(Id, Description)
VALUES (4, 'Hardware/Software')

INSERT INTO VoteTypes(Id, Description)
VALUES (7, 'Server/Network')

INSERT INTO VoteTypes(Id, Description)
VALUES (16, 'Off-site Resource')

INSERT INTO VoteTypes(Id, Description)
VALUES (13, 'No MCVE')

INSERT INTO VoteTypes(Id, Description)
VALUES (11, 'Typo')

INSERT INTO VoteTypes(Id, Description)
VALUES (2, 'Migration')

INSERT INTO VoteTypes(Id, Description)
VALUES (3, 'Other')

-- These IDs are not taken from stack overflow. Offset at 1000 to prevent collisions of new close reasons (it uses a string rather than an integer for these)
INSERT INTO VoteTypes(Id, Description)
VALUES (1000, 'Duplicate')
INSERT INTO VoteTypes(Id, Description)
VALUES (1001, 'Unclear')
INSERT INTO VoteTypes(Id, Description)
VALUES (1002, 'Broad')
INSERT INTO VoteTypes(Id, Description)
VALUES (1003, 'Opinion Based')
");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.QuestionVotes", "VoteTypeId", "dbo.VoteTypes");
            DropForeignKey("dbo.QuestionVotes", "QuestionId", "dbo.Questions");
            DropIndex("dbo.QuestionVotes", new[] { "VoteTypeId" });
            DropIndex("dbo.QuestionVotes", new[] { "QuestionId" });
            DropTable("dbo.QuestionVotes");
            DropTable("dbo.VoteTypes");
        }
    }
}
