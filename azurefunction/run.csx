#r "Newtonsoft.Json"

using System;
using Newtonsoft.Json;

public static void Run(string myIoTHubMessage, ILogger log, out object outputTelemetry)
{
    // Put out some log messages to verify the data comes in correctly.
    log.LogInformation($"C# IoT Hub trigger function processed a message: {myIoTHubMessage}");

    // Convert the input string received from the IoT Hub into a JSON object.
    var telemetryObject = JsonConvert.DeserializeObject < TelemetryData > (myIoTHubMessage);

    // Send the converted JSON object directly to the database defiend as the output of the function.
    outputTelemetry = telemetryObject;
}

// Define a nice object for the telemetry data received by the device.
public class TelemetryData
{
    public string device { get; set; }
    public string appversion { get; set; }
    public string readingtime { get; set; }
    public string temperature { get; set; }
    public string humidity { get; set; }
    public string brightness { get; set; }
    public string loudness { get; set; }
}