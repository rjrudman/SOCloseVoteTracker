using System;
using System.Text.RegularExpressions;
using ChatExchangeDotNet;
using Dapper;
using Data;
using Hangfire;
using Utils;

namespace Core.Workers
{
    public class Chat
    {
        const string UPSERT_CVPLS_SQL = @"
INSERT INTO CVPlsRequests(UserId, QuestionId, FullMessage, CreatedAt) VALUES (@UserId, @QuestionId, @FullMessage, GETUTCDATE())
";

        private static Client chatClient;
        private static Room chatRoom;
        private static readonly Regex QuestionIdRegex = new Regex("\\/(q(uestions)?|p(osts)?)\\/(?<questionID>\\d+)\\/.*");

        public static void JoinAndWatchRoom(string roomURL)
        {
            chatClient = new Client(Configuration.UserName, Configuration.Password, Configuration.ProxyUrl, Configuration.ProxyUsername, Configuration.ProxyPassword);
            chatRoom = chatClient.JoinRoom(roomURL);
            chatRoom.EventManager.IgnoreOwnEvents = false;

            var processMessage = new Action<Message>(message =>
            {
                if (!message.Content.Contains("[tag:cv-pls]"))
                    return;

                var matches = QuestionIdRegex.Matches(message.Content);
                foreach (Match match in matches)
                {
                    if (!match.Success)
                        continue;

                    var questionIDstr = match.Groups["questionID"].Value;
                    int questionId;
                    
                    if (int.TryParse(questionIDstr, out questionId))
                        BackgroundJob.Enqueue(() => QueryQuestionAndLogRequest(message.Author.ID, questionId, message.Content, DateTime.Now));
                }
            });

            chatRoom.EventManager.ConnectListener(EventType.MessagePosted, processMessage);
            chatRoom.EventManager.ConnectListener(EventType.MessageEdited, processMessage);
        }

        public static void QueryQuestionAndLogRequest(int userId, int questionId, string fullMessage, DateTime requestTime)
        {
            Pollers.QueryQuestion(questionId, requestTime);

            using (var connection = DataContext.PlainConnection())
            {
                connection.Open();
                connection.Execute(UPSERT_CVPLS_SQL, new { UserId = userId, QuestionId = questionId, FullMessage = fullMessage });
            }

            //Check on the question again in an hour
            var delay = TimeSpan.FromHours(1);
            BackgroundJob.Schedule(() => Pollers.QueryQuestion(questionId, DateTime.Now.Add(delay)), delay);
        }
    }
}
