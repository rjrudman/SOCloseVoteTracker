using System;
using System.ComponentModel.DataAnnotations;

namespace Data.Entities
{
    public class Log
    {
        [Key]
        public int LogId { get; set; }

        public DateTime DateLogged { get; set; }

        public string Message { get; set; }

        public int Level { get; set; }
    }
}
