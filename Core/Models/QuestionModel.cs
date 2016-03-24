using System.Collections.Generic;

namespace Core.Models
{
    public class QuestionModel
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public bool Closed { get; set; }

        public IList<string> Tags { get; set; }
        public IDictionary<int,int> CloseVotes { get; set; }
    }
}
