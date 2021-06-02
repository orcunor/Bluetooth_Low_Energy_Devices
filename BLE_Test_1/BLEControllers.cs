using Akka.Dispatch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;


namespace BLE_Test_1
{
    class BLEControllers
    {

        private GattCharacteristic gattCharacteristic;
        //private static object lockObject = new object();
        private BluetoothLEDevice bluetoothLeDevice;
        public List<DeviceInformation> bluetoothLeDevicesList = new List<DeviceInformation>();
        public DeviceInformation selectedBluetoothLeDevice = null;
        //public List<DeviceInformation> UnknownDevices = new List<DeviceInformation>();
      
        //public string[] matrix;
        //public const int rowSize = 14; //Kaç göz varsa o kadar yazılır
        //private byte cardID;
        //public bool IsScannerActiwe { get; set; }
        //public bool ButtonPressed { get; set; }
        //public bool IsConnected { get; set; }

        public static string StopStatus = null;
        public BluetoothLEAdvertisementWatcher watcher;
        private DeviceWatcher deviceWatcher;
       

        public BLEControllers()
        {
            StartBleDeviceWatcher();
            //StartWatcher();
            //StartAdvertisementWatcher();
            //ConnectDevice(selectedBluetoothLeDevice);
        }

        #region Methods

        private void StartBleDeviceWatcher()
        {
            // Additional properties we would like about the device.
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // BT_Code: Example showing paired and non-paired in a single query.
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);

            // Register event handlers before starting the watcher.
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            // Start over with an empty collection.
            bluetoothLeDevicesList.Clear();

            // Start the watcher. Active enumeration is limited to approximately 30 seconds.
            // This limits power usage and reduces interference with other Bluetooth activities.
            // To monitor for the presence of Bluetooth LE devices for an extended period,
            // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
            // sample for an example.
            deviceWatcher.Start();
        }

        private void StopBleDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                // Unregister the event handlers.
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                deviceWatcher.Removed -= DeviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped -= DeviceWatcher_Stopped;

                // Stop the watcher.
                deviceWatcher.Stop();
                deviceWatcher = null;
            }
        }

        public DeviceInformation FindUnknownDevices(string id,string name)
        {
            foreach (DeviceInformation bleDeviceInfo in bluetoothLeDevicesList)
            {
                if (bleDeviceInfo.Id == id && bleDeviceInfo.Name == name)
                {
                    return bleDeviceInfo;
                }
            }
            return null;
        }


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

        public async void Deneme(DeviceInformation deviceInformation)
        {
            BluetoothLEDevice device = await BluetoothLEDevice.FromIdAsync(deviceInformation.Id);
            GattDeviceServicesResult gattDeviceServicesResult = await device.GetGattServicesAsync();
            if (gattDeviceServicesResult == null || gattDeviceServicesResult.Status != GattCommunicationStatus.Success) return;
            IReadOnlyList<GattDeviceService> gattDeviceServices = gattDeviceServicesResult.Services;
            foreach (GattDeviceService gattDeviceService in gattDeviceServices)
            {
                //Console.WriteLine(gattDeviceService.Uuid);
                if (gattDeviceService.Uuid == new Guid("7905F431-B5CE-4E99-A40F-4B1E122D00D0"))// Apple Notification Central Service
                {
                    // device with the given service found
                    Console.WriteLine("Apple Notification Center Service");
                    
                }
                else if (gattDeviceService.Uuid == new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455")) // RN4677
                {
                    Console.WriteLine("RN4677");
                }
                


            }
        }
               

        public void StartAdvertisementWatcher()
        {
            try
            {
                // The following code demonstrates how to create a Bluetooth LE Advertisement watcher,
                // set a callback, and start watching for all LE advertisements.
                watcher = new BluetoothLEAdvertisementWatcher();

                watcher.Received += OnAdvertisementReceived;
                watcher.Stopped += OnAdvertisementStopped;

                watcher.Start();

                watcher.ScanningMode = BluetoothLEScanningMode.Active;


                //watcher = new BluetoothLEAdvertisementWatcher();
                //watcher.ScanningMode = BluetoothLEScanningMode.Active;
                //watcher.SignalStrengthFilter = new BluetoothSignalStrengthFilter
                //{
                //    InRangeThresholdInDBm = -75,
                //    OutOfRangeThresholdInDBm = -76,
                //    OutOfRangeTimeout = TimeSpan.FromSeconds(2),
                //    SamplingInterval = TimeSpan.FromSeconds(2)
                //};
                //watcher.AdvertisementFilter =
                //     new BluetoothLEAdvertisementFilter
                //     {
                //         Advertisement =
                //                  new BluetoothLEAdvertisement
                //                  {
                //                      ServiceUuids =
                //                                {
                //                        //BLEHelper.ServiceId
                //                                }
                //                  }
                //     };
                //watcher.Received += OnAdvertisementReceived;
                //watcher.Start();
                //To receive scan response advertisements as well, set the following after creating the watcher.
                //Note that this will cause greater power drain and is not available while in background modes.
                //watcher.ScanningMode = BluetoothLEScanningMode.Active;

                //// Set the in-range threshold to -70dBm. This means advertisements with RSSI >= -70dBm 
                //// will start to be considered "in-range" (callbacks will start in this range).
                //watcher.SignalStrengthFilter.InRangeThresholdInDBm = -70;

                //// Set the out-of-range threshold to -75dBm (give some buffer). Used in conjunction 
                //// with OutOfRangeTimeout to determine when an advertisement is no longer 
                //// considered "in-range".
                //watcher.SignalStrengthFilter.OutOfRangeThresholdInDBm = -75;

                //// Set the out-of-range timeout to be 2 seconds. Used in conjunction with 
                //// OutOfRangeThresholdInDBm to determine when an advertisement is no longer 
                //// considered "in-range"
                //watcher.SignalStrengthFilter.OutOfRangeTimeout = TimeSpan.FromMilliseconds(2000);

                //var manufacturerData = new BluetoothLEManufacturerData();
                //manufacturerData.CompanyId = 0xFFFE;

                //// Make sure that the buffer length can fit within an advertisement payload (~20 bytes). 
                //// Otherwise you will get an exception.
                //var writer = new DataWriter();
                //writer.WriteString("Hello World");
                //manufacturerData.Data = writer.DetachBuffer();

                //watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        //public async Task<DeviceInformationCollection> EnumerateSnapshot()
        //{
        //    DeviceInformationCollection collection = await DeviceInformation.FindAllAsync();
        //    return collection;
        //}

        public void StopWatcher()
        {
            try
            {
                if (deviceWatcher.Status == Windows.Devices.Enumeration.DeviceWatcherStatus.Stopped)
                {
                    StopStatus = "The enumeration is already stopped.";
                }
                else
                {
                    deviceWatcher.Stop();
                }
            }
            catch (ArgumentException ex)
            {
                Trace.WriteLine("Caught ArgumentException. Failed to stop watcher: " + ex);
            }
        }

        //public void SetDefaults(byte _cardID)
        //{
        //    cardID = _cardID;
        //    ButtonPressed = true;
        //    IsScannerActiwe = true;
        //}

        public async void ConnectDeviceWithName(string deviceName)
        {
            try
            {
                this.DisConnect();

                var deviceInstance = bluetoothLeDevicesList.FirstOrDefault(x => x.Name == deviceName);
                Console.WriteLine("created instance");
                if (deviceInstance != null)
                {
                    bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInstance.Id);

                    GattDeviceServicesResult resultGattService = await bluetoothLeDevice.GetGattServicesAsync();

                    if (resultGattService.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var service in resultGattService.Services)
                        {
                            Console.WriteLine($"{deviceInstance.Name} Cihazının Sahip olduğu Servis UUID's:  "+ service.Uuid);
                            //if (service.Uuid.ToString() == "7905f431-b5ce-4e99-a40f-4b1e122d00d0") // Apple Notifications Center Service
                            //{
                            //    Console.WriteLine("Selam iphoneeeeeeeeeeeee");
                            //}
                            if (service.Uuid.ToString() == "00001801-0000-1000-8000-00805f9b34fb") //      Generic Attribute                       RN4677       49535343-fe7d-4ae5-8fa9-9fafd205e455
                            {
                                GattCharacteristicsResult resultCharacteristics = await service.GetCharacteristicsAsync();

                                if (resultCharacteristics.Status == GattCommunicationStatus.Success)
                                {
                                    var characteristics = resultCharacteristics.Characteristics;

                                    foreach (var characteristic in characteristics)
                                    {
                                        Console.WriteLine(" Generic Attribute Servisin sahip olduğu characteristics UUIDS: "+characteristic.Uuid);
                                        if (characteristic.Uuid.ToString() == "00002a05-0000-1000-8000-00805f9b34fb") // notify  49535343-1e4d-4bd9-ba61-23c647249616        Fitness Machine Status
                                        {
                                            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;

                                            if (properties.HasFlag(GattCharacteristicProperties.Indicate)) // notify
                                            {
                                                GattCommunicationStatus communicationStatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate); // Notify

                                                if (communicationStatus == GattCommunicationStatus.Success)
                                                {
                                                    Console.WriteLine("Successs");
                                                    //IsScannerActiwe = false;
                                                    //ButtonPressed = false;
                                                    //IsConnected = true;

                                                    gattCharacteristic = characteristic;
                                                    gattCharacteristic.ValueChanged += Characteristic_ValueChanged;

                                                    //tryConnectCount = 3;

                                                    //return true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }

        }

        public async void ConnectDevice(DeviceInformation deviceInfo)   // public async Task<bool> Connect(string deviceName)  DeviceInformation deviceInfo
        {
            try
            {
                this.DisConnect();
                Console.WriteLine("Disconnect.....");
                int tryConnectCount = 0;

                while (tryConnectCount < 3)
                {
                    // Note: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                    BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
                    //var deviceInstance = bluetoothLeDevicesList.FirstOrDefault(x => x.Name == deviceName);

                    // ...
                    if (bluetoothLeDevice != null)
                    {
                        Console.WriteLine("null değil");
                        GattDeviceServicesResult resultService = await bluetoothLeDevice.GetGattServicesAsync();

                        if (resultService.Status == GattCommunicationStatus.Success)
                        {
                            Console.WriteLine("successss");
                            var services = resultService.Services;
                            
                            foreach (var service in services)
                            {
                                Console.WriteLine("servicelerrr ->> "+ service.Uuid);
                                GattCharacteristicsResult resultCharacestics = await service.GetCharacteristicsAsync();

                                if (resultCharacestics.Status == GattCommunicationStatus.Success)
                                {
                                    var characteristics = resultCharacestics.Characteristics;

                                    foreach (var character in characteristics)
                                    {
                                        GattCharacteristicProperties properties = character.CharacteristicProperties;

                                        if (properties.HasFlag(GattCharacteristicProperties.Read)) // read i destekliyorsa
                                        {
                                            // This characteristic supports reading from it.
                                            GattReadResult result = await character.ReadValueAsync();
                                            Console.WriteLine("Readi destekliyorsa");
                                            if (result.Status == GattCommunicationStatus.Success)
                                            {
                                                Console.WriteLine("Readi destekliyorsa Success");
                                                var reader = DataReader.FromBuffer(result.Value);
                                                byte[] input = new byte[reader.UnconsumedBufferLength];
                                                reader.ReadBytes(input);
                                                // Utilize the data as needed
                                                tryConnectCount = 3;
                                            }

                                        }
                                        else if (properties.HasFlag(GattCharacteristicProperties.Write)) // write 'ı destekliyorsa
                                        {
                                            Console.WriteLine("WRİTEI destekliyorsa");
                                            // This characteristic supports writing to it.
                                            var writer = new DataWriter();
                                            // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
                                            writer.WriteByte(0x01);

                                            GattCommunicationStatus result = await character.WriteValueAsync(writer.DetachBuffer());
                                            if (result == GattCommunicationStatus.Success)
                                            {
                                                Console.WriteLine("WRİTEI destekliyorsa SUCCESS");
                                                gattCharacteristic = character;
                                                gattCharacteristic.ValueChanged += Characteristic_ValueChanged;
                                                // Successfully wrote to device
                                                tryConnectCount = 3;
                                            }

                                        }
                                        else if (properties.HasFlag(GattCharacteristicProperties.Notify)) // 
                                        {
                                            Console.WriteLine("Subscribing destekliyorsa");
                                            // This characteristic supports subscribing to notifications.
                                            GattCommunicationStatus status = await character.WriteClientCharacteristicConfigurationDescriptorAsync(
                                                                                       GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                            if (status == GattCommunicationStatus.Success)
                                            {
                                                Console.WriteLine("Subscribing destekliyorsa SUCCESS");
                                                gattCharacteristic = character;
                                                gattCharacteristic.ValueChanged += Characteristic_ValueChanged;
                                                // Server has been informed of clients interest
                                                tryConnectCount = 3;

                                            }

                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("success değil");
                        }
                    }
                    tryConnectCount++;
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public List<DeviceInformation> GetBluetoothLEDevicesList()
        {
            try
            {

                return bluetoothLeDevicesList;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Handled -> GetBluetoothLEDevices: " + ex);
                throw ex;
            }
        }

        public List<string> GetBluetoothLEDevices()
        {
            try
            {
                return bluetoothLeDevicesList.Select(x => x.Name).ToList();
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("Exception Handled -> GetBluetoothLEDevices: " + ex);
                throw ex;
            }
        }

        public DeviceInformation GetSelectedBluetoothLEDevice()
        {
            try
            {
                selectedBluetoothLeDevice = bluetoothLeDevicesList.FirstOrDefault();
                return selectedBluetoothLeDevice;
            }
            catch (System.Exception ex)
            {
                Trace.WriteLine("Exception Handled -> GetBluetoothLEDevices: " + ex);
                throw ex;
            }
        }
        public async void ShowPairingDevices()
        {
            var devices = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelector());
            foreach (DeviceInformation di in devices)
            {
                BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(di.Id);

                Console.WriteLine(bleDevice.Name);
                Console.WriteLine(bleDevice.DeviceId);
            }
        }

        public void DisConnect()
        {
            try
            {
                //IsScannerActiwe = false;
                //ButtonPressed = false;
                //IsConnected = false;

                if (bluetoothLeDevice != null)
                {
                    bluetoothLeDevice.Dispose();
                    bluetoothLeDevice = null;
                }

                if (gattCharacteristic != null)
                {
                    gattCharacteristic.Service.Dispose();
                    gattCharacteristic = null;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Exception Handled -> DisConnect: " + ex);
            }
        }

        public bool IsConnectable(DeviceInformation deviceInformation)
        {
            if (string.IsNullOrEmpty(deviceInformation.Name))
                return false;
            // Let's make it connectable by default, we have error handles in case it doesn't work
            bool isConnectable = (bool?)deviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true;
            bool isConnected = (bool?)deviceInformation.Properties["System.Devices.Aep.IsConnected"] == true;
            return isConnectable || isConnected;
        }

        //private string ToBit(byte value)
        //{
        //    try
        //    {
        //        return Convert.ToString(value, 2).PadLeft(8, '0');
        //    }
        //    catch { return String.Empty; }
        //}


        //public bool IsButtonPressed()
        //{
        //    lock (lockObject)
        //        return ButtonPressed;
        //}
        public async void Pair(string name)
        {
            // Do not allow a new Pair operation to start if an existing one is in progress.
            Console.WriteLine("Pairing started. Please wait...");

            // For more information about device pairing, including examples of
            // customizing the pairing process, see the DeviceEnumerationAndPairing sample.

            selectedBluetoothLeDevice = bluetoothLeDevicesList.FirstOrDefault(x=>x.Name == name);

            // Pair the currently selected device.
            DevicePairingResult result = await selectedBluetoothLeDevice.Pairing.PairAsync();

            if (result.Status == DevicePairingResultStatus.Paired)
            {
                Console.WriteLine("Pairledim");
            }
            else
            {
                Console.WriteLine("pairleyemedim.");
            }
            

        }
            
        
        #endregion


        #region Events


        private void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // An Indicate or Notify reported that the value has changed.
            var reader = DataReader.FromBuffer(args.CharacteristicValue);

            // Parse the data however required.
        }
        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Console.WriteLine("No longer watching for devices.") ;
        }
        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            try
            {
               
                if (deviceInformation.Name != null && deviceInformation.Name != "" && deviceInformation.Id != null)
                {
                    Console.WriteLine(string.Format("Bluetooth Controller -> DeviceWatcher_Added -> Device Id: {0}  Device Name: {1}", deviceInformation.Id, deviceInformation.Name));
                }

                // protect against race condition if the task runs after the app stopped the devicewatcher.
                if (sender == deviceWatcher)
                {
                    // make sure device isn't already present in the list.
                    if (FindUnknownDevices(deviceInformation.Id,deviceInformation.Name) == null)
                    {
                        if (deviceInformation.Name != string.Empty)
                        {
                            // if device has a name add it to the list
                            bluetoothLeDevicesList.Add(deviceInformation);
                            //Console.WriteLine("Device Count: " + bluetoothLeDevicesList.Count);
                        }

                    }

                }
            }
            catch (Exception)
            {

                throw;
            }
               
                //Console.WriteLine($"Name of Device -> {name}, ID of Device -> {deviceID}, Is device enabled ? {isEnabled}");
                //bluetoothLeDevicesList.Add(deviceInformation);
                ////foreach (var device in bluetoothLeDevicesList)
                ////{
                ////    Console.WriteLine("---" + device.Name);
                ////}
                ////Console.WriteLine(bluetoothLeDevicesList.Count);

        }
        private  void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            try
            {
                // We must update the collection on the UI thread because the collection is databound to a UI element.
                
                    // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                    if (sender == deviceWatcher)
                    {
                        Console.WriteLine(($"{bluetoothLeDevicesList.Count} devices found. Enumeration completed."));

                    }
                foreach (var devices in bluetoothLeDevicesList)
                {
                    Console.WriteLine($"Listedeki BLE Cihaz Adı:{devices.Name} Id -> {devices.Id}" );
                }

                StopBleDeviceWatcher();
                //selectedBluetoothLeDevice = bluetoothLeDevicesList.FirstOrDefault();
                //this.Pair(selectedBluetoothLeDevice.Name);
                //ConnectDevice(selectedBluetoothLeDevice);
                ConnectDeviceWithName("selam");

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            
        }
        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            //DeviceInformation deviceInfo = FindUnknownDevices(args.Id);
            //if (deviceInfo != null)
            //{
            //    deviceInfo.Update(args);
                
            //    if (deviceInfo.Name != String.Empty)
            //    {
                   
            //    }
            //}
            var id = args.Id;
            var kind = args.Kind;
            Console.WriteLine($"Updated device ID : {id} Device Kind : {kind}");
            //selectedBluetoothLeDevice = bluetoothLeDevicesList.FirstOrDefault();
            //Deneme(selectedBluetoothLeDevice);
            //ConnectDevice(selectedBluetoothLeDevice);

            //Deneme(selectedBluetoothLeDevice);

        }
        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            try
            {
                    var instance = bluetoothLeDevicesList.FirstOrDefault(x => x.Id == args.Id);

                    if (instance != null)
                    {
                        if (bluetoothLeDevice != null)
                        {
                            if (instance.Name != bluetoothLeDevice.Name)
                                bluetoothLeDevicesList.Remove(bluetoothLeDevicesList.FirstOrDefault(x => x.Id == args.Id));
                        }
                        else
                            bluetoothLeDevicesList.Remove(bluetoothLeDevicesList.FirstOrDefault(x => x.Id == args.Id));

                        Console.WriteLine("Bluetooth Controller -> DeviceWatcher_Removed -> Device: " + instance.Name + " -> DeviceCount: " + bluetoothLeDevicesList.Count); 
                    }
                
            }
                catch (Exception ex) { Console.WriteLine("Exception Handled -> DeviceWatcher_Removed: " + ex); }
            }
        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            try
            {
                
                // The received signal strength indicator (RSSI)
                Int16 rssi = eventArgs.RawSignalStrengthInDBm;
                Console.WriteLine("RSSI = " + rssi);
                var address = eventArgs.BluetoothAddress;
                Console.WriteLine("Adress = " + address.ToString());
                var type = eventArgs.AdvertisementType;
                Console.WriteLine("Type = " + type.ToString());
                Console.WriteLine("-----------------------------");
                // Do whatever you want with the advertisement
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message); 
            }
            

        }
        private async void OnAdvertisementStopped(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs)
        {
            Console.WriteLine("Stopped.");

        }
        private void BleAdvertHandlerAsync(BluetoothLEAdvertisementReceivedEventArgs args)
        {

        }

        #endregion 
    } 

}


