using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using Data.Entities;

namespace Data
{
    public class DataContext : DbContext
    {
        public const string CONNECTION_STRING_NAME = "DBConnectionString";

        /// <summary>
        /// Get a plain connection to the database. Can be used with dapper. Uses the same connection string as EntityFramework
        /// </summary>
        /// <returns></returns>
        public static IDbConnection PlainConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[CONNECTION_STRING_NAME].ConnectionString;
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public DataContext() : base("DBConnectionString") { }

        public DbSet<Question> Questions { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<VoteType> VoteTypes { get; set; }
        public DbSet<CloseVote> CloseVotes { get; set; }
        public DbSet<CVPlsRequest> CVPlsRequests { get; set; }
        public DbSet<WebRequest> WebRequests { get; set; }
        public DbSet<OrderStatusChange> OrderStatusChanges { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Question>().ToTable("Questions");
            modelBuilder.Entity<Tag>().ToTable("Tags");
            modelBuilder.Entity<VoteType>().ToTable("VoteTypes");
            modelBuilder.Entity<CloseVote>().ToTable("CloseVotes");
            modelBuilder.Entity<CVPlsRequest>().ToTable("CVPlsRequests");
            modelBuilder.Entity<WebRequest>().ToTable("WebRequests");
            modelBuilder.Entity<OrderStatusChange>().ToTable("OrderStatusChanges");
            
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Tags)
                .WithMany(t => t.Questions)
                .Map(m => 
                    m.MapLeftKey("QuestionId")
                     .MapRightKey("TagName")
                     .ToTable("QuestionTags")
                );

            base.OnModelCreating(modelBuilder);
        }
    }
}
