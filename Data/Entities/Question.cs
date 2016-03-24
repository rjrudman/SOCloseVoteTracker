using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Data.Entities
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }
        public int VoteCount { get; set; }
        public bool Closed { get; set; }

        public IList<Tag> Tags { get; set; }
    }
}
