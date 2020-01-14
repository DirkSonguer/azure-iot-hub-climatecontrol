//---------------------------------------------------------------------------//
// DeviceApplication - An Azure IoT Hub example application for IoT devices
// link: https://github.com/DirkSonguer/azure-iot-hub-climatecontrol
// authors: Dirk Songuer
//
// You should have received a copy of the MIT License
// along with this program called LICENSE.md
// If not, see <https://choosealicense.com/licenses/mit/>
//---------------------------------------------------------------------------//
// This application is meant as an example how to connect and interact with
// Azure IoT Hub. You can find step-by-step instructions how to set up
// the Azure side of things on the project page.
//
// This app wants to showcase a number of things:
// - How to establish a connection from an IoT device to Azure IoT Hub
// - How to update the digital twin telemetry data (device to cloud, d2c)
// - How to receive a direct method from the IoT Hub (cloud to device, c2d)
// - How to send generic event data (device to cloud, d2c)
//
// For more information what the application does, check the documentation here:
// https://github.com/DirkSonguer/azure-iot-hub-climatecontrol/blob/master/documentation/6-device-application.md
//
// Note that the application architecture and code is not necessarily the
// most efficient - it's optimized for readability and to be easy to follow.
// For more information about the communication concepts, see:
// - https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-d2c-guidance
// - https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-c2d-guidance
//---------------------------------------------------------------------------//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using System.Threading;
using System.Threading.Tasks;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.ApplicationModel;

// This package is required to access provisioned data managed by the TPM.
// Include this in your own project via:
// Project -> Manage NuGet Packages -> Microsoft.Devices.Tpm
using Microsoft.Devices.Tpm;

// This package is required to handle the communication between the application and Azure.
// Include this in your own project via:
// Project -> Manage NuGet Packages -> Microsoft.Azure.Devices.Client
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

// This package is required to access the GrovePi shield and attached sensors.
// Include this in your own project via:
// Project -> Manage NuGet Packages -> GrovePi
using GrovePi;
using GrovePi.Sensors;

// This package is required to easily create / use JSON objects.
// Include this in your own project via:
// Project -> Manage NuGet Packages -> Newtonsoft.Json
using Newtonsoft.Json;

namespace DeviceApplication
{
    // This is is a structure that is used to store all telemetry data into.
    public class TelemetryDataObject
    {
        public string device { get; set; }
        public string appversion { get; set; }
        public double temperature { get; set; }
        public double humidity { get; set; }
        public int brightness { get; set; }
        public int loudness { get; set; }
        public List<int> soundbuffer = new List<int>();
        public string readingtime { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        // Current version number as string.
        private string appVersion;

        // Add sensors attached to the board.
        ILightSensor sensorLight;
        IDHTTemperatureAndHumiditySensor sensorTemperatureHumidity;
        ISoundSensor sensorSound;

        // TPM connection / auth values.
        private string tpmHubUri;
        private string tpmDeviceId;
        private string tpmSasToken;

        // The telemetry data object.
        // All different threads will write into this object to have one
        // central instance of truth.
        TelemetryDataObject telemetryData = new TelemetryDataObject();

        // This is similar to the telemetry data object.
        // It will hold the temeletry data exchanged with Azure IoT Hub
        // as digital twin data. While the entire telemetry data is used
        // within the messages, the digital twin communication needs
        // to contain only the ones that changed from time to time.
        static TwinCollection digitalTwinProperties = new TwinCollection();

        // The connection to Azure IoT Hub.
        static DeviceClient deviceClient = null;

        // We are using DispatcherTimer because ThreadPoolTimer
        // is not neccesarily reliable under Windows 10 IoT Core together
        // with some boards (example: Raspberry Pi 3B).
        // DispatcherTimer is a bit more effort to set up, however they
        // work reliably with every board.
        private DispatcherTimer timerProcessTemperatureHumidity;
        const int _timerProcessTemperatureHumidityIntervalInMS = 1000;

        private DispatcherTimer timerProcessBrightness;
        const int _timerProcessBrightnessIntervalInMS = 1000;

        private DispatcherTimer timerProcessLoudness;
        const int _timerProcessLoudnessIntervalInMS = 100;

        private DispatcherTimer timerUpdateUI;
        const int _timerUpdateUIIntervalInMS = 1000;

        // This is the default timer to send updates to the Azure IoT Hub.
        // Note that the value can be changed via direct method during runtime,
        // so this is not a constant.
        private DispatcherTimer timerSendTelemetryData;
        static int timerSendTelemetryDataIntervalInMS = 30000;

        // Flags to block running specific tasks multiple times at once.
        bool tempMeasurementInProgress = false;
        bool dataSendInProgress = false;

        public MainPage()
        {
            this.InitializeComponent();

            // Confirm that application has indeed started and can write to the UI.
            uiApplicationOut.Text = "Application started at " + DateTime.Now;

            // Get current application package version.
            // This will be included in the device twin data to validate which version of the software
            // the devices are running within the Azure Portal.
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;
            appVersion = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            uiAppVersionOut.Text += " - " + appVersion;

            // Read out Azure connection data from the TPM module.
            // These are essentially the connection credentials to connect to the Azure IoT Hub.
            // If we are not able to get them (i.e. the device is not provisioned ),
            // then we can essentially quit the application.
            if (!ReadTpmValuesFromDevice())
            {
                uiApplicationOut.Text += "\nCould not read out Azure IoT Hub authentication data from TPM module.";
                uiApplicationOut.Text += "\nStopping.";
                // Application.Current.Exit();
                return;
            }
            else
            {
                uiApplicationOut.Text += "\non device " + tpmDeviceId;
            }

            // Initialze all sensors on the device.
            // If there is an issue with one of the sensors, then we can also quit.
            if (!InitializeSensors())
            {
                uiApplicationOut.Text += "\nCould not initialize the sensors on the device.";
                uiApplicationOut.Text += "\nStopping.";
                // Application.Current.Exit();
                return;
            }

            // Create the timer to read out the light sensor.
            timerProcessBrightness = new DispatcherTimer();
            timerProcessBrightness.Tick += ProcessBrightness;
            timerProcessBrightness.Interval = TimeSpan.FromMilliseconds(_timerProcessBrightnessIntervalInMS);
            timerProcessBrightness.Start();

            // Create the timer to read out the sound sensor.
            timerProcessLoudness = new DispatcherTimer();
            timerProcessLoudness.Tick += ProcessLoudness;
            timerProcessLoudness.Interval = TimeSpan.FromMilliseconds(_timerProcessLoudnessIntervalInMS);
            timerProcessLoudness.Start();

            // Create the timer to read out the temperature and humidity sensor.
            timerProcessTemperatureHumidity = new DispatcherTimer();
            timerProcessTemperatureHumidity.Tick += ProcessTemperatureHumidity;
            timerProcessTemperatureHumidity.Interval = TimeSpan.FromMilliseconds(_timerProcessTemperatureHumidityIntervalInMS);
            timerProcessTemperatureHumidity.Start();

            // Create the timer to update the UI.
            timerUpdateUI = new DispatcherTimer();
            timerUpdateUI.Tick += UpdateUi;
            timerProcessBrightness.Interval = TimeSpan.FromMilliseconds(_timerUpdateUIIntervalInMS);
            timerUpdateUI.Start();

            // Establish the connection to the Azure IoT Hub.
            EstablishIoTHubConnection();

            // Create the timer to send data to the Hub.
            timerSendTelemetryData = new DispatcherTimer();
            timerSendTelemetryData.Tick += SendTelemetryData;
            timerSendTelemetryData.Interval = TimeSpan.FromMilliseconds(timerSendTelemetryDataIntervalInMS);
            timerSendTelemetryData.Start();
        }


        /// <summary>
        /// Read out the connection data stored in the TPM module
        /// and store them in the application variables for later use.
        /// </summary>
        /// <returns>Returns a bool to indicate if the data was found.</returns>
        private bool ReadTpmValuesFromDevice()
        {
            // Use logical device 0 on the TPM by default.
            TpmDevice myDevice = new TpmDevice(0);

            // Read out relevant connection data.
            tpmHubUri = myDevice.GetHostName();
            tpmDeviceId = myDevice.GetDeviceId();
            tpmSasToken = myDevice.GetSASToken();

            // If either the device ID or sas token are empty, return false.
            if ((tpmDeviceId == "") || (tpmSasToken == ""))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialize all the sensors attached to the board.
        /// </summary>
        /// <returns>void</returns>
        private async void EstablishIoTHubConnection()
        {
            // To establish a connection, we need to make sure to read out current TPM values.
            // This needs to be done every time a connection should be established as the SAS tokens
            // will eventually expire. In this case you will get a
            // Microsoft.Azure.Devices.Client.Exceptions.UnauthorizedException
            // exception when sending data.
            // In this case, request a new SAS token and connect again.
            if (!ReadTpmValuesFromDevice())
            {
                uiApplicationOut.Text += "\nCould not read out Azure IoT Hub authentication data from TPM module.";
                //return false;
                return;
            }

            try
            {
                // This will create a connection from the device to the Azure IoT Hub.
                // The connection is created with the SAS token.
                // The chosen transport protocol is MQTT, see http://mqtt.org/.
                uiApplicationOut.Text += "\nTrying to connect to Azure IoT Hub";
                deviceClient = DeviceClient.Create(tpmHubUri, AuthenticationMethodFactory.CreateAuthenticationWithToken(tpmDeviceId, tpmSasToken), TransportType.Mqtt);

                // This registers a direct method that can be called from the Azure IoT Hub
                // (cloud to device communication).
                // When such a "SetTelemetryInterval" method call is received, the respective function
                // is called "IoTHubMethodSetTelemetryInterval".
                uiApplicationOut.Text += "\nTrying to register new method";
                await deviceClient.SetMethodHandlerAsync("SetTelemetryInterval", IoTHubMethodSetTelemetryInterval, null);
            }
            catch (Exception ex)
            {
                uiApplicationOut.Text += "\nCould not connect to Azure IoT Hub: " + ex.ToString();
                //return false;
                return;
            }

            uiApplicationOut.Text += "\nConnection ok.";
            return;
        }


        /// <summary>
        /// Handle the "SetTelemetryInterval" method call from Azure IoT Hub.
        /// As a result, the timerSendTelemetryDataIntervalInMS is changed and the
        /// application sends the telemtry data accoridng to the new interval.
        /// Also, the device twin data is updated.
        /// </summary>
        /// <param name="methodRequest">The request object sent from the cloud</param>
        /// <param name="userContext">User context, usually the connection between cloud and device</param>
        /// <returns>void</returns>
        private Task<MethodResponse> IoTHubMethodSetTelemetryInterval(MethodRequest methodRequest, object userContext)
        {
            // Read the payload, which should contain the new requested interval.
            var payloadData = Encoding.UTF8.GetString(methodRequest.Data);

            // Try and convert the payload (string) to an integer.
            // If this fails, then the payload probably wasn't a number.
            int newIntervalInMS = 0;
            if (!Int32.TryParse(payloadData, out newIntervalInMS))
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    uiApplicationOut.Text += "\nDirect method \"SetTelemetryInterval\" received, but with an invalid parameter: " + payloadData;
                });

                // Acknowlege the direct method call with a 400 error message
                // and add a meaningful error message.
                string result = "{\"result\":\"SetTelemetryInterval: Invalid parameter given (should be a number between 5000 and 60000)\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }

            // Check if the requested interval is within sensible boundaries
            // (from 5 seconds to 1 minute).
            if ((newIntervalInMS < 5000) || (newIntervalInMS > 60000))
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    uiApplicationOut.Text += "\nDirect method \"SetTelemetryInterval\" received, but with an invalid parameter: " + payloadData;
                });

                // Acknowlege the direct method call with a 400 error message
                // and add a meaningful error message.
                string result = "{\"result\":\"SetTelemetryInterval: Invalid interval given (should be a number between 5000 and 60000)\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }

            // Check that the payload is indeed an integer value.
            try
            {
                // Set the new interval.
                timerSendTelemetryDataIntervalInMS = newIntervalInMS;

                // Update the UI as well as change the time span for the timer running
                // the telemetry updates to the new interval.
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                  {
                      uiApplicationOut.Text += "\nDirect method \"SetTelemetryInterval\" received, telemetry interval set to " + payloadData + " ms";
                      timerSendTelemetryData.Interval = TimeSpan.FromMilliseconds(timerSendTelemetryDataIntervalInMS);
                  });

                // Send an update to the device twin data with the new interval time.
                digitalTwinProperties = new TwinCollection();
                digitalTwinProperties["appVersion"] = appVersion;
                digitalTwinProperties["dataIntervalInMS"] = timerSendTelemetryDataIntervalInMS;
                deviceClient.UpdateReportedPropertiesAsync(digitalTwinProperties).Wait();

                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"SetTelemetryInterval: Successfully changed the interval\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
            }
            catch (Exception ex)
            {
                _ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    uiApplicationOut.Text += "\nDirect method \"SetTelemetryInterval\" received, but an error occured: " + ex.ToString();
                });

                // Acknowlege the direct method call with a 400 error message
                string result = "{\"result\":\"SetTelemetryInterval: An error occured when executing the method\"}";
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 400));
            }
        }


        /// <summary>
        /// Initialize all the sensors attached to the board.
        /// </summary>
        /// <returns>Returns a bool to indicate if all sensors were found.</returns>
        private bool InitializeSensors()
        {
            sensorTemperatureHumidity = DeviceFactory.Build.DHTTemperatureAndHumiditySensor(Pin.DigitalPin4, DHTModel.Dht11);
            sensorLight = DeviceFactory.Build.LightSensor(Pin.AnalogPin1);
            sensorSound = DeviceFactory.Build.SoundSensor(Pin.AnalogPin2);

            return true;
        }


        /// <summary>
        /// Read the temperature and humidity from the sensor and do all the intermediate calculations,
        /// like smoothing data.
        /// This is called every _timerProcessTemperatureHumidityIntervalInMS by timerProcessTemperatureHumidity.
        /// </summary>
        /// <param name="sender">Sender information as sent by the timer logic</param>
        /// <param name="e">Additional information as sent by the timer logic</param>
        /// <returns>void</returns>
        private void ProcessTemperatureHumidity(object sender, object e)
        {
            // Measuring data for the temperature and humidity sensor.
            // While it's supposed to wait until the data has been collected, the
            // async / await for Measure() seems to have issues with Grove on a
            // Rapsberry Pi: The sensor will come back whenever.
            // Also, sometimes the sensor will return NaN or 0 instead of proper
            // values when asked for either temperature or humidity.
            // This happens if calling for a measurement multiple times, for example if one
            // measurement takes very long and another is triggered.
            // To avoid that we make sure we only measure once.
            if (tempMeasurementInProgress) return;
            tempMeasurementInProgress = true;

            try
            {
                sensorTemperatureHumidity.Measure();

                telemetryData.temperature = Math.Round(sensorTemperatureHumidity.TemperatureInCelsius, 2);
                telemetryData.humidity = sensorTemperatureHumidity.Humidity;

                // Check if we got NaN instead of proper values when asked for either temperature or humidity.
                if ((Double.IsNaN(sensorTemperatureHumidity.TemperatureInCelsius)) || (sensorTemperatureHumidity.TemperatureInCelsius == 0))
                {
                    throw new System.ArgumentException("Could not poll sensor", "ProcessTemperatureHumidity");
                }
            }
            catch (Exception ex)
            {
                // NOTE: There are frequent exceptions of the following:
                // ** WinRT information: Unexpected number of bytes was transferred. Expected: '. Actual: ' **.
                // This appears to be caused by the rapid frequency of writes to the GPIO.
                // It can also happen if the sensor does not return any sensible values on Measure(),
                // see above.
                // These are being swallowed here.

                // If you want to see the exceptions, uncomment the following:
                // uiApplicationOut.Text += "\nProcessTemperatureHumidity exception: " + ex.ToString();
            }

            // Release flag again.
            tempMeasurementInProgress = false;
        }


        /// <summary>
        /// Read the loudness from the sound sensor and do all the intermediate calculations,
        /// like smoothing data.
        /// This is called every _timerProcessLoudnessIntervalInMS by timerProcessLoudness.
        /// </summary>
        /// <param name="sender">Sender information as sent by the timer logic</param>
        /// <param name="e">Additional information as sent by the timer logic</param>
        /// <returns>void</returns>
        private void ProcessLoudness(object sender, object e)
        {
            // Measuring the loudness in an environment is tricky since "loudness"
            // is something subjective. If a sharp noise happens once every 30 seconds,
            // is it overall loud or just annoying?
            // As a result we take multiple measurements during the reporting interval
            // and average out the readings over time before sending.
            var tempSoundADCLevel = sensorSound.SensorValue();
            if (tempSoundADCLevel > 512)
            {
                telemetryData.loudness = tempSoundADCLevel;
                telemetryData.soundbuffer.Add(tempSoundADCLevel);
            }
        }


        /// <summary>
        /// Read the brightness from the light sensor and do all the intermediate calculations,
        /// like smoothing data.
        /// This is called every _timerProcessBrightnessIntervalInMS by timerProcessBrightness.
        /// </summary>
        /// <param name="sender">Sender information as sent by the timer logic</param>
        /// <param name="e">Additional information as sent by the timer logic</param>
        /// <returns>void</returns>
        private void ProcessBrightness(object sender, object e)
        {
            var tempBrightess = sensorLight.SensorValue();
            if (telemetryData.brightness < tempBrightess)
            {
                telemetryData.brightness = tempBrightess;
            }
        }


        /// <summary>
        /// Update the UI with updated values.
        /// This is called every _timerUpdateUIIntervalInMS by timerUpdateUI.
        /// </summary>
        /// <returns>void</returns>
        private void UpdateUi(object sender, object e)
        {
            telemetryData.readingtime = DateTime.Now.ToString();
            uiCurrentTimeOut.Text = telemetryData.readingtime;
            uiCurrentTemperatureOut.Text = telemetryData.temperature.ToString();
            uiCurrentHumidityOut.Text = telemetryData.humidity.ToString();
            uiCurrentLoudnessOut.Text = telemetryData.loudness.ToString();
            uiCurrentBrightnessOut.Text = telemetryData.brightness.ToString();
        }


        /// <summary>
        /// Takes the data from the sensors and sends it to the Azure IoT Hub event.
        /// This is called every timerSendTelemetryDataIntervalInMS by timerSendTelemetryData.
        /// </summary>
        /// <returns>void</returns>
        private async void SendTelemetryData(object sender, object e)
        {
            if (dataSendInProgress) return;
            dataSendInProgress = true;

            timerProcessBrightness.Stop();
            timerProcessLoudness.Stop();
            timerProcessTemperatureHumidity.Stop();

            // Get the average of all values in the current sound buffer.
            // See ProcessLoudness() for more information.
            int soundBufferAverage = 0;
            if (telemetryData.soundbuffer.Count > 0)
            {
                soundBufferAverage = Convert.ToInt32(telemetryData.soundbuffer.Average());
                telemetryData.soundbuffer.Clear();
            }

            try
            {
                // Reset the data transfer output component.
                uiDataTransferOut.Text = "Preparing to send data";

                // Update the telemetry data point with the current telemetry.
                uiDataTransferOut.Text += "\nCreate a new object with the measured sensor data.";
                var telemetryDataPoint = new
                {
                    device = tpmDeviceId,
                    appversion = appVersion,
                    readingtime = telemetryData.readingtime.ToString(),
                    temperature = telemetryData.temperature.ToString(),
                    humidity = telemetryData.humidity.ToString(),
                    brightness = telemetryData.brightness.ToString(),
                    loudness = soundBufferAverage.ToString()
                };

                // Update the UI with the current telemetry data.
                uiDataTransferOut.Text += "Data is:";
                uiDataTransferOut.Text += "\n- device: " + tpmDeviceId;
                uiDataTransferOut.Text += "\n- appversion: " + appVersion;
                uiDataTransferOut.Text += "\n- readingtime: " + telemetryData.readingtime;
                uiDataTransferOut.Text += "\n- temperature: " + telemetryData.temperature.ToString();
                uiDataTransferOut.Text += "\n- humidity: " + telemetryData.humidity.ToString();
                uiDataTransferOut.Text += "\n- brightness: " + telemetryData.brightness.ToString();
                uiDataTransferOut.Text += "\n- loudness: " + soundBufferAverage.ToString();

                // Convert the telemetry data into a JSON object and message.
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Send the message to the Azure IoT Hub.
                await deviceClient.SendEventAsync(message);

                uiDataTransferOut.Text += "\nDone sending data.";
            }
            catch (Exception ex)
            {
                uiDataTransferOut.Text += "\nCould not send telemetry data: " + ex.GetType().ToString();

                // Check the actual error why the telemetry data yould not be sent.
                // Eventually the SAS token will expire and the IoT Hub will return a
                // Microsoft.Azure.Devices.Client.Exceptions.UnauthorizedException
                // exception.
                // In this case, we need to request a new SAS token and connect again.
                if (ex.GetType().ToString() == "Microsoft.Azure.Devices.Client.Exceptions.UnauthorizedException")
                {
                    uiDataTransferOut.Text += "\nConnection to IoT Hub has been closed, reconnecting.";
                    EstablishIoTHubConnection();
                }

                // Otherwise something else might be wrong. Add exception handling here as required
                // for your specific use case.
                // Application.Current.Exit();
            }

            uiDataTransferOut.Text += "\nCleaning data, restarting timers, releasing block.";
            telemetryData.brightness = 0;
            telemetryData.loudness = 0;

            timerProcessBrightness.Start();
            timerProcessLoudness.Start();
            timerProcessTemperatureHumidity.Start();

            dataSendInProgress = false;
        }


        /// <summary>
        /// Explicitly trigger measuring & sending data via the UI button.
        /// </summary>
        /// <returns>void</returns>
        private void SendData_Button_Click(object sender, RoutedEventArgs e)
        {
            SendTelemetryData(null, null);
        }
    }
}
