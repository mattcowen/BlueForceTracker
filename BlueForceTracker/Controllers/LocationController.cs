using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Client;
using BlueForceTracker.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.FileProviders;

namespace BlueForceTracker.Controllers
{
    [Route("api/[controller]")]
    public class LocationController : Controller
    {
        private readonly IHubContext<Locator> _hubContext;

        public static IConfiguration Configuration { get; set; }

        private readonly IFileProvider _files;

        public LocationController(IHubContext<Locator> hubContext, IConfiguration configuration, IFileProvider files)
        {
            _hubContext = hubContext;
            Configuration = configuration;
            _files = files;
        }

        // api/location/5101/00012
        [HttpGet("{id}/{lat}/{lon}", Name = "GetLocation")]
        public async Task<ObjectResult> SendLocation(string id, string lat, string lon)
        {
            var item = new { Pilot = id, Latitude = lat, Longitude = lon };
            await _hubContext.Clients.All.InvokeAsync("Send", $"{item.Pilot}&{item.Latitude}&{item.Longitude}");

            return new ObjectResult(item);
        }

        // api/track/SteelG-2017-07-05
        [HttpGet("/api/track/{trackId}")]
        public async Task<IActionResult> SendTrackLog(string trackId)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Configuration["Values:myConnection"]);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("bft");

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(trackId + ".igc");

            //var fileInfo = _files.GetFileInfo("wwwroot/tracklogs/" + trackId + ".igc");
            
            // Create or overwrite the "myblob" blob with contents from a local file.
            await blockBlob.UploadFromFileAsync($"wwwroot/tracklogs/{trackId}.igc");

            return new ObjectResult(trackId);
        }
    }
}