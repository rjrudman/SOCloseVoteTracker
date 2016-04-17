using System;
using System.Text;
using Dapper;
using Data;
using Data.Entities;

namespace Core
{
    public static class Logger
    {
        private static void LogMessage(int level, string message)
        {
            const string INSERT_SQL = @"
INSERT INTO Logs (DateLogged, Level, Message) VALUES (GETUTCDATE(), @Level, @Message)
";
            var log = new Log
            {
                Level = level,
                Message = message
            };
            using (var con = ReadWriteDataContext.PlainConnection())
                con.Execute(INSERT_SQL, log);
        }

        public static void LogInfo(string message)
        {
            LogMessage(2, message);
        }

        public static void LogDebug(string message)
        {
            LogMessage(1, message);
        }

        public static void LogException(Exception ex, string message = null)
        {
            var sb = new StringBuilder();
            if (message != null)
                sb.AppendLine(message);
            while (ex != null)
            {
                sb.AppendLine("****");
                sb.AppendLine(ex.Message);
                sb.AppendLine(ex.StackTrace);
                sb.AppendLine("****");
                ex = ex.InnerException;
            }
            LogMessage(3, sb.ToString());
        }
    }
}
