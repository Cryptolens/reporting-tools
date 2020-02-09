using System;
using System.Collections.Generic;
using SKM.V3;
using SKM.V3.Methods;
using SKM.V3.Models;

// include this since GetWebAPILog is not yet available in v4021 (coming in v4022)
using SKM.V3.Internal;

using System.Linq;


namespace RetrieveAuditLog
{
    class Program
    {
        static void Main(string[] args)
        {
            // This token should have GetKey and GetWebAPILog permission.
            string token = "";

            // We need to set 3 things: the product (5693), the license, the floating time interval(100) and the token.
            var res = GetAuditLog(5693, "FVNZH-QDRNW-OVJYL-TKIHN", 100, token);

            Console.WriteLine("Max number of machines: " + res.MaxNoOfMachines);
            Console.WriteLine($"At this point, {res.MaxNoOfMachines - res.UsedFloatingLicenses} seats are available.");

            foreach (var log in res.Logs)
            {
                Console.WriteLine(log.Time + "\t" + log.Action + "\t" + log.FreeSeatsRemaining  + "\t" + log.MachineId);
            }

            Console.ReadLine();
        }

        static AuditLog GetAuditLog(int productId, string licenseKey, int floatingTimeInteral, string token)
        {
            var auditLog = new AuditLog();

            // 1. Getting the LicenseKey (for max no of machines)
            var res = Key.GetKey(token, new KeyInfoModel { ProductId = productId, Key = licenseKey, Metadata = true });
            auditLog.MaxNoOfMachines = res.LicenseKey.MaxNoOfMachines;

            // 2. Retrieving the logs associated with the license key. This will be available in the next release tomorrow.
            // For now, a diffent version is used.

            var rawLogs = new List<WebAPILog>();
            long pointer = 0;

            while (true)
            {
                var res2 = HelperMethods.SendRequestToWebAPI3<GetWebAPILogResult>(new GetEventsModel 
                {
                    Limit = 1000,
                    ProductId = productId, 
                    Key = licenseKey, 
                    StartingAfter = pointer, 
                    LicenseServerUrl = "https://app.cryptolens.io/"
                }, "/ai/getwebapilog/", token);

                if(res2.Logs.Count == 0)
                {
                    break;
                }

                rawLogs.AddRange(res2.Logs);
                pointer = res2.Logs.Last().Id;
            }

            var preprocesedLogs = new List<Activity>();

            var floatingLicenses = new Dictionary<string, long>();

            foreach (var webAPIEntry in rawLogs)
            {
                // we don't want to include activations/deactivations that were not successful.
                if(webAPIEntry.State % 100 / 10 == 2) { continue; }

                // ignore the node-locked case
                if(webAPIEntry.State == 2010 || webAPIEntry.State == 2011 || webAPIEntry.State == 2012 || webAPIEntry.State == 2013) { continue; }

                string action = "";
                if(webAPIEntry.State / 1000 == 2) 
                {
                    action = "Activation";

                    if(webAPIEntry.State == 2014 || webAPIEntry.State == 2015)
                    {
                        // floating
                        if (floatingLicenses.ContainsKey(webAPIEntry.MachineCode))
                        {
                            floatingLicenses[webAPIEntry.MachineCode] = webAPIEntry.Time + floatingTimeInteral;
                            action = "Verification";
                        }
                        else
                        {
                            floatingLicenses.Add(webAPIEntry.MachineCode, webAPIEntry.Time + floatingTimeInteral);
                        }
                    }
                }
                else if (webAPIEntry.State / 1000 == 6)
                {
                    action = "Deactivation";

                    if (webAPIEntry.State == 6011)
                    {
                        //release of floating
                        if (floatingLicenses.ContainsKey(webAPIEntry.MachineCode))
                        {
                            floatingLicenses.Remove(webAPIEntry.MachineCode);
                            //floatingLicenses[webAPIEntry.MachineCode] = 0;
                        }
                    }
                }
                else
                {
                    // we are not interested in other events.
                    continue;
                }

                int activeFloatingSeats = floatingLicenses.Where(x => x.Value > webAPIEntry.Time).Count();

                preprocesedLogs.Add(new Activity { Time = DateTimeOffset.FromUnixTimeSeconds(webAPIEntry.Time).DateTime, Action = action, MachineId = webAPIEntry.MachineCode, FreeSeatsRemaining = auditLog.MaxNoOfMachines - activeFloatingSeats });

                // now, let's check if some devices are no longer used.
                var inactiveFloatingSeats = floatingLicenses.Where(x => x.Value <= webAPIEntry.Time && x.Value > 0);

                foreach (var inactiveDevice in inactiveFloatingSeats)
                {
                    preprocesedLogs.Add(new Activity { Time = DateTimeOffset.FromUnixTimeSeconds(inactiveDevice.Value).DateTime, Action = "Deactivation", MachineId = inactiveDevice.Key, FreeSeatsRemaining = int.MinValue/*auditLog.MaxNoOfMachines - activeFloatingSeats*/ });
                    floatingLicenses.Remove(inactiveDevice.Key);
                }
            }

            var timeNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // one final time, let's check if some devices are no longer used.
            var inactiveFloatingSeatsFinal = floatingLicenses.Where(x => x.Value <= timeNow && x.Value > 0);

            foreach (var inactiveDevice in inactiveFloatingSeatsFinal)
            {
                preprocesedLogs.Add(new Activity { Time = DateTimeOffset.FromUnixTimeSeconds(inactiveDevice.Value).DateTime, Action = "Deactivation", MachineId = inactiveDevice.Key, FreeSeatsRemaining = int.MinValue/*auditLog.MaxNoOfMachines - activeFloatingSeats*/ });
                floatingLicenses.Remove(inactiveDevice.Key);
            }
            preprocesedLogs = preprocesedLogs.OrderBy(x=> x.Time).ToList();

            // fix the number of active seats for licenses that were automatically deactivated.
            for (int i = 0; i < preprocesedLogs.Count; i++)
            {
                if(preprocesedLogs[i].FreeSeatsRemaining == int.MinValue)
                {
                    // do Min just in case floating overdraft was used.
                    preprocesedLogs[i].FreeSeatsRemaining = Math.Min(preprocesedLogs[i - 1].FreeSeatsRemaining + 1, auditLog.MaxNoOfMachines);
                }
            }

            auditLog.Logs = preprocesedLogs;
            auditLog.UsedFloatingLicenses = floatingLicenses.Where(x => x.Value > timeNow).Count();

            return auditLog;
        }

    }

    public class AuditLog
    {
        public int MaxNoOfMachines { get; set; }
        public List<Activity> Logs { get; set; }
        //public int NodeLockedDevices { get; set; }
        public int UsedFloatingLicenses { get; set; }

    }

    public class Activity
    {
        public DateTime Time { get; set; }
        public string Action { get; set; }
        public string MachineId { get; set; }

        public int FreeSeatsRemaining { get; set; }
    }

    public class GetWebAPILogResult : BasicResult
    {
        public List<WebAPILog> Logs { get; set; }
    }

    public class WebAPILog
    {
        public long Id { get; set; }
        public int ProductId { get; set; }
        public string Key { get; set; }
        public string IP { get; set; }
        public long Time { get; set; }
        public short State { get; set; }
        public string MachineCode { get; set; }
    }

    public class GetEventsModel : RequestModel
    {
        public int Limit { get; set; }
        public long StartingAfter { get; set; }
        public int ProductId { get; set; }
        public string Key { get; set; }
    }
    public class KeyInfoModelV2 : RequestModel
    {
        public int ProductId { get; set; }
        public string Key { get; set; }
        public bool Sign { get; set; }
        public SignMethod SignMethod { get; set; }
        public bool Metadata { get; set; }
        public int FloatingTimeInterval { get; set; }
        public int FieldsToReturn { get; set; }
    }
}
