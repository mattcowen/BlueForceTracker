using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Text;
using System;
using System.Net.Http;
using System.Globalization;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace GpsTrackSpoofer
{
    public class TrackLog
    {
        public TimeSpan Time { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

    }

    
    public static class GpsTrackSpoofer
    {

        [FunctionName("GpsTrackSpoofer")]
        public static void Run([BlobTrigger("bft/{name}", Connection = "myConnection")]Stream myBlob, string name, TraceWriter log)
        {
            log.Warning($"C# Blob trigger function processing blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            // stream the file from storage
            var lines = ReadLines(() => myBlob, Encoding.UTF8).ToList();

            // belt and braces to ensure we have the file
            if (!lines.Any())
            {
                log.Error($"Blob {name} could not be found");
                return;
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(ConfigurationManager.AppSettings["WebsiteUrl"]);
            TimeSpan waitTime = TimeSpan.Zero;

            string pilot = "<unknown>";
            
            // for each line extract and send the coordinates to the api
            foreach(var line in lines)
            {
                // pilot as id
                if (line.StartsWith("HFPLTPILOT"))
                    pilot = line.Substring("HFPLTPILOT:".Length);

                // actual track data
                if (line.StartsWith("B"))
                {
                    var locationData = ParseLocation(line);

                    if(waitTime != TimeSpan.Zero)
                    {
                        var milliseconds = locationData.Time.Subtract(waitTime).TotalMilliseconds;
                        Thread.Sleep((int)milliseconds);
                    }

                    waitTime = locationData.Time;

                    SendLocation(client, pilot, locationData.Latitude, locationData.Longitude, log);
                }
            }

            

        }


        private static async void SendLocation(HttpClient client, string pilot, string latitude, string longitude, TraceWriter log)
        {
            var uri = Uri.EscapeUriString($"{pilot}/{latitude}/{longitude}");
            try
            {
                HttpResponseMessage response = client.GetAsync($"/api/location/{uri}", HttpCompletionOption.ResponseHeadersRead).Result;
                if (!response.IsSuccessStatusCode)
                {
                    log.Error($"Track log not sent. Status {response.StatusCode}");
                }
                else
                {
                    //log.Warning($"Tracking {pilot} {latitude} {longitude}");
                }
                response.Dispose();
            }
            catch (Exception ex)
            {
                log.Error($"Comms exception. Status {ex.StackTrace}");
            }
            
        }

        //B1101355206343N00006198WA0058700558
        //B: record type is a basic tracklog record
        //110135: <time> tracklog entry was recorded at 11:01:35 i.e.just after 11am
        //5206343N: <lat> i.e. 52 degrees 06.343 minutes North
        //00006198W: <long> i.e. 000 degrees 06.198 minutes West
        //A: <alt valid flag> confirming this record has a valid altitude value
        //00587: <altitude from pressure sensor>
        //00558: <altitude from GPS>
        private static TrackLog ParseLocation(string tracklog)
        {
            var eHemi = tracklog.IndexOf("N") > -1 ? 'N' : 'S';
            var mHemi = tracklog.IndexOf("W") > -1 ? 'W' : 'E';

            var latIx = tracklog.IndexOf(eHemi);
            var lonIx = tracklog.IndexOf(mHemi);
            TrackLog t = new TrackLog
            {
                Time = TimeSpan.ParseExact(tracklog.Substring(1, 6), "hhmmss", CultureInfo.CurrentCulture),
                Latitude = ConvertToNumericCoordinate(tracklog.Substring(7, latIx - 7), eHemi),
                Longitude = ConvertToNumericCoordinate(tracklog.Substring(latIx + 1, lonIx - 15), mHemi) 
            };

            return t;
        }

        private static string ConvertToNumericCoordinate(string coordinate, char compassBearing)
        {
            Decimal coord = Decimal.Parse(coordinate) / 100000;

            if (compassBearing == 'W' || compassBearing == 'S')
                return Decimal.Negate(coord).ToString();

            return coord.ToString();
        }

        private static IEnumerable<string> ReadLines(Func<Stream> streamProvider,
                                     Encoding encoding)
        {
            using (var stream = streamProvider())
            using (var reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }


    }
}
