using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ServiceModel;
using System.Xml;
using System.Xml.Linq;

namespace Interface_Modules
{
    public class CA_SDM
    {
        private static SDMWS.USD_WebServiceSoapClient WebServiceSoapClient()
        {
            BasicHttpBinding bhb = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            bhb.Name = "USD_WebServiceSoapSoapBinding";
            bhb.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            bhb.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
            bhb.MaxReceivedMessageSize = 2147483647;

            EndpointAddress endpointAddress = new EndpointAddress("https://servicedesk.dhsoha.state.or.us/axis/services/USD_R11_WebService");
            SDMWS.USD_WebServiceSoapClient WS_Client = new SDMWS.USD_WebServiceSoapClient(bhb, endpointAddress);

            return WS_Client;
        }

        public static int Get_SIDToken(string username, string password)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            int SID = ws_client.login(username, password);

            return SID;
        }

        public static void Remove_SIDToken(int SID)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();
            
            ws_client.logout(SID);
        }

        public static List<SDM_Contact> Find_Contact (int SID, string UserID = "", string Last_Name = "", string First_Name = "", string Email = "", string Access_Type = "", bool Only_Active = true)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            int inactiveFlag = new int();

            if (Only_Active)
                inactiveFlag = 0;
            else
                inactiveFlag = -999;

            //XmlDocument ReturnedXml = new XmlDocument();
            //ReturnedXml.LoadXml(ws_client.findContacts(SID, UserID, Last_Name, First_Name, Email, Access_Type, inactiveFlag));

            XDocument ReturnedXml = XDocument.Parse(ws_client.findContacts(SID, UserID, Last_Name, First_Name, Email, Access_Type, inactiveFlag));

            //List<SDM_Contact> results = new List<SDM_Contact>();
            List<SDM_Contact> results = new List<SDM_Contact>();
            foreach (var contact in ReturnedXml.Descendants("UDSObject"))
            {
                SDM_Contact nextContact = new SDM_Contact();

                nextContact.Handle = contact.Element("Handle").Value;

                foreach (var attribute in contact.Descendants("Attribute"))
                {
                    if (attribute.Element("AttrName").Value == "first_name")
                        nextContact.First_Name = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "last_name")
                        nextContact.Last_Name = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "email_address")
                        nextContact.Email = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "userid")
                        nextContact.UserId = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "access_type")
                        nextContact.AccessType = Int32.Parse(attribute.Element("AttrValue").Value);
                    if (attribute.Element("AttrName").Value == "id")
                        nextContact.Id = attribute.Element("AttrValue").Value;
                }

                results.Add(nextContact);
            }

            return results;
        }

        public static List<SDM_Ticket> Get_TicketHistory(int SID, string UserHandle)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            UserHandle = UserHandle.TrimStart("cnt:".ToCharArray());

            var TicketHistoryList = ws_client.doQuery(SID, "cr", "customer.id = U'" + UserHandle + "'");

            int listHandle = TicketHistoryList.listHandle;
            double listLength = TicketHistoryList.listLength;
            double counter = Math.Ceiling(listLength / 250);

            //List which attributes to get (must match SDM_Ticket object properties)
            string[] attrNames = { "assignee", "status", "summary", "description", "type", "id", "open_date" };

            List<SDM_Ticket> results = new List<SDM_Ticket>();

            List<SDM_Ticket> tickets = new List<SDM_Ticket>();

            for (int i = 1; i <= counter; i++)
            {
                var rawXml = ws_client.getListValues(SID, listHandle, 250 * (i - 1), -1, attrNames);
                
                XDocument ReturnedXml = XDocument.Parse(rawXml);

                foreach (var ticket in ReturnedXml.Descendants("UDSObject"))
                {
                    SDM_Ticket nextTicket = new SDM_Ticket();

                    nextTicket.Handle = ticket.Element("Handle").Value;

                    foreach (var attribute in ticket.Descendants("Attribute"))
                    {
                        if (attribute.Element("AttrName").Value == "status")
                            nextTicket.Status = attribute.Element("AttrValue").Value;
                        if (attribute.Element("AttrName").Value == "summary")
                            nextTicket.Summary = attribute.Element("AttrValue").Value;
                        if (attribute.Element("AttrName").Value == "description")
                            nextTicket.Description = attribute.Element("AttrValue").Value;
                        if (attribute.Element("AttrName").Value == "type")
                            nextTicket.Type = attribute.Element("AttrValue").Value;
                        if (attribute.Element("AttrName").Value == "id")
                            nextTicket.Id = Int32.Parse(attribute.Element("AttrValue").Value);
                        if (attribute.Element("AttrName").Value == "open_date")
                            nextTicket.OpenDate = Int32.Parse(attribute.Element("AttrValue").Value);

                    }

                    results.Add(nextTicket);
                }
            }
            
            return results;
        }

        public static string Get_Site(int SID, string UserId)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();
            
            List<SDM_Contact> contactList = Find_Contact(SID, UserId);
            SDM_Contact contact = new SDM_Contact();

            if (contactList.Count() != 1)
                throw new Exception();
            else
                contact = contactList[0];

            var rawXml = ws_client.getContact(SID, contact.Id.ToString());

            XDocument ReturnedXml = XDocument.Parse(rawXml);

            string loc = String.Empty;
            
            foreach (var attribute in ReturnedXml.Descendants("Attribute"))
            {
                if (attribute.Element("AttrName").Value == "location")
                    loc = attribute.Element("AttrValue").Value;
            }

            rawXml = ws_client.doSelect(SID, "loc", "id=U'" + loc + "'", -1, new string[] {"site"});

            ReturnedXml = XDocument.Parse(rawXml);

            string siteNum = String.Empty;

            foreach (var attribute in ReturnedXml.Descendants("Attribute"))
            {
                if (attribute.Element("AttrName").Value == "site")
                    siteNum = attribute.Element("AttrValue").Value;
            }

            rawXml = ws_client.doSelect(SID, "site", "id=" + siteNum, -1, new string[] { "name" });

            ReturnedXml = XDocument.Parse(rawXml);

            string siteName = String.Empty;

            foreach (var attribute in ReturnedXml.Descendants("Attribute"))
            {
                if (attribute.Element("AttrName").Value == "name")
                    siteName = attribute.Element("AttrValue").Value;
            }

            return siteName;
        }

        public static List<SDM_Contact> Get_GroupMember(int SID, string GroupName)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            var rawXml = ws_client.getGroupMemberListValues(SID, "group.last_name='" + GroupName + "'", -1, new string[] { "member" });

            XDocument ReturnedXml = XDocument.Parse(rawXml);

            List<SDM_Contact> members = new List<SDM_Contact>();
            List<string> memberIdList = new List<string>();

            foreach (var attribute in ReturnedXml.Descendants("Attribute"))
            {
                memberIdList.Add(attribute.Element("AttrValue").Value);
            }

            foreach (string memberId in memberIdList)
            {
                rawXml = ws_client.getContact(SID, memberId);

                ReturnedXml = XDocument.Parse(rawXml);

                foreach (var member in ReturnedXml.Descendants("Attribute"))
                {
                    if (member.Element("AttrName").Value == "userid")
                    {
                        List<SDM_Contact> memberList = Find_Contact(SID, member.Element("AttrValue").Value);
                        if (memberList.Count() != 1)
                            throw new IndexOutOfRangeException();
                        else
                            members.Add(memberList[0]);
                    }
                }
            }

            return members;
        }

        public static int Get_TicketId(int SID, int TicketNumber)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            int ticketNumber = 0;

            var rawXml = ws_client.doSelect(SID, "cr", "ref_num='757840'", -1, new string[] {"id"}); 

            XDocument ReturnedXml = XDocument.Parse(rawXml);

            foreach (var attribute in ReturnedXml.Descendants("Attribute"))
            {
                if (attribute.Element("AttrName").Value == "id")
                    ticketNumber = Int32.Parse(attribute.Element("AttrValue").Value);
            }

            
            return ticketNumber;
        }



    }


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
}
