using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interface_Modules
{
    public class SDM_Contact
    {
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Email { get; set; }
        public string Handle { get; set; }
        public string UserId { get; set; }
        public string Id { get; set; }
        public int AccessType { get; set; }
    }

    public class SDM_Ticket
    {
        public string Handle { get; set; }
        public string Assignee { get; set; }
        public string Status { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public int Id { get; set; }
        public int OpenDate { get; set; }
    }

    public class SDM_Activity_Log
    {
        public string ActionDesc { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Analyst { get; set; }
        public string Description { get; set; }
    }
}
