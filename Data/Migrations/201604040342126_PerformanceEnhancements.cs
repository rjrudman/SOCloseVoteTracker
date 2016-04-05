namespace Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PerformanceEnhancements : DbMigration
    {
        public override void Up()
        {
            Sql(@"
CREATE NONCLUSTERED INDEX [IX_ReviewId] ON [dbo].[Questions]
(
	[ReviewId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");
            Sql(@"
CREATE NONCLUSTERED INDEX [IX_Closed] ON [dbo].[Questions]
(
	[Closed] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");
            Sql(@"
CREATE NONCLUSTERED INDEX [IX_LastTimeActive] ON [dbo].[Questions]
(
	[LastTimeActive] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");

            Sql(@"
CREATE NONCLUSTERED INDEX [IX_LastUpdated] ON [dbo].[Questions]
(
	[LastUpdated] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");
            Sql(@"
CREATE NONCLUSTERED INDEX [IX_Deleted] ON [dbo].[Questions]
(
	[Deleted] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
");
        }
        
        public override void Down()
        {
            Sql(@"DROP INDEX [IX_Deleted] ON [dbo].[Questions]");
            Sql(@"DROP INDEX [LastUpdated] ON [dbo].[Questions]");
            Sql(@"DROP INDEX [LastTimeActive] ON [dbo].[Questions]");
            Sql(@"DROP INDEX [Closed] ON [dbo].[Questions]");
            Sql(@"DROP INDEX [ReviewId] ON [dbo].[Questions]");
        }
    }
}
