using System;
using System.ComponentModel.DataAnnotations;

namespace Data.Entities
{
    public class WebRequest
    {
        [Key]
        public int WebRequestID { get; set; }
        public DateTime DateExecuted { get; set; }
    }
}
