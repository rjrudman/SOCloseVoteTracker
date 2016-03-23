using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Data.Entities
{
    public class Tag
    {
        [Key]
        public string TagName { get; set; }

        public IList<Question> Questions { get; set; }
    }
}
