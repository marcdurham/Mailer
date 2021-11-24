using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailerCommon
{
    public class Schedule
    {
        public string[] Headers { get; set; }
        public Dictionary<string, string[]> Days { get; set; }
    }
}
