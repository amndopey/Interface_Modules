using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Interface_Modules;
using System.Diagnostics;
using System.Xml;

namespace Test_Console
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.Write("Employee ID: ");
            string userName = Console.ReadLine();

            Console.WriteLine("");

            Console.Write("Password: ");
            string password = ReadPassword();

            int sid_token = CA_SDM.Get_SIDToken(userName, password);

            //List<SDM_Activity_Log> activityLog = CA_SDM.Get_ActivityLog(sid_token, 638977);

            //foreach (var activity in activityLog)
            //{
            //    Console.WriteLine("Activity: {0}", activity.ActionDesc);
            //    Console.WriteLine("Description: {0}", activity.Description);
            //    Console.WriteLine("Analyst: {0}", activity.Analyst);
            //    Console.WriteLine("Timestamp: {0}", activity.TimeStamp);
            //    Console.WriteLine("-------------------------------");
            //}

            //List<SDM_Contact> test = CA_SDM.Find_Contact(sid_token, "or0210312");

            //foreach (var contact in test)
            //{
            //    Console.WriteLine("Handle: {0}", contact.Handle);
            //    Console.WriteLine("First Name: {0}", contact.First_Name);
            //    Console.WriteLine("Last Name: {0}", contact.Last_Name);
            //    Console.WriteLine("---------------------");

            //    List<SDM_Ticket> tickets = CA_SDM.Get_TicketHistory(sid_token, contact.Handle);

            //    foreach (SDM_Ticket ticket in tickets)
            //    {
            //        Console.WriteLine("Ticket Id: {0}", ticket.Id);
            //        Console.WriteLine("");
            //    }
                //Console.WriteLine("---------------------");

                //string site = CA_SDM.Get_Site(sid_token, contact.UserId);
                //Console.WriteLine("Site: {0}", site);
            //}
            
            //-------------------------------------------------------------------------------------------------
            
            //SDM_Contact me = test[0];

            //List<SDM_Contact> groupMembers = CA_SDM.Get_GroupMember(sid_token, "CSS T2 On Site " + CA_SDM.Get_Site(sid_token, "or0210312"));

            //foreach (SDM_Contact person in groupMembers)
            //{
            //    Console.WriteLine(person.Last_Name + ", " + person.First_Name);
            //}


            //List<SDM_Contact> vipList = CA_SDM.Get_VIPContacts(sid_token);

            //foreach (SDM_Contact vip in vipList)
            //{
            //    Console.WriteLine(vip.Last_Name + ", " + vip.First_Name);
            //}

            int test = CA_SDM.Create_Ticket(sid_token, "OR0210312", "", "R", "Assistance/Inquiry.Hardware.IOS", "CSS CC Mobile", "OP", "", "");


            Console.WriteLine("--End of program--");
            Console.ReadLine();

            return;
        }

        public static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo info = Console.ReadKey(true);
            while (info.Key != ConsoleKey.Enter)
            {
                if (info.Key != ConsoleKey.Backspace)
                {
                    Console.Write("*");
                    password += info.KeyChar;
                }
                else if (info.Key == ConsoleKey.Backspace)
                {
                    if (!string.IsNullOrEmpty(password))
                    {
                        // remove one character from the list of password characters
                        password = password.Substring(0, password.Length - 1);
                        // get the location of the cursor
                        int pos = Console.CursorLeft;
                        // move the cursor to the left by one character
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        // replace it with space
                        Console.Write(" ");
                        // move the cursor to the left by one character again
                        Console.SetCursorPosition(pos - 1, Console.CursorTop);
                    }
                }
                info = Console.ReadKey(true);
            }
            // add a new line because user pressed enter at the end of their password
            Console.WriteLine();
            return password;
        }

    }
}
