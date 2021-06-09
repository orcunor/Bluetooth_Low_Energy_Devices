
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;


namespace BLE_Test_1
{
    class BLEControllers
    {
        #region Fields
        GattDeviceService service = null;
        GattCharacteristic charac = null;
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

        Guid MyService_GUID; // StartAdvertisementWatcher 
        Guid MYCharacteristic_GUID;  // StartAdvertisementWatcher 
        string bleDeviceName = "StockArtScan-F401"; // StartAdvertisementWatcher 
        long deviceFoundMilis = 0, serviceFoundMilis = 0;  // StartAdvertisementWatcher 
        long connectedMilis = 0, characteristicFoundMilis = 0;  // StartAdvertisementWatcher 
        long WriteDescriptorMilis = 0;  // StartAdvertisementWatcher 
        Stopwatch stopwatch;  // StartAdvertisementWatcher 
        #endregion

        #region Constructor
        public BLEControllers()
        {
            //StartBleDeviceWatcher();
            //StartWatcher();
            //StartAdvertisementWatcher();
        }
        #endregion

        #region Methods

        public void StartAdvertisementWatcher()
        {
            try
            {
                //My service !!!
                MyService_GUID = new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455");
                //My characteristic!!!
                MYCharacteristic_GUID = new Guid("{49535343-1e4d-4bd9-ba61-23c647249616}");

                // The following code demonstrates how to create a Bluetooth LE Advertisement watcher,
                // set a callback, and start watching for all LE advertisements.
                watcher = new BluetoothLEAdvertisementWatcher();
                watcher.ScanningMode = BluetoothLEScanningMode.Active;

                
                watcher.Received += OnAdvertisementReceivedAsync;
                watcher.Stopped += OnAdvertisementStopped;

                stopwatch = new Stopwatch();
                stopwatch.Start();

                watcher.Start();
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        public void StartBleDeviceWatcher()
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

        public void StopBleDeviceWatcher()
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

        public async void TestServiceForMyPhone(DeviceInformation deviceInformation)
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
                    Console.WriteLine("My phone : Apple Notification Center Service");

                }
                else if (gattDeviceService.Uuid == new Guid("49535343-fe7d-4ae5-8fa9-9fafd205e455")) // RN4677
                {
                    Console.WriteLine("RN4677");
                }



            }
        }

        private DeviceInformation FindUnknownDevices(string id, string name)
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
                int tryConnectCount = 0;

                while (tryConnectCount < 3)
                {
                    var deviceInstance = bluetoothLeDevicesList.FirstOrDefault(x => x.Name == deviceName);

                    if (deviceInstance != null)
                    {
                        bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInstance.Id);

                        GattDeviceServicesResult resultGattService = await bluetoothLeDevice.GetGattServicesAsync();

                        if (resultGattService.Status == GattCommunicationStatus.Success)
                        {
                            foreach (var service in resultGattService.Services)
                            {
                                Console.WriteLine($"{  deviceInstance.Name  }  Cihazının Sahip olduğu Servis UUID's:   " + service.Uuid);
                                
                                if ((service.Uuid.ToString() == "49535343-fe7d-4ae5-8fa9-9fafd205e455")) //      Generic Attribute 49535343-fe7d-4ae5-8fa9-9fafd205e455     RN4677           
                                {
                                    GattCharacteristicsResult resultCharacteristics = await service.GetCharacteristicsAsync();

                                    if (resultCharacteristics.Status == GattCommunicationStatus.Success)
                                    {
                                        var characteristics = resultCharacteristics.Characteristics;

                                        foreach (var characteristic in characteristics)
                                        {
                                            Console.WriteLine($"{service.Uuid.ToString() }  Servisininn sahip olduğu characteristics UUIDS: {characteristic.Uuid}");
                                            if (characteristic.Uuid.ToString() == "49535343-1e4d-4bd9-ba61-23c647249616") // notify    49535343-1e4d-4bd9-ba61-23c647249616       Fitness Machine Status    
                                            {
                                                string descriptor = string.Empty;
                                                GattCharacteristicProperties properties = characteristic.CharacteristicProperties;


                                                if (properties.HasFlag(GattCharacteristicProperties.Indicate))
                                                {
                                                    Console.WriteLine("This characteristic supports subscribing to Indication");
                                                    GattCommunicationStatus communicationStatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
                                                    if (communicationStatus == GattCommunicationStatus.Success)
                                                    {
                                                        descriptor = "indications";
                                                        Console.WriteLine("Successfully registered for  " + descriptor);
                                                        characteristic.ValueChanged += Charac_ValueChanged; ;

                                                    }
                                                }

                                                if (properties.HasFlag(GattCharacteristicProperties.Notify)) // notify i destekliyorsa
                                                {
                                                    Console.WriteLine("This characteristic supports subscribing to notifications.");
                                                    GattCommunicationStatus communicationStatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);

                                                    if (communicationStatus == GattCommunicationStatus.Success)
                                                    {
                                                        descriptor = "notifications";

                                                        //IsScannerActiwe = false;
                                                        //ButtonPressed = false;
                                                        //IsConnected = true;

                                                        Console.WriteLine("Successfully registered for " + descriptor );
                                                        gattCharacteristic = characteristic;
                                                        gattCharacteristic.ValueChanged += GattCharacteristic_ValueChanged;
                                                        //tryConnectCount = 3;

                                                    }
                                                }
                                                
                                                if (properties.HasFlag(GattCharacteristicProperties.Read)) // notify
                                                {
                                                    Console.WriteLine("This characteristic supports reading .");
                                                    GattReadResult result = await characteristic.ReadValueAsync();
                                                    if (result.Status == GattCommunicationStatus.Success)
                                                    {

                                                        var reader = DataReader.FromBuffer(result.Value);
                                                        byte[] input = new byte[reader.UnconsumedBufferLength];
                                                        reader.ReadBytes(input);
                                                        Console.WriteLine("Read success Input Length : "+ input.Length);
                                                        // Utilize the data as needed
                                                    }
                                                }
                                                if (properties.HasFlag(GattCharacteristicProperties.Write)) // write 'ı destekliyorsa
                                                {
                                                    Console.WriteLine("This characteristic supports write .");
                                                    // This characteristic supports writing to it.
                                                    var writer = new DataWriter();
                                                    // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
                                                    byte[] testBytes = new byte[]
                                                    {
                                                        0x00,
                                                        0x01,
                                                        0x02,
                                                        0x03,
                                                        0x04,
                                                        0x05,
                                                        0x06,
                                                        0x07,
                                                        0x08

                                                    };
                                                    //writer.WriteByte(0x01);
                                                    writer.WriteBytes(testBytes);

                                                    GattCommunicationStatus result = await characteristic.WriteValueAsync(writer.DetachBuffer());

                                                    if (result == GattCommunicationStatus.Success)
                                                    {
                                                        Console.WriteLine("Write success.");
                                                        characteristic.ValueChanged += Charac_ValueChanged;
                                                        //gattCharacteristic = characteristic;
                                                        //gattCharacteristic.ValueChanged += GattCharacteristic_ValueChanged;

                                                        // Successfully wrote to device
                                                        tryConnectCount = 3;
                                                    }

                                                }

                                            }
                                            else
                                            {
                                                Console.WriteLine("Diğer karakteristik uuidler geldi.");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    tryConnectCount++;
                }       
             }
            catch (Exception exception) { Console.WriteLine("ConnectDeviceWithName Method Exception => " + exception.Message); }

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
                            Console.WriteLine("servie status successss");
                            var services = resultService.Services;

                            foreach (var service in services)
                            {
                                Console.WriteLine("service uuid ->> " + service.Uuid);
                                GattCharacteristicsResult resultCharacestics = await service.GetCharacteristicsAsync();

                                if (resultCharacestics.Status == GattCommunicationStatus.Success)
                                {
                                    var characteristics = resultCharacestics.Characteristics;

                                    foreach (var character in characteristics)
                                    {
                                        Console.WriteLine("---------karakter uuid ->> " + character.Uuid);
                                        GattCharacteristicProperties properties = character.CharacteristicProperties;

                                        if (properties.HasFlag(GattCharacteristicProperties.Read)) // read i destekliyorsa
                                        {
                                            // This characteristic supports reading from it.
                                            GattReadResult result = await character.ReadValueAsync();
                                            Console.WriteLine("Readi destekliyorsa");
                                            if (result.Status == GattCommunicationStatus.Success)
                                            {
                                                Console.WriteLine("Read Success");
                                                var reader = DataReader.FromBuffer(result.Value);
                                                byte[] input = new byte[reader.UnconsumedBufferLength];
                                                reader.ReadBytes(input);
                                                Console.WriteLine("------Input Length: "+ input.Length + "---------------------");
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
                                                Console.WriteLine("WRİTE SUCCESS");
                                                gattCharacteristic = character;
                                                gattCharacteristic.ValueChanged += GattCharacteristic_ValueChanged;
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
                                                Console.WriteLine("Subscribing SUCCESS");
                                                gattCharacteristic = character;
                                                gattCharacteristic.ValueChanged += GattCharacteristic_ValueChanged;
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
                if (deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    return bluetoothLeDevicesList;
                }
                return null;
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

                Console.WriteLine("Pairing device name : " + bleDevice.Name);
                Console.WriteLine("Pairing device Id : " + bleDevice.DeviceId);
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

            selectedBluetoothLeDevice = bluetoothLeDevicesList.FirstOrDefault(x => x.Name == name);

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

        private static void Charac_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out byte[] data);
            //If data is raw bytes skip all the next lines and use data byte array. Or
            //CryptographicBuffer.CopyToByteArray(args.CharacteristicValue, out byte[] dataArray);
            string dataFromNotify;
            try
            {
                //Asuming Encoding is in ASCII, can be UTF8 or other!
                dataFromNotify = Encoding.ASCII.GetString(data);
                Console.Write(dataFromNotify);
            }
            catch (ArgumentException)
            {
                Console.Write("Unknown format");
            }
        }
        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {

            BluetoothLEDevice bluetoothLEDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);
            if (bluetoothLEDevice != null)
            {
                GattDeviceServicesResult servicesResult = await bluetoothLEDevice.GetGattServicesAsync();
                foreach (GattDeviceService service in servicesResult.Services)
                {
                    GattCharacteristicsResult characteristicsResult = await service.GetCharacteristicsAsync();
                    if (characteristicsResult.Status == GattCommunicationStatus.Success)
                    {
                        foreach (GattCharacteristic characteristic in characteristicsResult.Characteristics)
                        {
                            if (characteristic.CharacteristicProperties.Equals(GattCharacteristicProperties.Read))
                            {
                                GattReadResult readResult = await characteristic.ReadValueAsync();
                            }
                        }
                    }
                    

                }
            }
        }
        private void GattCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            Console.WriteLine("asdfasdfasdf");
            // An Indicate or Notify reported that the value has changed.
            var reader = DataReader.FromBuffer(args.CharacteristicValue);

            //var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] input = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(input);

            string inComeData = String.Empty;
            foreach (var item in input)
                inComeData += item + " ";
             
            Console.WriteLine("Bluetooth Controller-> GattCharacteristic_ValueChanged-> Data: " + inComeData);

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
                //ConnectDeviceWithName("StockArtScan-F401");
                //var name = "StockArtScan-F401";
                //ConnectDevice(bluetoothLeDevicesList.FirstOrDefault(x => x.Name == name));     

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
        //private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        //{
        //    try
        //    {
                
        //        // The received signal strength indicator (RSSI)
        //        Int16 rssi = eventArgs.RawSignalStrengthInDBm;
        //        Console.WriteLine("RSSI = " + rssi);
        //        var address = eventArgs.BluetoothAddress;
        //        Console.WriteLine("Adress = " + address.ToString());
        //        var type = eventArgs.AdvertisementType;
        //        Console.WriteLine("Type = " + type.ToString());
        //        Console.WriteLine("-----------------------------");
        //        // Do whatever you want with the advertisement
        //    }
        //    catch (Exception ex)
        //    {

        //        Console.WriteLine(ex.Message); 
        //    }
            

        //}
        private async void OnAdvertisementStopped(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementWatcherStoppedEventArgs eventArgs)
        {
            Console.WriteLine("Stopped.");

        }
        private async void OnAdvertisementReceivedAsync(BluetoothLEAdvertisementWatcher watcher,
                                                BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            Console.WriteLine("Sinyal gönderiliyor....");
            // Filter for specific Device by name
            if (eventArgs.Advertisement.LocalName == bleDeviceName)
            {
                watcher.Stop();
                var device = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);
                //always check for null!!
                if (device != null)
                {
                    deviceFoundMilis = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine("Buldumm Ormiiiiii kankiiiiiiiiiiii");
                    Console.WriteLine("Device found in " + deviceFoundMilis + " ms");

                    var rssi = eventArgs.RawSignalStrengthInDBm;
                    Console.WriteLine("Signalstrengt = " + rssi + " DBm");

                    var bleAddress = eventArgs.BluetoothAddress;
                    Console.WriteLine("Ble address = " + bleAddress);

                    var advertisementType = eventArgs.AdvertisementType;
                    Console.WriteLine("Advertisement type = " + advertisementType);

                    var result = await device.GetGattServicesForUuidAsync(MyService_GUID);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        connectedMilis = stopwatch.ElapsedMilliseconds;
                        Console.WriteLine("Connected in " + (connectedMilis - deviceFoundMilis) + " ms");
                        var services = result.Services;
                        service = services[0];
                        if (service != null)
                        {
                            serviceFoundMilis = stopwatch.ElapsedMilliseconds;
                            Console.WriteLine("Service found in " +
                               (serviceFoundMilis - connectedMilis) + " ms" + " ," + "Service uuid : " + MyService_GUID.ToString());
                            
                            var charResult = await service.GetCharacteristicsForUuidAsync(MYCharacteristic_GUID);
                            if (charResult.Status == GattCommunicationStatus.Success)
                            {
                                charac = charResult.Characteristics[0];
                                if (charac != null)
                                {
                                    characteristicFoundMilis = stopwatch.ElapsedMilliseconds;
                                    Console.WriteLine("Characteristic found in " +
                                                   (characteristicFoundMilis - serviceFoundMilis) + " ms" + " ," + "Characteristic uuid : " + MYCharacteristic_GUID.ToString());

                                    var descriptorValue = GattClientCharacteristicConfigurationDescriptorValue.None;
                                    GattCharacteristicProperties properties = charac.CharacteristicProperties;
                                    string descriptor = string.Empty;

                                    if (properties.HasFlag(GattCharacteristicProperties.Read))
                                    {
                                        Console.WriteLine("This characteristic supports reading .");
                                        GattReadResult descriptorWriteResult = await charac.ReadValueAsync();

                                        if (descriptorWriteResult.Status == GattCommunicationStatus.Success)
                                        {    
                                            var reader = DataReader.FromBuffer(descriptorWriteResult.Value);
                                            byte[] input = new byte[reader.UnconsumedBufferLength];
                                            reader.ReadBytes(input);
                                            Console.WriteLine("Reading success Input Length : " + input.Length);
                                            // Utilize the data as needed
                                        }
                                    }
                                    if (properties.HasFlag(GattCharacteristicProperties.Write))
                                    {
                                        // This characteristic supports writing to it.
                                        Console.WriteLine("This characteristic supports writing .");

                                        var writer = new DataWriter();
                                        // WriteByte used for simplicity. Other common functions - WriteInt16 and WriteSingle
                                        byte[] testBytes = new byte[]
                                        {
                                                        0x00,
                                                        0x01,
                                                        0x02,
                                                        0x03,
                                                        0x04,
                                                        0x05,
                                                        0x06,
                                                        0x07,
                                                        0x08

                                        };
                                        //writer.WriteByte(0x01);
                                        writer.WriteBytes(testBytes);

                                        GattCommunicationStatus descriptorWriteResult = await charac.WriteValueAsync(writer.DetachBuffer());

                                        if (descriptorWriteResult == GattCommunicationStatus.Success)
                                        {
                                            Console.WriteLine("Write success.");
                                            charac.ValueChanged += Charac_ValueChanged;
                                            //gattCharacteristic = characteristic;
                                            //gattCharacteristic.ValueChanged += GattCharacteristic_ValueChanged;

                                            // Successfully wrote to device
                                            //tryConnectCount = 3;
                                        }

                                    }
                                    if (properties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                                    {
                                        Console.WriteLine("This characteristic supports writing  whithout responce.");
                                    }
                                    if (properties.HasFlag(GattCharacteristicProperties.Indicate))
                                    {
                                        descriptor = "indications";
                                        descriptorValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                                        Console.WriteLine("This characteristic supports subscribing to Indication");
                                    }
                                    if (properties.HasFlag(GattCharacteristicProperties.Notify))
                                    {
                                        descriptor = "notifications";
                                        descriptorValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                                        Console.WriteLine("This characteristic supports subscribing to notifications.");
                                    }
                                    
                                    try
                                    {
                                        var descriptorWriteResult = await charac.WriteClientCharacteristicConfigurationDescriptorAsync(descriptorValue);
                                        if (descriptorWriteResult == GattCommunicationStatus.Success)
                                        {

                                            WriteDescriptorMilis = stopwatch.ElapsedMilliseconds;
                                            Console.WriteLine("Successfully registered for " + descriptor + " in " +
                                                           (WriteDescriptorMilis - characteristicFoundMilis) + " ms");
                                            charac.ValueChanged += Charac_ValueChanged; ;
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Error registering for " + descriptor + ": {result}");
                                            this.DisConnect();
                                            device = null;
                                            watcher.Start(); //Start watcher again for retry
                                        }
                                    }
                                    catch (UnauthorizedAccessException ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                            }
                            else Console.WriteLine("No characteristics found");
                        }
                    }
                    else Console.WriteLine("No services found");
                }
                else Console.WriteLine("No device found");
            }
        }
        private void BleAdvertHandlerAsync(BluetoothLEAdvertisementReceivedEventArgs args)
        {

        }

        #endregion 
    } 

}


