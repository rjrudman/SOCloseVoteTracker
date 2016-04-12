using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using Data.Entities;

namespace Data
{
    public class ReadWriteDataContext : DbContext
    {
        public const string READ_WRITE_CONNECTION_STRING_NAME = "DBConnectionStringReadWrite";
        public const string READ_ONLY_CONNECTION_STRING_NAME = "DBConnectionStringReadOnly";

        public static IDbConnection ReadWritePlainConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[READ_WRITE_CONNECTION_STRING_NAME].ConnectionString;
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }
        public static IDbConnection ReadOnlyPlainConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[READ_WRITE_CONNECTION_STRING_NAME].ConnectionString;
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        public ReadWriteDataContext() : base(READ_WRITE_CONNECTION_STRING_NAME) { }
        public ReadWriteDataContext(string connectionString) : base(connectionString) { }

        public DbSet<Question> Questions { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<VoteType> VoteTypes { get; set; }
        public DbSet<CloseVote> CloseVotes { get; set; }
        public DbSet<CVPlsRequest> CVPlsRequests { get; set; }
        public DbSet<WebRequest> WebRequests { get; set; }
        public DbSet<OrderStatusChange> OrderStatusChanges { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Question>().ToTable("Questions");
            modelBuilder.Entity<Tag>().ToTable("Tags");
            modelBuilder.Entity<Log>().ToTable("Logs");
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


    public class ReadOnlyDataContext : ReadWriteDataContext
    {
        public ReadOnlyDataContext() : base(READ_ONLY_CONNECTION_STRING_NAME) { }
    }

}
