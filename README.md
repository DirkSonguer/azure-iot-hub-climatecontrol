# azure-iot-hub-climatecontrol
This is a sample project that shows how to set up a working Azure IoT environment, using Azure IoT Hub and some IoT sensor devices.

Table of contents:
1) [Project Prerequisites](documentation/1-prerequisites.md)
2) [Setting up Azure IoT Hub](documentation/2-azure-iot-hub-setup.md)
3) [Setting up a database](documentation/3-database-setup.md)
4) [Building the devices](documentation/4-device-setup.md)
5) [Provisioning your device](documentation/5-device-provisioning.md)
6) [Application running on the device](documentation/6-device-application.md)
7) [Azure Functions](documentation/7-azure-functions.md)
8) [Summary and further reading](documentation/8-summary-further-reading.md)


## Overall project goals
The goal is to provide an easy to follow example how to create an end-to-end Internet of Things project using the Azure IoT platform.

1) **Easy to follow** means that beginners with limited prior exposure to hardware or IoT projects can follow the steps, set up their own instance of the project and then continue from there
2) **End-to-end** means that the sample covers the device side of things, but also the server / platform side, including things like device provisioning, communications and so on
3) **Project** means that the outcome is a sensible, real-world scenario and not an abstract concept


## Specific project goals
The sample scenario is about climate control: A system that is able quantify environment conditions like temperature, humidity, brightness and loudness and then makes this data actionable. The scenario was chosen as it is easy to extend into home automation scenarios as well as to apply the learnings directly to scenarios like predictive maintenance and automation.


## Project architecture
The project is designed as a [multilayer architrecture](https://en.wikipedia.org/wiki/Multitier_architecture) where every component is loosely coupled to make it easier to extend or repurpose the example code.

The components are:
1) Azure IoT Hub is used to take care of device provisioning, authentication, management and communication 2) The hardware device with attached environmental sensors will allow to collect data about the surroundings 
3) An application on the hardware device will query the sensors, do some pre-processing and send the data to the Azure IoT Hub
4) A service collects the telemetry data and write it to a database
5) The database provides long term storage and retrieval for the telemetry data


## Considerations and disclaimers
The example is about an end-to-end showcase of an IoT project, so we will focus mainly on the IoT aspects. That means we will gloss over topics like networking, virtualization / containerization and microservice architecture. If you are interested in those, there are great ressources out there already.

Also, the sample chose simplicity over "doing things right" in some cases. For example the chosen IoT prototyping platform (Raspberry Pi) has no TPM hardware, thus we are not able to properly secure / provision it. That said, it's easy to obtain and to develop for and while not secure, the TPM software emulation gets the point across.

In other words: If you want to take this scenario into any kind of production environment, things get more complicated.

**The goal is to learn more about Azure IoT and the project is not meant as a production ready project template!**

Also: This scenario is about collecting data from an environment. One design goal was to make sure the project does not to collect any personal data or invade the privacy or personal space of anybody while doing so. This scenario is not about tracking people and no data shall be collected that allows doing so.
