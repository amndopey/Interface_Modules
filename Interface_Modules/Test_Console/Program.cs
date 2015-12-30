﻿using System;
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
            int sid_token = CA_SDM.Get_SIDToken("or0210312", "Cj8mt1d!");
            SDM_Contact test = CA_SDM.Find_Contact(sid_token, "or0210312");

            Console.WriteLine(test.Name);

            Console.ReadLine();

            CA_SDM.Remove_SIDToken(sid_token);
        }
    }
}