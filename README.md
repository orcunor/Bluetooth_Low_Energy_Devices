# Bluetooth Low Energy Devices Communication - Pairing and Connecting , Device Enumeration
Seeing nearby bluetooth-low-energy devices, getting information about devices by triggering events (update, add,remove) Seeing information about Gatt, Gap protocol services, characters, features and attributes. A console application that provides write, read and subscribe, communication between client and server.

# Documentation
You can check the Bluetooth Documentation.pdf file I prepared for documentation. 

# Requirements:
Windows 10, BT 4.0 adapter

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
