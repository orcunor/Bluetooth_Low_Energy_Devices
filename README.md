# Bluetooth Low Energy Devices Communication - Pairing and Connecting , Device Enumeration
Seeing nearby bluetooth-low-energy devices, getting information about devices by triggering events (update, add,remove, enumerate devices) Seeing information about Gatt, Gap protocol services, characters, features and attributes. A console application that provides write, read and subscribe, communication between client and server.

# Documentation
You can check the Bluetooth Documentation.pdf file I prepared for documentation. 

# Requirements:
Windows 10, atleast BT 4.0 adapter or above

# Build in Visual Studio
For building from source, Microsoft Visual Studio is required (free, Community edition will work).

Start Microsoft Visual Studio and select File > Open > Project/Solution.
Double-click the Visual Studio Solution (.sln) file.
Press Ctrl+Shift+B, or select Build > Build Solution.
# Run the debug session
To debug the application and then run it, press F5 or select Debug > Start Debugging. To run without debugging, press Ctrl+F5 or selectDebug > Start Without Debugging.

Some debug information available in the "Output" section in Visual St

# Example of code:

    public void StartWatcher()
           {
               try
               {

                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Manufacturer", "System.Devices.Aep.ModelName", "System.Devices.Aep.ModelId" };

                deviceWatcher =
                          DeviceInformation.CreateWatcher(
                              BluetoothLEDevice.GetDeviceSelectorFromPairingState(false),
                                  requestedProperties,
                                  DeviceInformationKind.AssociationEndpoint);

                // Register event handlers before starting the watcher.
                // Added, Updated and Removed are required to get all nearby devices
                deviceWatcher.Added += DeviceWatcher_Added;
                deviceWatcher.Updated += DeviceWatcher_Updated;
                deviceWatcher.Removed += DeviceWatcher_Removed;

                // EnumerationCompleted and Stopped are optional to implement.
                deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped += DeviceWatcher_Stopped;


                // Start the watcher.
                deviceWatcher.Start();

            }

            catch (Exception ex)
            {

                Console.WriteLine("Exception -> ", ex.Message);
            }
        }

