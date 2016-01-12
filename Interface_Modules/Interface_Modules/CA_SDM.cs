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
            bhb.MaxBufferSize = 2147483647;
            bhb.MaxBufferPoolSize = 2147483647;
            bhb.ReaderQuotas.MaxArrayLength = 2147483647;
            bhb.ReaderQuotas.MaxStringContentLength = 2147483647;

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

        public static List<SDM_Contact> Find_Contact (int SID,
                                                      string UserID = "",
                                                      string Last_Name = "",
                                                      string First_Name = "",
                                                      string Email = "",
                                                      string Access_Type = "",
                                                      bool Only_Active = true)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            int inactiveFlag = new int();

            if (Only_Active)
                inactiveFlag = 0;
            else
                inactiveFlag = -999;

            XDocument ReturnedXml = XDocument.Parse(ws_client.findContacts(SID, UserID, Last_Name, First_Name, Email, Access_Type, inactiveFlag));

            List<SDM_Contact> results = new List<SDM_Contact>();
            
            foreach (var contact in ReturnedXml.Descendants("UDSObject"))
            {
                SDM_Contact nextContact = new SDM_Contact();

                results.Add(Find_Contact_By_Handle(SID, contact.Element("Handle").Value));
            }

            return results;
        }

        private static SDM_Contact Find_Contact_By_Handle(int SID, string Handle)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();
            
            SDM_Contact contact = new SDM_Contact();

            contact.Handle = Handle.Replace("cnt:","");

            var rawXml = ws_client.getContact(SID, contact.Handle);

            XDocument ReturnedXml = XDocument.Parse(rawXml);

            foreach (var attributes in ReturnedXml.Descendants("UDSObject"))
            {
                foreach (var attribute in attributes.Descendants("Attribute"))
                {
                    if (attribute.Element("AttrName").Value == "first_name")
                        contact.First_Name = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "last_name")
                        contact.Last_Name = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "email_address")
                        contact.Email = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "userid")
                        contact.UserId = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "access_type")
                        contact.AccessType = Int32.Parse(attribute.Element("AttrValue").Value);
                    if (attribute.Element("AttrName").Value == "id")
                        contact.Id = attribute.Element("AttrValue").Value;
                }
            }

            return contact;
        }

        public static List<SDM_Ticket> Get_TicketHistory(int SID, string UserId)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            List<SDM_Contact> user = Find_Contact(SID, UserId);

            if (user.Count() != 1)
                throw new ArgumentException("UserId returns " + user.Count() + " users (Must be 1)");

            string UserHandle = user[0].Handle.TrimStart("cnt:".ToCharArray());

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

        private static int Get_TicketId(int SID, int TicketNumber)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            int ticketId = 0;

            var rawXml = ws_client.doSelect(SID, "cr", "ref_num='" + TicketNumber + "'", -1, new string[] {"id"}); 

            XDocument ReturnedXml = XDocument.Parse(rawXml);

            foreach (var attribute in ReturnedXml.Descendants("Attribute"))
            {
                if (attribute.Element("AttrName").Value == "id")
                    ticketId = Int32.Parse(attribute.Element("AttrValue").Value);
            }

            
            return ticketId;
        }

        public static List<SDM_Activity_Log> Get_ActivityLog(int SID, int TicketNumber)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            int ticketID = Get_TicketId(SID, TicketNumber);

            SDMWS.ListResult ticketHandleRequest = ws_client.getRelatedList(SID, "cr:" + ticketID, "act_log_all");

            int listHandle = ticketHandleRequest.listHandle;

            string[] myAttr = { "action_desc", "time_stamp", "analyst", "description" };

            var rawXml = ws_client.getListValues(SID, listHandle, 0, -1, myAttr);

            XDocument ReturnedXml = XDocument.Parse(rawXml);

            List<SDM_Activity_Log> activityLog = new List<SDM_Activity_Log>();

            DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            foreach (var attributeSet in ReturnedXml.Descendants("Attributes"))
            {
                SDM_Activity_Log activity = new SDM_Activity_Log();
                
                foreach (var attribute in attributeSet.Descendants("Attribute"))
                {
                    if (attribute.Element("AttrName").Value == "action_desc")
                        activity.ActionDesc = attribute.Element("AttrValue").Value.Trim();
                    if (attribute.Element("AttrName").Value == "time_stamp")
                        activity.TimeStamp = startTime.AddSeconds(Int32.Parse(attribute.Element("AttrValue").Value)).ToLocalTime();
                    if (attribute.Element("AttrName").Value == "analyst")
                        activity.Analyst = Find_Contact_By_Handle(SID, attribute.Element("AttrValue").Value).UserId;
                    if (attribute.Element("AttrName").Value == "description")
                        activity.Description = attribute.Element("AttrValue").Value.Trim();
                }

                activityLog.Add(activity);
            }

            return activityLog;
        }

        public static List<SDM_Contact> Get_VIPContacts(int SID)
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            var rawXml = ws_client.doSelect(SID, "special_handling", @"description LIKE '%VIP'", -1, new string[] { "persistent_id" });

            XDocument ReturnedXml = XDocument.Parse(rawXml);

            string persistentId = "";

            foreach (var attribute in ReturnedXml.Descendants("Attribute"))
            {
                if (attribute.Element("AttrName").Value == "persistent_id")
                    persistentId = attribute.Element("AttrValue").Value;
            }
            
            SDMWS.ListResult getList = ws_client.getRelatedList(SID, persistentId, "cnthandling_list");

            int listHandle = getList.listHandle;

            rawXml = ws_client.getListValues(SID, listHandle, 1, -1, new string[] { "contact" });

            ReturnedXml = XDocument.Parse(rawXml);

            List<SDM_Contact> vipList = new List<SDM_Contact>();

            foreach (var attributeSet in ReturnedXml.Descendants("Attributes"))
            {
                SDM_Contact nextVip = new SDM_Contact();

                foreach (var attribute in attributeSet.Descendants("Attribute"))
                {
                    if (attribute.Element("AttrName").Value == "contact")
                    {
                        string contactHandle = attribute.Element("AttrValue").Value.Trim();

                        nextVip = Find_Contact_By_Handle(SID, contactHandle);
                    }
                
                
                }

                vipList.Add(nextVip);
            }

            return vipList;
        }

        public static int Create_Ticket(int SID,
                                        string CreatorUserId,
                                        string AffectedUserId,
                                        string TicketType,
                                        string RequestArea,
                                        string Group,
                                        string Status = "OP",
                                        string RequesterUserID = "",
                                        string AssigneeUserID = "",
                                        int Priority = 0,
                                        int Severity = 0,
                                        int Urgency = 2,
                                        int Impact = 1,
                                        string Summary = "",
                                        string Description = "")
        {
            SDMWS.USD_WebServiceSoapClient ws_client = WebServiceSoapClient();

            Dictionary<string, string> attrValues = new Dictionary<string, string>();

            //Check that user comes back with one contact
            SDM_Contact creatorUser = new SDM_Contact();            
            if (!String.IsNullOrEmpty(CreatorUserId))
            {

                List<SDM_Contact> tempUserCheck = Find_Contact(SID, CreatorUserId);

                if (tempUserCheck.Count() != 1)
                    throw new ArgumentException("UserId returns " + tempUserCheck.Count() + " users (Must be 1)");

                creatorUser = tempUserCheck[0];
            }
            else
                throw new ArgumentException("Invalid creator user ID given");

            //Check that Affected User comes back with only one contact

            if (!String.IsNullOrEmpty(AffectedUserId))
            {
                List<SDM_Contact> tempUserCheck = Find_Contact(SID, AffectedUserId);

                if (tempUserCheck.Count() != 1)
                    throw new ArgumentException("AffectedUserId returns " + tempUserCheck.Count() + " users (Must be 1)");

                attrValues.Add("customer", tempUserCheck[0].Handle);
            }
            else
                throw new ArgumentException("No valid affected user given");

            //Verify Requester comes back with one contact
            if (!String.IsNullOrEmpty(RequesterUserID))
            {
                List<SDM_Contact> tempUserCheck = Find_Contact(SID, RequesterUserID);

                if (tempUserCheck.Count() != 1)
                    throw new ArgumentException("RequesterUserId returns " + tempUserCheck.Count() + " users (Must be 1)");

                attrValues.Add("requested_by", tempUserCheck[0].Handle);
            }

            
            //Verify ticket type is valid and create name-value string
            if (TicketType.ToUpper() != "R" && TicketType.ToUpper() != "I")
                throw new ArgumentException("Ticket Type must be either 'I' for Incident or 'R' for Request");
            else
                attrValues.Add("type", TicketType.ToUpper());

            //Verify Request Area and create name-value string
            var rawXml = ws_client.doSelect(SID, "pcat", "sym = '" + RequestArea + "'", 250, new string[] { "sym" });
            XDocument ReturnedXml = XDocument.Parse(rawXml);

            if (ReturnedXml.Descendants("UDSObjectList").Elements().Count() == 0)
                throw new ArgumentException("Invalid Request Area");
            else
            {
                foreach (var attribute in ReturnedXml.Descendants("UDSObject"))
                {
                    attrValues.Add("category", attribute.Element("Handle").Value);
                }
            }

            //Verify group
            rawXml = ws_client.doSelect(SID, "cnt", "last_name = '" + Group + "' AND type = 2308", 250, new string[] { "persistent_id" });
            ReturnedXml = XDocument.Parse(rawXml);

            if (ReturnedXml.Descendants("UDSObjectList").Elements().Count() == 0)
                throw new ArgumentException("Invalid Group");
            else
            {
                foreach (var attribute in ReturnedXml.Descendants("UDSObject"))
                {
                    attrValues.Add("group", attribute.Element("Handle").Value);
                }
            }

            //Verify Status
            if (Status.ToUpper() != "OP" && Status.ToUpper() != "CL")
                throw new ArgumentException("Ticket status must be 'OP' or 'CL'");
            else
                attrValues.Add("status", Status.ToUpper());

            //Verify proper priority
            if (Priority < 0 || Priority > 5)
                throw new ArgumentException("Priority must be between 0 and 5");
            else
                attrValues.Add("priority", Priority.ToString());

            //Verify proper severity
            if (Severity < 0 || Severity > 5)
                throw new ArgumentException("Severity must be between 1 and 5 (or 0 for blank)");
            else if (Severity == 0)
                attrValues.Add("severity", "");
            else
                attrValues.Add("severity", Severity.ToString());

            //Verify proper urgency
            if (Urgency < 0 || Urgency > 4)
                throw new ArgumentException("Urgency must be between 0 and 4");
            else
                attrValues.Add("urgency", Urgency.ToString());

            //Verify proper impact
            if (Impact < 0 || Impact > 5)
                throw new ArgumentException("Severity must be between 0 and 5");
            else
                attrValues.Add("impact", Impact.ToString());

            // ------------------Create ticket portion --------------------

            String csv = String.Join(
                ",",
                attrValues.Select(d => d.Key + "," + d.Value));

            string requestHandle = "";
            string requestNumber = "";

            rawXml = ws_client.createRequest(SID, creatorUser.Handle, csv, new string[0], "", new string[] { "persistent_id" }, ref requestHandle, ref requestNumber);
            ReturnedXml = XDocument.Parse(rawXml);

            return Int32.Parse(requestNumber);
        }
    }

}
