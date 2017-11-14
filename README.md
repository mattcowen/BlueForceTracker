# BlueForceTracker

<p>Clicking the Add Track button copies a paragliding track log to Azure Blob Storage.</p>
<p>The new blob triggers an Azure Function that simulates the flight by reading the file and sending coordinates to an API method on the website.</p> 
<p>The API call updates the map using WebSockets.</p>

<p>Shows how to use the new SignalR for ASP.NET Core 2.</p>

<p>Update the appsettings.json with your Bing Maps key and Azure storage connection string</p>

<p><a href="http://www.mattcowen.co.uk/single-post/2017/11/14/SignalR-in-ASPNET-Core-2">Read more here</a>