using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Scrapers.Chat;
using Core.Workers;
using Dapper;
using Data;
using Newtonsoft.Json.Linq;

namespace Core.Managers
{
    public static class ChatroomManager
    {
        public static void JoinAndWatchSOCVR()
        {
            LiteChatScraper.JoinAndWatchRoom(Utils.GlobalConfiguration.ChatRoomID, data => OnMessage(Utils.GlobalConfiguration.ChatRoomID, data));
        }

        private static void OnMessage(long roomId, Dictionary<string, JObject> data)
        {
            var messages = new[] { new { userId = 0, content = string.Empty } }.ToList();
            messages.Clear();

            try
            {
                var children = data[$"r{roomId}"].Children();
                foreach (var child in children)
                {
                    if (child.Path == "e")
                    {
                        var subChildren = child.Children();
                        foreach (var subChild in subChildren)
                        {
                            foreach (var @event in subChild)
                            {
                                var userId = @event.Value<int>("user_id");
                                var message = @event.Value<string>("content");
                                messages.Add(new { userId = userId, content = message });
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Failed to parse chat");
            }
            foreach (var message in messages)
                ParseContent(message.userId, message.content);
        }

        private static void ParseContent(int userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            if (!content.Contains(@"<a href=""//stackoverflow.com/questions/tagged/cv-pls"">"))
                return;

            var matches = QuestionIdRegex.Matches(content);
            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                var questionIDstr = match.Groups["questionID"].Value;
                int questionId;

                if (int.TryParse(questionIDstr, out questionId))
                    QueryQuestionAndLogRequest(userId, questionId, content);
            }
        }
        
        public static void QueryQuestionAndLogRequest(int userId, int questionId, string fullMessage)
        {
            Pollers.QueueQuestionQuery(questionId);

            using (var connection = ReadWriteDataContext.ReadWritePlainConnection())
                connection.Execute(UPSERT_CVPLS_SQL, new { UserId = userId, QuestionId = questionId, FullMessage = fullMessage });
        }

        const string UPSERT_CVPLS_SQL = @"INSERT INTO CVPlsRequests(UserId, QuestionId, FullMessage, CreatedAt) VALUES (@UserId, @QuestionId, @FullMessage, GETUTCDATE())";
        private static readonly Regex QuestionIdRegex = new Regex("\\/(q(uestions)?|p(osts)?)\\/(?<questionID>\\d+)\\/.*");
    }
}
