using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Managed.Adb
    {
    /**
     * A Device monitor. This connects to the Android Debug Bridge and get device and debuggable
     * process information from it.
     */
    public class DeviceTracker
        {
        //---------------------------------------------------------------------------------------------
        // State
        // ---------------------------------------------------------------------------------------------

        public IList<Device>                Devices                 { get; private set; }
        public bool                         IsTrackingDevices       { get; private set; }
        public bool                         HasDeviceList           { get; private set; }

        private readonly AndroidDebugBridge bridge;
        private int                         adbFailedOpens;
        private bool                        stopRequested;
        private Socket                      socketTrackDevices;
        private Thread                      deviceTrackingThread;
        private const string                loggingTag   = "DeviceMonitor";
        private byte[]                      lengthBuffer = null;
        private ManualResetEventSlim        startedEvent = new ManualResetEventSlim(false);
        private ReaderWriterLock            socketLock   = new ReaderWriterLock();

        //---------------------------------------------------------------------------------------------
        // Constructoin 
        // ---------------------------------------------------------------------------------------------

        public DeviceTracker(AndroidDebugBridge bridge)
            {
            this.bridge        = bridge;
            this.Devices       = new List<Device>();
            this.lengthBuffer  = new byte[4];
            this.stopRequested = false;
            }

        public void StartDeviceTracking()
            {
            // Paranoia: stop, just in case
            StopDeviceTracking();

            // Start the monitor thread a-going
            this.deviceTrackingThread = new Thread(new ThreadStart(DeviceTrackingThread));
            this.deviceTrackingThread.Name = "Device List Monitor";
            this.deviceTrackingThread.Start();

            // Wait until the thread actually starts; this allows us to shut it down reliably
            // as we will actually have access to it's socket
            this.startedEvent.Wait();
            }

        // A lock for controlling access to the right to set the socket variable
        void AcquireSocketLock()    { this.socketLock.AcquireWriterLock(-1); }
        void ReleaseSocketLock()    { this.socketLock.ReleaseWriterLock();   }

        public void StopDeviceTracking()
            {
            if (this.stopRequested)
                {
                // Set the flag for he gets around to lookign
                this.stopRequested = true;
                
                // Close the socket to get him out of Receive() if he's there
                this.CloseSocket(ref socketTrackDevices);
                
                // Interrupt the thread just in case there are other waits
                this.deviceTrackingThread.Interrupt();

                this.deviceTrackingThread.Join();
                this.deviceTrackingThread = null;
                }
            }

        /**
         * Get ourselves a socket to our ADB server, restarting it as needed.
         *
         * @return  true if we opened a new socket
         */
        bool OpenSocketIfNecessary()
            {
            bool result = false;
            this.AcquireSocketLock();
            try
                {
                // If we haven't a socket, try to open one
                if (this.socketTrackDevices == null || !this.socketTrackDevices.Connected)
                    {
                    Debug.Assert(!this.stopRequested);
                    CloseSocket(ref this.socketTrackDevices);
                    this.socketTrackDevices = ConnectToServer();
                    //
                    if (this.socketTrackDevices == null)
                        {
                        // Connect attempt failed. Restart the server if we can
                        this.adbFailedOpens++;
                        if (this.adbFailedOpens > 10)
                            {
                            this.bridge.KillServer();
                            this.bridge.StartServer();
                            }

                        // Wait a bit before attempting another socket open
                        this.ReleaseSocketLock();
                        Thread.Sleep(1000);
                        this.AcquireSocketLock();
                        }
                    else
                        {
                        result = true;
                        Log.d(loggingTag, "Connected to adb for device monitoring");
                        this.adbFailedOpens = 0;
                        }
                    }
                }
            finally
                {
                this.ReleaseSocketLock();
                }
            return result;
            }

        /**
         * Close the socket, which may be null. If asked to, take the socket lock while doing so.
         */
        void CloseSocket(ref Socket socket, bool takeLock=true)
            {
            try {
                socket?.Close();
                try {
                    if (takeLock) this.AcquireSocketLock();
                    socket = null;
                    }
                finally
                    {
                    if (takeLock) this.ReleaseSocketLock();
                    }
                }
            catch (Exception)
                {
                // Don't actually know if anything ever throw on a close, 
                // but we'll ignore it, as we're just try to close, dang it
                }
            }

        void DeviceTrackingThread()
            {
            // Right here we know that Start() hasn't yet returned. Do the interlock and let it return.
            this.startedEvent.Set();

            // Loop until asked to stop
            while (!this.stopRequested)
                {
                try
                    {
                    if (OpenSocketIfNecessary())
                        {
                        // Ask the ADB server to give us device notifications
                        this.IsTrackingDevices = RequestDeviceNotifications();
                        }

                    if (this.IsTrackingDevices)
                        {
                        // read the length of the incoming message
                        int length = ReadLength(this.socketTrackDevices, this.lengthBuffer);
                        if (length >= 0)
                            {
                            // read the incoming message
                            ProcessTrackingDevicesNotification(length);

                            // flag the fact that we have build the list at least once.
                            this.HasDeviceList = true;
                            }
                        }
                    }
                catch (Exception e)
                    {
                    Log.e(loggingTag, "exception in DeviceTrackingThread: ", e);
                    this.IsTrackingDevices = false;
                    CloseSocket(ref this.socketTrackDevices);
                    }
                } 
            }

        //---------------------------------------------------------------------------------------------------------------
        // Device tracking
        //---------------------------------------------------------------------------------------------------------------

        #region Device Tracking        
        /**
         * Ask the ADB server to inform us of the connection and disconnection of devices
         *
         * @exception   IOException Thrown when an IO failure occurred.
         *
         * @return  true if it succeeds, false if it fails.
         */
        private bool RequestDeviceNotifications()
            {
            byte[] request = AdbHelper.Instance.FormAdbRequest("host:track-devices");
            AdbHelper.Instance.Write(this.socketTrackDevices, request);

            AdbResponse resp = AdbHelper.Instance.ReadAdbResponse(this.socketTrackDevices);
            if (!resp.IOSuccess)
                {
                Log.e(loggingTag, "Failed to read the adb response!");
                this.CloseSocket(ref this.socketTrackDevices);
                throw new IOException("Failed to read the adb response!");
                }

            if (!resp.Okay)
                {
                // request was refused by adb!
                Log.e(loggingTag, "adb refused request: {0}", resp.Message);
                }

            return resp.Okay;
            }

        private void ProcessTrackingDevicesNotification(int length)
            {
            List<Device> currentDevices = new List<Device>();
            if (length > 0)
                {
                byte[] buffer = new byte[length];
                string result = Read(this.socketTrackDevices, buffer);
                string[] devices = result.Split(new string[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string deviceData in devices)
                    {
                    try
                        {
                        Device device = Device.CreateFromAdbData(deviceData);
                        if (device != null)
                            {
                            currentDevices.Add(device);
                            }
                        }
                    catch (ArgumentException ae)
                        {
                        Log.e(loggingTag, ae);
                        }
                    }
                }

            UpdateDevices(currentDevices);
            }

        /**
         * Updates our understanding of the set of current devices based on a report
         * of the now-current set.
         *
         * @param   currentDevices  the report of the current devices
         */
        private void UpdateDevices(List<Device> newCurrentDevices)
            {
            lock (this.Devices)
                {
                // For each device in the existing list, we look for a match in the new current list.
                // * if we find it, we update the existing object with whatever new information
                //   there is (mostly state change, if the device becomes ready, we query for build info).
                //   We also remove the device from the new current list to mark it as "processed"
                // * if we do not find it, we remove it from our existing list.
                //
                // Once this is done, the new current list contains device we aren't tracking yet, so we
                // add them to the list

                for (int d = 0; d < this.Devices.Count;)
                    {
                    Device device = this.Devices[d];

                    // look for a similar device in the new list.
                    int count = newCurrentDevices.Count;
                    bool foundMatch = false;
                    for (int dd = 0; dd < count; dd++)
                        {
                        Device newDevice = newCurrentDevices[dd];
                        // see if it matches in serial number
                        if (Util.equalsIgnoreCase(newDevice.SerialNumber, device.SerialNumber))
                            {
                            foundMatch = true;

                            // update the state if needed.
                            if (device.State != newDevice.State)
                                {
                                device.State = newDevice.State;
                                device.OnStateChanged(EventArgs.Empty);

                                // if the device just got ready/online, we need to start monitoring it.
                                if (device.IsOnline)
                                    {
                                    OnDeviceTransitionToOnline(device);
                                    }
                                }

                            // remove the new device from the list since it's been used
                            newCurrentDevices.RemoveAt(dd);
                            break;
                            }
                        }

                    if (!foundMatch)
                        {
                        // the device is gone, we need to remove it, and keep current index to process the next one.
                        this.Devices.Remove(device);
                        if (device.State == DeviceState.Online)
                            {
                            device.State = DeviceState.Offline;
                            device.OnStateChanged(EventArgs.Empty);
                            this.bridge?.OnDeviceDisconnected(new DeviceEventArgs(device));
                            }
                        }
                    else
                        {
                        // process the next one
                        d++;
                        }
                    }

                // At this point we should still have some new devices in newList, so we process them.
                // These are the devices that we are not yet monitoring
                foreach (Device newDevice in newCurrentDevices)
                    {
                    this.Devices.Add(newDevice);
                    if (newDevice.State == DeviceState.Online)
                        {
                        OnDeviceTransitionToOnline(newDevice);
                        }
                    }
                }
            }

        // Do what we need to do when we detect a device making its transition to the online state
        private void OnDeviceTransitionToOnline(Device device)
            {
            this.bridge?.OnDeviceConnected(new DeviceEventArgs(device));
            QueryNewDeviceForInfo(device);
            }

        private void QueryNewDeviceForInfo(Device device)
            {
            // TODO: do this in a separate thread.
            try
                {
                // first get the list of properties.
                if (device.State != DeviceState.Offline && device.State != DeviceState.Unknown)
                    {
                    // get environment variables
                    QueryNewDeviceForEnvironmentVariables(device);
                    // instead of getting the 3 hard coded ones, we use mount command and get them all...
                    // if that fails, then it automatically falls back to the hard coded ones.
                    QueryNewDeviceForMountingPoint(device);

                    // now get the emulator Virtual Device name (if applicable).
                    if (device.IsEmulator)
                        {
                        /*EmulatorConsole console = EmulatorConsole.getConsole ( device );
						if ( console != null ) {
							device.AvdName = console.AvdName;
						}*/
                        }
                    }
                }
            catch (IOException)
                {
                // if we can't get the build info, it doesn't matter too much
                }
            }

        private void QueryNewDeviceForEnvironmentVariables(Device device)
            {
            try
                {
                if (device.State != DeviceState.Offline && device.State != DeviceState.Unknown)
                    {
                    device.RefreshEnvironmentVariables();
                    }
                }
            catch (IOException)
                {
                // if we can't get the build info, it doesn't matter too much
                }
            }

        private void QueryNewDeviceForMountingPoint(Device device)
            {
            try
                {
                if (device.State != DeviceState.Offline && device.State != DeviceState.Unknown)
                    {
                    device.RefreshMountPoints();
                    }
                }
            catch (IOException)
                {
                // if we can't get the build info, it doesn't matter too much
                }
            }

        #endregion

        //---------------------------------------------------------------------------------------------------------------
        // Utility
        //---------------------------------------------------------------------------------------------------------------

        /**
         * Reads the length of the next message from a socket.
         * @return  the length, or 0 (zero) if no data is available from the socket.
         */
        private int ReadLength(Socket socket, byte[] buffer)
            {
            string msg = Read(socket, buffer);
            if (msg != null)
                {
                try
                    {
                    int len = int.Parse(msg, System.Globalization.NumberStyles.HexNumber);
                    return len;
                    }
                catch (FormatException)
                    {
                    // we'll throw an exception below.
                    }
                }
            //throw new IOException ( "unable to read data length" );
            // we receive something we can't read. It's better to reset the connection at this point.
            return -1;
            }

        /**
         * Reads the specified socket.
         *
         * @exception   IOException Thrown when an IO failure occurred.
         *
         * @return  the data read
         */
        private string Read(Socket socket, byte[] data)
            {
            int count = -1;
            int totalRead = 0;

            while (count != 0 && totalRead < data.Length)
                {
                try
                    {
                    int left = data.Length - totalRead;
                    int buflen = left < socket.ReceiveBufferSize ? left : socket.ReceiveBufferSize;

                    byte[] buffer = new byte[buflen];
                    socket.ReceiveBufferSize = buffer.Length;
                    count = socket.Receive(buffer, buflen, SocketFlags.None);
                    if (count < 0)
                        {
                        throw new IOException("EOF");
                        }
                    else if (count == 0)
                        {
                        }
                    else
                        {
                        Array.Copy(buffer, 0, data, totalRead, count);
                        totalRead += count;
                        }
                    }
                catch (SocketException sex)
                    {
                    if (sex.Message.Contains("connection was aborted"))
                        {
                        // ignore this?
                        return string.Empty;
                        }
                    else
                        {
                        throw new IOException($"No Data to read: {sex.Message}");
                        }
                    }
                }

            return data.GetString(AdbHelper.DEFAULT_ENCODING);
            }

        /**
         * Opens a socket to the ADB server.
         *
         * @return  a connected socket, or null a connection could not be obtained
         */
        private Socket ConnectToServer()
            {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
                {
                socket.Connect(AndroidDebugBridge.SocketAddress);
                socket.NoDelay = true;
                }
            catch (Exception e)
                {
                Log.w(loggingTag, e);
                this.CloseSocket(ref socket, false);
                }
            return socket;
            }
        }
    }
