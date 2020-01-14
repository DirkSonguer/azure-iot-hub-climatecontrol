# Summary
In this sample you created an end-to-end IoT senario, working as a climate control system. An IoT device quantifies environment conditions like temperature, humidity, brightness and loudness and sends the telemetry data to an Azure IoT Hub, which in turn uses a trigger to persist the data in a database for later usage. Further, the device can be configured via the IoT Hub.

The scenario should offer lots of opportunity to extend the individual components with your own functionalities. Here is some inspiration:

* Add more sensors to the IoT device and include their data as telemetry
* Add logic to the Azure Function to send notifications if a data point exceeds or is below a defined threshold
* Create some form of analytics for the recorded telemetry data, for example using [analytics](https://azure.microsoft.com/en-us/product-categories/analytics/) or [Power BI](https://powerbi.microsoft.com/en-us/)
* Create a public endpoint / API for other services to access your recorded telemetry data, maybe doing some visualization with it

## Feedback & participation
I hope this sample is helpful to you. If you have feedback, feel free to contact me via [Twitter](https://twitter.com/dirksonguer). If you encounter an error or would like to add / change things, please create a PR. Thank you!


## Further Reading
* The [Microsoft IoT Developer blog](https://devblogs.microsoft.com/iotdev/) is a good place to get more information around Azure IoT and related topics.

* A [technical sample](https://github.com/microsoft/Windows-iotcore-samples/tree/develop/Samples/HelloBlinky/CS) on how to control hardware outputs on the Raspberry Pi. Use this if you don't want to use Grove.

* Building upon that, [here](https://docs.microsoft.com/en-us/archive/blogs/uktechnet/creating-a-simple-windows-10-iot-application-led-dice) is a more technical example how to control multiple outputs and add additional logic.

* More information on [building apps on Windows 10 IoT](https://docs.microsoft.com/en-us/windows/iot-core/develop-your-app/buildingappsforiotcore)

* [Here](https://blogs.windows.com/windowsdeveloper/2016/07/20/building-secure-apps-for-windows-iot-core/) you can find more information on the whole provisioning aspect.


## Additional tools
The Device Explorer Twin Application allows you to connect to your Azure IoT Hub instance and monitor the messages sent by each connected device: https://github.com/Azure/azure-iot-sdk-csharp/releases/download/2018-3-13/SetupDeviceExplorer.msi 

---

**Back to the [project README](../README.md).**