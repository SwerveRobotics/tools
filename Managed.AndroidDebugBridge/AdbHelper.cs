using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Org.SwerveRobotics.Tools.ManagedADB.Exceptions;
using Org.SwerveRobotics.Tools.ManagedADB.Logs;
using Org.SwerveRobotics.Tools.Util;
using static Org.SwerveRobotics.Tools.ManagedADB.Util;

#pragma warning disable 1591

// services that are supported by adb: https://github.com/android/platform_system_core/blob/master/adb/SERVICES.TXT

namespace Org.SwerveRobotics.Tools.ManagedADB
    {
    public enum TransportType
        {
        Usb,
        Local,
        Any,
        Host
        }

    /**
     * The ADB Helper class.
     *
     * @sa  https://github.com/android/platform_system_core/blob/master/adb/SERVICES.TXT    SERVICES.TXT
     * @sa  https://github.com/android/platform_system_core/blob/master/adb/adb_client.c    adb_client.c
     * @sa  https://github.com/android/platform_system_core/blob/master/adb/adb.c           adb.c
     */

    public class AdbHelper
        {
        //-------------------------------------------------------------------------------------------------------------
        // State
        //-------------------------------------------------------------------------------------------------------------

        private const   string      LOGGING_TAG = "AdbHelper";
        private const   int         WAIT_TIME = 5;

        public  static  string      DEFAULT_ENCODING = "ISO-8859-1";
        public  static  AdbHelper   Instance { get; } = new AdbHelper();

        //-------------------------------------------------------------------------------------------------------------
        // Construction
        //-------------------------------------------------------------------------------------------------------------

        /** Constructor that prevents other than a singleton instance of this class from being created. */
        private AdbHelper()
            {
            }

        //-------------------------------------------------------------------------------------------------------------
        // Operations
        //-------------------------------------------------------------------------------------------------------------

        /**
         * Opens a socket to the indicated device at indicated address and port
         * 
         * @exception   SocketException thrown when the socket parameters are invalid
         * @exception   AdbException    Thrown when an Adb error condition occurs
         *
         * @param   address The address.
         * @param   device  the device to connect to. Can be null in which case the connection will be to the first available
         *                  device.
         * @param   port    The port.
         *
         * @return  A Socket.
         */
        public Socket Open(IPAddress address, IDevice device, int port)
            {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
                {
                s.Connect(address, port);
                s.Blocking = true;
                s.NoDelay = false;

                SetDevice(s, device);

                byte[] req = CreateAdbForwardRequest(null, port);
                Write(s, req);
                AdbResponse resp = ReadAdbResponse(s);
                if (!resp.Okay)
                    {
                    throw new AdbException("connection request rejected");
                    }
                s.Blocking = true;
                }
            catch (Exception)
                {
                s?.Close();
                throw;
                }
            return s;
            }

        public int KillAdb(IPEndPoint address)
            {
            byte[] request = FormAdbRequest("host:kill");
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                socket.Connect(address);
                socket.Blocking = true;
                Write(socket, request);
                AdbResponse resp = ReadAdbResponse(socket);
                if (!resp.IOSuccess || !resp.Okay)
                    {
                    Log.e(LOGGING_TAG, "Got timeout or unhappy response from ADB req: " + resp.Message);
                    socket.Close();
                    return -1;
                    }
                return 0;
                }
            }

        [Obsolete("This is not yet functional")]
        public void Backup(IPEndPoint address)
        // https://github.com/android/platform_system_core/blob/master/adb/backup_service.c
            {
            byte[] request = FormAdbRequest("backup:all");
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                socket.Connect(address);
                socket.Blocking = true;
                Write(socket, request);
                AdbResponse resp = ReadAdbResponse(socket);
                if (!resp.IOSuccess || !resp.Okay)
                    {
                    Log.e(LOGGING_TAG, "Got timeout or unhappy response from ADB req: " + resp.Message);
                    socket.Close();
                    return;
                    }

                byte[] data = new byte[6000];
                int count = -1;
                while (count != 0)
                    {
                    count = socket.Receive(data);
                    Console.Write("received: {0}", count);
                    }
                }
            }

        [Obsolete("This is not yet functional")]
        public void Restore()
            {
            throw new NotImplementedException();
            }

        public int GetAdbServerVersion(IPEndPoint address)
            {
            byte[] request = FormAdbRequest("host:version");
            byte[] reply;
            Socket adbChan = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
                {
                adbChan.Connect(address);
                adbChan.Blocking = true;
                Write(adbChan, request);

                AdbResponse resp = ReadAdbResponse(adbChan);
                if (!resp.IOSuccess || !resp.Okay)
                    {
                    Log.e(LOGGING_TAG, "Got timeout or unhappy response from ADB fb req: " + resp.Message);
                    adbChan.Close();
                    return -1;
                    }

                reply = new byte[4];
                Read(adbChan, reply);

                string lenHex = reply.GetString(DEFAULT_ENCODING);
                int len = int.Parse(lenHex, NumberStyles.HexNumber);

                // the protocol version.
                reply = new byte[len];
                Read(adbChan, reply);

                string sReply = reply.GetString(DEFAULT_ENCODING);
                return int.Parse(sReply, NumberStyles.HexNumber);
                }
            finally
                {
                adbChan?.Close();
                }
            }

        public Socket CreatePassThroughConnection(IPEndPoint endpoint, Device device, int pid)
            {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
                {
                socket.Connect(endpoint);
                socket.NoDelay = true;

                // if the device is not -1, then we first tell adb we're looking to talk to a specific device
                SetDevice(socket, device);

                byte[] req = CreateJdwpForwardRequest(pid);

                Write(socket, req);

                AdbResponse resp = ReadAdbResponse(socket);
                if (!resp.Okay)
                    throw new AdbException("connection request rejected: " + resp.Message); //$NON-NLS-1$
                }
            catch (Exception)
                {
                socket?.Close();
                throw;
                }

            return socket;
            }

        public byte[] CreateAdbForwardRequest(string address, int port)
            {
            string request;

            if (address == null)
                request = "tcp:" + port;
            else
                request = "tcp:" + port + ":" + address;
            return FormAdbRequest(request);
            }

        public byte[] FormAdbRequest(string req)
            {
            string resultStr = $"{req.Length.ToString("X4")}{req}\n";
            byte[] result;
            try
                {
                result = resultStr.GetBytes(DEFAULT_ENCODING);
                }
            catch (EncoderFallbackException efe)
                {
                Log.e(LOGGING_TAG, efe);
                return null;
                }
            Debug.Assert(result.Length == req.Length + 5, string.Format("result: {1}{0}\nreq: {3}{2}", result.Length, result.GetString(DEFAULT_ENCODING), req.Length, req));
            return result;
            }

        //----------------------------------------------------------------------------------------------------
        // Low level reading and writing
        //----------------------------------------------------------------------------------------------------

        /**
         * @exception   AdbException    Thrown when an Adb error condition occurs.
         * @exception   SocketException Thrown when a Socket error condition occurs.
         * @exception   ObjectDisposedException                              
         */
        public void Write(Socket socket, byte[] data)
            {
            try
                {
                Write(socket, data, -1, DdmPreferences.Timeout);
                }
            catch (Exception e)
                {
                Log.e(LOGGING_TAG, e);
                throw;
                }
            }

        /**
         * Only socket writing routine in all of Org.SwerveRobotics.Tools.ManagedADB
         *
         * @exception   AdbException    Thrown when an Adb error condition occurs.
         * @exception   SocketException Thrown when a Socket error condition occurs.
         * @exception   ObjectDisposedException                              
         *
         * @param   socket  The socket.
         * @param   data    The data.
         * @param   length  The length.
         * @param   timeout The timeout.
         */
        public void Write(Socket socket, byte[] data, int length, int timeout)
            {
            try
                {
                int numWaits = 0;
                int count = socket.Send(data, 0, length != -1 ? length : data.Length, SocketFlags.None);
                if (count < 0)
                    {
                    throw new EndOfFileException();
                    }
                else if (count == 0)
                    {
                    // TODO: need more accurate timeout?
                    if (timeout != 0 && numWaits*WAIT_TIME > timeout)
                        {
                        throw new AdbException("timeout");
                        }

                    // non-blocking spin
                    Thread.Sleep(WAIT_TIME);
                    numWaits++;
                    }
                else
                    {
                    numWaits = 0;
                    }
                }
            catch (SocketException sex)
                {
                ConsoleTraceError(sex);
                throw;
                }
            }

        public AdbResponse ReadAdbResponse(Socket socket)
            {
            return ReadAdbResponseInternal(socket, false, false);
            }
        public AdbResponse ReadAdbResponseWithDiagnosis(Socket socket)
            {
            return ReadAdbResponseInternal(socket, false, true);
            }
        public AdbResponse ReadAdbResponseNoLogging(Socket socket)
            {
            return ReadAdbResponseInternal(socket, true, false);
            }

        // Read and parse a response from the ADB server. Throw if we don't
        // get enough data from the server to form a response
        public AdbResponse ReadAdbResponseInternal(Socket socket, bool suppressLogging, bool readDiagString)
            {
            AdbResponse resp = new AdbResponse();

            byte[] reply = new byte[4];
            Read(socket, reply);
            resp.IOSuccess      = true;
            resp.Okay           = IsOkay(reply);
            if (resp.Okay)
                readDiagString = true; // look for a reason after the FAIL

            // not a loop -- use "while" so we can use "break"
            while (readDiagString)
                {
                // length string is in next 4 bytes
                byte[] lenBuf = new byte[4];
                Read(socket, lenBuf);

                string lenStr = ReplyToString(lenBuf);

                int len;
                try
                    {
                    len = int.Parse(lenStr, NumberStyles.HexNumber);
                    }
                catch (FormatException)
                    {
                    Log.e(LOGGING_TAG, "Expected digits, got '{0}' : {1} {2} {3} {4}", lenStr, lenBuf[0], lenBuf[1], lenBuf[2], lenBuf[3]);
                    Log.e(LOGGING_TAG, "reply was {0}", ReplyToString(reply));
                    break;
                    }

                byte[] msg = new byte[len];
                Read(socket, msg);

                resp.Message = ReplyToString(msg);
                if (!suppressLogging)
                    Log.e(LOGGING_TAG, "Got reply '{0}', diag='{1}'", ReplyToString(reply), resp.Message);

                break;
                }

            return resp;
            }

        // Read up until the next newline, or until EOF
        public string ReadLine(Socket socket)
            {
            StringBuilder result = new StringBuilder();
            try 
                {
                for (;;)
                    {
                    byte[] data = new byte[1];
                    Read(socket, data);
                    char ch = (char) data[0];
                    if (ch == '\n')
                        break;
                    result.Append(ch);
                    }
                }
            catch (EndOfFileException)
                {
                }
            return result.ToString();
            }

        // Read enough data to fill the buffer. Throw
        // if we don't get enough to do that, for whatever reason
        public void Read(Socket socket, byte[] data)
            {
            Read(socket, data, -1, DdmPreferences.Timeout);
            }

        // Read length bytes into the buffer. Throw if we don't
        // get enough data to do that, for whatever reason
        public void Read(Socket socket, byte[] data, int length, int timeout)
            {
            int cbExpected = length >=0 ? length : data.Length;
            int cbRead = 0;

            while (cbRead < cbExpected)
                {
                try
                    {
                    int cbRemaining = cbExpected - cbRead;
                    int cbBuffer = cbRemaining < socket.ReceiveBufferSize ? cbRemaining : socket.ReceiveBufferSize;

                    byte[] buffer = new byte[cbBuffer];
                    socket.ReceiveBufferSize = cbExpected;
                    int cbReceived = socket.Receive(buffer, cbBuffer, SocketFlags.None);
                    if (cbReceived < 0)
                        {
                        throw new EndOfFileException();
                        }
                    else if (cbReceived == 0)
                        {
                        throw new EndOfFileException();
                        }
                    else
                        {
                        Array.Copy(buffer, 0, data, cbRead, cbReceived);
                        cbRead += cbReceived;
                        }
                    }
                catch (SocketException)
                    {
                    throw;  // we don't actually suppress the socket read errors
                    }
                }
            }

        private byte[] CreateJdwpForwardRequest(int pid)
            {
            string req = $"jdwp:{pid}";
            return FormAdbRequest(req);
            }

        public bool CreateForward(IPEndPoint adbSockAddr, Device device, int localPort, int remotePort)
            {
            Socket adbChan = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
                {
                adbChan.Connect(adbSockAddr);
                adbChan.Blocking = true;

                // host-serial should be different based on the transport...
                byte[] request = FormAdbRequest($"host-serial:{device.SerialNumber}:forward:tcp:{localPort};tcp:{remotePort}");

                Write(adbChan, request);

                AdbResponse resp = ReadAdbResponse(adbChan);
                if (!resp.IOSuccess || !resp.Okay)
                    {
                    throw new AdbException("Device rejected command: " + resp.Message);
                    }
                }
            finally
                {
                adbChan?.Close();
                }

            return true;
            }

        public void ListForward(IPEndPoint address, Device device)
            {
            throw new NotImplementedException();
            }

        public bool RemoveForward(IPEndPoint address, Device device, int localPort)
            {
            using (Socket socket = ExecuteRawSocketCommand(address, device, "host-serial:{0}:killforward:tcp:{1}".With(device.SerialNumber, localPort)))
                {
                // do nothing...
                return true;
                }
            }

        public bool RemoveAllForward(IPEndPoint address, Device device)
            {
            using (Socket socket = ExecuteRawSocketCommand(address, device, "host-serial:{0}:killforward-all".With(device.SerialNumber)))
                {
                // do nothing...
                return true;
                }
            }

        public bool IsOkay(byte[] reply)
            {
            return reply.GetString().Equals("OKAY");
            }

        public string ReplyToString(byte[] reply)
            {
            string result;
            try
                {
                result = Encoding.Default.GetString(reply);
                }
            catch (DecoderFallbackException uee)
                {
                Log.e(LOGGING_TAG, uee);
                result = "";
                }
            return result;
            }

        public List<Device> GetDevices(IPEndPoint address)
            {
            // -l will return additional data
            using (Socket socket = ExecuteRawSocketCommand(address, "host:devices-l"))
                {
                byte[] reply = new byte[4];
                Read(socket, reply);

                string lenHex = reply.GetString(Encoding.Default);
                int len = int.Parse(lenHex, NumberStyles.HexNumber);

                reply = new byte[len];
                Read(socket, reply);

                string[] data = reply.GetString(Encoding.Default).Split(new[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
                return data.Select(Device.CreateFromAdbData).ToList();
                }
            }

        public RawImage GetFrameBuffer(IPEndPoint adbSockAddr, IDevice device)
            {
            RawImage imageParams = new RawImage();
            byte[] request = FormAdbRequest("framebuffer:"); //$NON-NLS-1$
            byte[] nudge = { 0 };
            byte[] reply;

            Socket adbChan = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
                {
                adbChan.Connect(adbSockAddr);
                adbChan.Blocking = true;

                // if the device is not -1, then we first tell adb we're looking to talk
                // to a specific device
                SetDevice(adbChan, device);
                Write(adbChan, request);

                AdbResponse resp = ReadAdbResponse(adbChan);
                if (!resp.IOSuccess || !resp.Okay)
                    {
                    Log.v(LOGGING_TAG, "Got timeout or unhappy response from ADB fb req: " + resp.Message);
                    adbChan.Close();
                    return null;
                    }

                // first the protocol version.
                reply = new byte[4];
                Read(adbChan, reply);
                BinaryReader buf;
                int version = 0;
                using (MemoryStream ms = new MemoryStream(reply))
                    {
                    buf = new BinaryReader(ms);
                    version = buf.ReadInt16();
                    }

                // get the header size (this is a count of int)
                int headerSize = RawImage.GetHeaderSize(version);
                // read the header
                reply = new byte[headerSize*4];
                Read(adbChan, reply);
                using (MemoryStream ms = new MemoryStream(reply))
                    {
                    buf = new BinaryReader(ms);

                    // fill the RawImage with the header
                    if (imageParams.ReadHeader(version, buf) == false)
                        {
                        Log.v(LOGGING_TAG, "Unsupported protocol: " + version);
                        return null;
                        }
                    }

                Log.d(LOGGING_TAG, "image params: bpp=" + imageParams.Bpp + ", size="
                    + imageParams.Size + ", width=" + imageParams.Width
                    + ", height=" + imageParams.Height);

                Write(adbChan, nudge);

                reply = new byte[imageParams.Size];
                Read(adbChan, reply);
                imageParams.Data = reply;
                }
            finally
                {
                adbChan?.Close();
                }

            return imageParams;
            }

        public void ExecuteRemoteRootCommand(IPEndPoint endPoint, string command, Device device, IShellOutputReceiver rcvr)
            {
            ExecuteRemoteRootCommand(endPoint, $"su -c \"{command}\"", device, rcvr, int.MaxValue);
            }

        public void ExecuteRemoteRootCommand(IPEndPoint endPoint, string command, Device device, IShellOutputReceiver rcvr, int maxTimeToOutputResponse)
            {
            ExecuteRemoteCommand(endPoint, $"su -c \"{command}\"", device, rcvr);
            }

        public void ExecuteRemoteCommand(IPEndPoint endPoint, string command, Device device, IShellOutputReceiver rcvr, int maxTimeToOutputResponse)
            {
            using (Socket socket = ExecuteRawSocketCommand(endPoint, device, "shell:{0}".With(command)))
                {
                socket.ReceiveTimeout = maxTimeToOutputResponse;
                socket.SendTimeout = maxTimeToOutputResponse;

                try
                    {
                    byte[] data = new byte[16384];
                    int count = -1;
                    while (count != 0)
                        {
                        if (rcvr != null && rcvr.IsCancelled)
                            {
                            Log.v(LOGGING_TAG, "execute: cancelled");
                            throw new OperationCanceledException();
                            }

                        count = socket.Receive(data);
                        if (count == 0)
                            {
                            // we're at the end, we flush the output
                            rcvr.Flush();
                            Log.v(LOGGING_TAG, "execute '" + command + "' on '" + device + "' : EOF hit. Read: " + count);
                            }
                        else
                            {
                            string[] cmd = command.Trim().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                            string sdata = data.GetString(0, count, DEFAULT_ENCODING);

                            string sdataTrimmed = sdata.Trim();
                            if (sdataTrimmed.EndsWith($"{cmd[0]}: not found"))
                                {
                                Log.v(LOGGING_TAG, "The remote execution returned: '{0}: not found'", cmd[0]);
                                throw new FileNotFoundException($"The remote execution returned: '{cmd[0]}: not found'");
                                }

                            if (sdataTrimmed.EndsWith("No such file or directory"))
                                {
                                Log.v(LOGGING_TAG, "The remote execution returned: {0}", sdataTrimmed);
                                throw new FileNotFoundException($"The remote execution returned: {sdataTrimmed}");
                                }

                            // for "unknown options"
                            if (sdataTrimmed.Contains("Unknown option"))
                                {
                                Log.v(LOGGING_TAG, "The remote execution returned: {0}", sdataTrimmed);
                                throw new UnknownOptionException(sdataTrimmed);
                                }

                            // for "aborting" commands
                            if (sdataTrimmed.IsMatch("Aborting.$"))
                                {
                                Log.v(LOGGING_TAG, "The remote execution returned: {0}", sdataTrimmed);
                                throw new CommandAbortingException(sdataTrimmed);
                                }

                            // for busybox applets 
                            // cmd: applet not found
                            if (sdataTrimmed.IsMatch("applet not found$") && cmd.Length > 1)
                                {
                                Log.v(LOGGING_TAG, "The remote execution returned: '{0}'", sdataTrimmed);
                                throw new FileNotFoundException($"The remote execution returned: '{sdataTrimmed}'");
                                }

                            // checks if the permission to execute the command was denied.
                            // workitem: 16822
                            if (sdataTrimmed.IsMatch("(permission|access) denied$"))
                                {
                                Log.v(LOGGING_TAG, "The remote execution returned: '{0}'", sdataTrimmed);
                                throw new PermissionDeniedException($"The remote execution returned: '{sdataTrimmed}'");
                                }

                            // Add the data to the receiver
                            if (rcvr != null)
                                {
                                rcvr.AddOutput(data, 0, count);
                                }
                            }
                        }
                    }
                catch (SocketException)
                    {
                    throw new ShellCommandUnresponsiveException();
                    }
                finally
                    {
                    rcvr.Flush();
                    }
                }
            }

        public void ExecuteRemoteCommand(IPEndPoint endPoint, string command, Device device, IShellOutputReceiver rcvr)
            {
            ExecuteRemoteCommand(endPoint, command, device, rcvr, int.MaxValue);
            }

        public void SetDevice(Socket adbChan, IDevice device)
            {
            // if the device is not null, then we first tell adb we're looking to talk to a specific device
            if (device != null)
                {
                string msg = "host:transport:" + device.SerialNumber;
                byte[] device_query = FormAdbRequest(msg);

                Write(adbChan, device_query);

                AdbResponse resp = ReadAdbResponseNoLogging(adbChan);
                if (!resp.Okay)
                    {
                    if (equalsIgnoreCase("device not found", resp.Message))
                        {
                        throw new DeviceNotFoundException(device.SerialNumber);
                        }
                    else
                        {
                        throw new AdbException("device (" + device + ") request rejected: " + resp.Message);
                        }
                    }
                }
            }

        public void RunEventLogService(IPEndPoint address, Device device, LogReceiver rcvr)
            {
            RunLogService(address, device, "events", rcvr);
            }

        public void RunLogService(IPEndPoint address, Device device, string logName, LogReceiver rcvr)
            {
            using (Socket socket = ExecuteRawSocketCommand(address, device, "log:{0}".With(logName)))
                {
                byte[] data = new byte[16384];
                using (MemoryStream ms = new MemoryStream(data))
                    {
                    int offset = 0;

                    while (true)
                        {
                        int count;
                        if (rcvr != null && rcvr.IsCancelled)
                            {
                            break;
                            }
                        byte[] buffer = new byte[4*1024];

                        count = socket.Receive(buffer);
                        if (count < 0)
                            {
                            break;
                            }
                        else if (count == 0)
                            {
                            try
                                {
                                Thread.Sleep(WAIT_TIME*5);
                                }
                            catch (ThreadInterruptedException)
                                {
                                }
                            }
                        else
                            {
                            ms.Write(buffer, offset, count);
                            offset += count;
                            if (rcvr != null)
                                {
                                byte[] d = ms.ToArray();
                                rcvr.ParseNewData(d, 0, d.Length);
                                }
                            }
                        }
                    }
                }
            }

        public void Reboot(IPEndPoint adbSocketAddress, Device device)
            {
            Reboot(string.Empty, adbSocketAddress, device);
            }

        public void Reboot(string into, IPEndPoint adbSockAddr, Device device)
            {
            byte[] request = string.IsNullOrEmpty(@into) ? FormAdbRequest("reboot:") : FormAdbRequest("reboot:" + @into);

            using (ExecuteRawSocketCommand(adbSockAddr, device, request))
                {
                // nothing to do...
                }
            }

        public void TcpIp(int port, IPEndPoint adbSockAddr, Device device)
            {
            byte[] request = FormAdbRequest($"tcpip:{port}");
            using (Socket socket = ExecuteRawSocketCommand(adbSockAddr, device, request))
                {
                // Listen for the positive response. We 
                string response = ReadLine(socket);
                string expectedResponsePrefix = "restarting in TCP mode".ToLowerInvariant();
                string responsePrefix = response.Substring(0, Math.Min(response.Length, expectedResponsePrefix.Length)).ToLowerInvariant();
                if (string.IsNullOrEmpty(response) || expectedResponsePrefix != responsePrefix)
                    {
                    // We don't reliably get a response?
                    // throw new AdbException("device probably failed to restart in TCPIP mode");
                    }
                }
            }

        public bool Connect(string hostNameOrAddress, int port, IPEndPoint adbSockAddr)
        // Returns success/fail
            {
            bool result = false;

            string addressAndPort = $"{hostNameOrAddress}:{port}";
            byte[] request = FormAdbRequest($"host:connect:{addressAndPort}");
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                socket.Connect(adbSockAddr);
                socket.Blocking = true;
                Write(socket, request);

                // The response is a length-prefixed OKAY string
                AdbResponse resp = ReadAdbResponseWithDiagnosis(socket);
                if (resp.IOSuccess && resp.Okay)
                    {
                    // The two 'success' cases are
                    //      "already connected to %s"
                    //      "connected to %s"
                    result = "already connected to".IsPrefixOf(resp.Message) || "connected to".IsPrefixOf(resp.Message);
                    }
                }

            return result;
            }

        private Socket ExecuteRawSocketCommand(IPEndPoint address, Device device, string command)
            {
            return ExecuteRawSocketCommand(address, device, FormAdbRequest(command));
            }

        private Socket ExecuteRawSocketCommand(IPEndPoint address, string command)
            {
            return ExecuteRawSocketCommand(address, FormAdbRequest(command));
            }

        private Socket ExecuteRawSocketCommand(IPEndPoint address, byte[] command)
            {
            return ExecuteRawSocketCommand(address, null, command);
            }

        private Socket ExecuteRawSocketCommand(IPEndPoint address, Device device, byte[] command)
            {
            if (device != null && !device.IsOnline)
                throw new AdbException("Device is offline");

            Socket adbChan = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            adbChan.Connect(address);
            adbChan.Blocking = true;
            if (device != null)
                {
                SetDevice(adbChan, device);
                }
            Write(adbChan, command);

            AdbResponse resp = ReadAdbResponse(adbChan);
            if (!resp.IOSuccess || !resp.Okay)
                {
                throw new AdbException("Device rejected command: {0}".With(resp.Message));
                }
            return adbChan;
            }

        private string HostPrefixFromDevice(IDevice device)
            {
            switch (device.TransportType)
                {
            case TransportType.Host:
                return "host-serial";
            case TransportType.Usb:
                return "host-usb";
            case TransportType.Local:
                return "host-local";
            case TransportType.Any:
            default:
                return "host";
                }
            }
        }
    }