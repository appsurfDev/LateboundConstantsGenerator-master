﻿using CommandLine;
using System;

namespace Rappen.XTB.LCG.Cmd
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(o =>
                {
                    Console.WriteLine($"********************** Authorized By Appsurfs {DateTime.Now.Year} Author: Ian Mok **********************");

                    var lcgHelper = new LCGHelper();
                    lcgHelper.LoadSettingsFromFile(o.SettingsFilePath);

                    if (o.ConnectionString != null)
                    {
                        lcgHelper.ConnectCrm(o.ConnectionString);
                    }
                    else
                    {
                        var credentials = new CrmCredentials
                        {
                            Domain = o.Domain,
                            OrgUnit = o.OrgUnit,
                            ServerName = o.ServerName,
                            Password = o.Password,
                            Protocol = o.Protocol,
                            User = o.User
                        };

                        lcgHelper.ConnectCrm(credentials);
                    }

                    lcgHelper.GenerateConstants();
                });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
