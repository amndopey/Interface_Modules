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

        public static SDM_Contact Find_Contact (int SID, string UserID = "", string Last_Name = "", string First_Name = "", string Email = "", string Access_Type = "", bool Only_Active = true)
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
            SDM_Contact results = new SDM_Contact();
            foreach (var contact in ReturnedXml.Descendants("UDSObject"))
            {
                results.Handle = contact.Element("Handle").Value;

                foreach (var attribute in contact.Descendants("Attribute"))
                {
                    if (attribute.Element("AttrName").Value == "first_name")
                        results.First_Name = attribute.Element("AttrValue").Value;
                    if (attribute.Element("AttrName").Value == "last_name")
                        results.Last_Name = attribute.Element("AttrValue").Value;
                }
            }

            return results;
        }
    }

    public class SDM_Contact
    {
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Email { get; set; }
        public string Handle { get; set; }
    }
}
