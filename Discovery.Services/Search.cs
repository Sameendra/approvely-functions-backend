using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Discovery.Services
{
    [StorageAccount("AzureWebJobsStorage")]
    public static class Search
    {
        [FunctionName("Search")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req, [Blob("functiondata/Slots.json", FileAccess.Read, Connection = "AzureWebJobsStorage")] string slotsconfig, [Blob("functiondata/Defaults.json", FileAccess.Read, Connection = "AzureWebJobsStorage")] string defaultconfig, TraceWriter log)
        {

            JObject defaults;
            JObject slots;
            JObject manifest = new JObject();

            if (defaultconfig != null)
            {
                defaults = JObject.Parse(defaultconfig);
            }
            else
            {
                defaults = new JObject();
            }
            if(slotsconfig != null){
                slots = JObject.Parse(slotsconfig);
            }
            else
            {
                slots = new JObject();
            }

            manifest.Merge(defaults);

            // parse query parameter
            string ver = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "ver", true) == 0)
                .Value;

            bool debug = false;
            try
            {
                debug = Convert.ToBoolean(req.GetQueryNameValuePairs()
                    .FirstOrDefault(q => string.Compare(q.Key, "debug", true) == 0)
                    .Value);
            }
            catch (FormatException) { }

            slots = GetSlotOrDefault(version: ver, debug: debug, json: slots);
            manifest.Merge(slots, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });

            log.Info(manifest.ToString());

            return req.CreateResponse(HttpStatusCode.OK, manifest, "application/json");

        }


        public static JObject GetSlotOrDefault(string version, bool debug, JObject json)
        {

            IEnumerable<JToken> apislots = json[SlotsJson.ApiSlotsOrdinal].ToList();

            if (!string.IsNullOrWhiteSpace(version))
            {
                apislots = apislots.Where(slot => slot[SlotsJson.VersionOrdinal] != null && (string)slot[SlotsJson.VersionOrdinal] == version);
            }
            if (debug)
            {
                apislots = apislots.Where(slot => slot[SlotsJson.IsInDebugModeOrdinal] != null && (bool)slot[SlotsJson.IsInDebugModeOrdinal] == true);
            }

            JToken apislot;
            if (apislots.Count() == 1)
            {
                apislot = apislots.First();
            }
            else
            {
                apislot = json[SlotsJson.ApiSlotsOrdinal].First(slot => slot[SlotsJson.IsDefault] != null && (bool)slot[SlotsJson.IsDefault] == true);
            }

            return (JObject)apislot;

        }


        private static class SlotsJson
        {
            public const string ApiSlotsOrdinal = "apislots";
            public const string VersionOrdinal = "version";
            public const string IsInDebugModeOrdinal = "isindebugmode";
            public const string IsDefault = "isdefault";
        }
    }
}