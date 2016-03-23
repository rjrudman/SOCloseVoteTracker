using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;

namespace Data
{
    public class DataContext : DbContext
    {
        private const string CONNECTION_STRING_NAME = "DBConnectionString";

        /// <summary>
        /// Get a plain connection to the database. Can be used with dapper. Uses the same connection string as EntityFramework
        /// </summary>
        /// <returns></returns>
        public static IDbConnection PlainConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings[CONNECTION_STRING_NAME].ConnectionString;
            return new SqlConnection(connectionString);
        }

        public DataContext() : base("DBConnectionString") { }
    }
}
