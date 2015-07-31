//
// Connection.cs
//
// Infrastructure for connecting to NXT's in various ways: Bluetooth, USB, and Samantha.
//
// http://www.ftdichip.com/Support/Documents/AppNotes/AN_152_Detecting_USB_%20Device_Insertion_and_Removal.pdf
// https://support.microsoft.com/en-us/kb/171890
// https://social.msdn.microsoft.com/Forums/vstudio/en-US/6a355e69-16d4-4588-90dc-fbe0736aaa53/equivalent-of-wndproc-in-c-windows-service?forum=csharpgeneral
// http://www.bing.com/search?q=wndproc+service&FORM=AWRE
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
// using Excel = Microsoft.Office.Interop.Excel;

namespace Org.SwerveRobotics.Tools.Library
    {
    //------------------------------------------------------------------------------------------------
    // ThreadContext
    //
    // Help for running dedicated worker threads.
    //------------------------------------------------------------------------------------------------

    public class ThreadContext
        {
        public ParameterizedThreadStart StartFunction  = null;
        public Thread                   Thread         = null;
        public bool                     StopRequest    = false;
        public EventWaitHandle          StopEvent      = new EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);

        public ThreadContext(ParameterizedThreadStart start)
            {
            this.StartFunction = start;
            }

        public void Start()
            {
            if (this.Thread == null)
                {
                this.Thread = new Thread((ctx) => ((ThreadContext)ctx).ThreadRoot());
                this.StopRequest = false;
                this.StopEvent.Reset();
                this.Thread.Start(this);
                }
            }
        public void Stop()
            {
            if (this.Thread != null)
                {
                this.StopRequest = true;
                this.StopEvent.Set();
                this.Thread.Join();
                this.Thread = null;
                }
            }

        private void ThreadRoot()
            {
            System.Threading.Thread.CurrentThread.SetApartmentState(ApartmentState.MTA);
            this.StartFunction(this);
            }
        };

    //------------------------------------------------------------------------------------------------
    // KnownNXT
    //
    // A wrapper around a connection over which we can communicate with a NXT that we know about
    //------------------------------------------------------------------------------------------------

    public class KnownNXT
        {
        //--------------------------------------------------------------------------
        // Types
        //--------------------------------------------------------------------------

        public enum CONNECTIONTYPE { BLUETOOTH, USB, IP };

        //--------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------

        public bool?  HasNXTConnected    = null;
               string nxtName            = "";
        public string NxtName { get { return nxtName==null ? "(unknown NXT)" : nxtName; } set { nxtName = value; }}
        
        public string ListBoxDisplayName { get { 
        // for use in listbox UI
            string status = (HasNXTConnected==true ? "" : (HasNXTConnected==false ? "not active" : "status unknown"));
            string suffix = (status.Length>0 ? ": " + status: "");
            switch (this.ConnectionType)
                {
            case CONNECTIONTYPE.BLUETOOTH:  
            case CONNECTIONTYPE.IP:         return this.NxtName + " on " + this.sConnectionParameter + suffix;
            case CONNECTIONTYPE.USB:        return this.NxtName + " on USB" + suffix;
            //
            default: return Util.Fail<string>(); 
                }
            }}   

        string                       sConnectionParameter = "COM1";
        public CONNECTIONTYPE        ConnectionType       = CONNECTIONTYPE.BLUETOOTH;
        public Connection            Connection;

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public KnownNXT(CONNECTIONTYPE connectionType, string sConnectionParameter, string nxtName=null, bool? hasNXTConnected = null)
            {
            this.ConnectionType       = connectionType;
            this.sConnectionParameter = sConnectionParameter;
            this.NxtName              = nxtName;
            this.HasNXTConnected      = hasNXTConnected;
            }

        public static void AddKnownNXT(List<KnownNXT> list, KnownNXT knownNXT)
            {
            foreach (KnownNXT existing in list)
                {
                if (existing.Equals(knownNXT))
                    return;
                }
            list.Add(knownNXT);
            }

        //--------------------------------------------------------------------------
        // Comparison
        //--------------------------------------------------------------------------

        public override bool Equals(object obj)
            {
            if (obj != null && obj.GetType() == this.GetType())
                {
                KnownNXT him = obj as KnownNXT;
                if (him.ConnectionType == this.ConnectionType)
                    {
                    if (him.sConnectionParameter == this.sConnectionParameter)
                        {
                        return true;
                        }
                    }
                }
            return false;
            }

        public override int GetHashCode()
            {
            return this.sConnectionParameter.GetHashCode();
            }

        //--------------------------------------------------------------------------
        // Communication
        //--------------------------------------------------------------------------

        public bool IsOpen { get { return this.Connection != null && this.Connection.IsOpen; }}

        Connection NewConnection()
            {
            switch (this.ConnectionType)
                {
            case CONNECTIONTYPE.BLUETOOTH:  return (new BluetoothConnection(this.sConnectionParameter));
            case CONNECTIONTYPE.USB:        return (new USBConnection(this.sConnectionParameter));
            case CONNECTIONTYPE.IP:         return (new IPConnection(this.sConnectionParameter));
            //
            default: return Util.Fail<Connection>(); 
                }
            }

        void Open()
        // Idempotent
            {
            if (!this.IsOpen)
                {
                try {
                    this.Connection = this.NewConnection();
                    this.Connection.Open();
                    }
                catch (System.TimeoutException)
                    {
                    this.Connection.Close();    // REVIEW: more of an error message?
                    this.Connection = null;
                    }
                }
            }

        void Close()
        // Idempotent
            {
            if (this.IsOpen)
                {
                this.Connection.Close();
                this.Connection = null;
                }
            }

        public void Run(bool fUseJoystick)
        // Idempotent
            {
            //Program.CurrentNXT = this;
            ////
            //this.Open();
            //Program.TheForm.timerJoystickTransmission.Enabled = (fUseJoystick && JoystickController.HasControllers());
            }

        public void Stop()
        // Idempotent
            {
            //Program.TheForm.timerJoystickTransmission.Enabled = false;
            //Program.TheForm.ConnectionWantsTelemetryPolling = false;
            //this.Close();
            ////
            //Program.CurrentNXT = null;
            }

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public bool ProbeForNXT()
        // Quickly see if there a live NXT at the other end; answer true or false accordingly
            {
            if (null == this.HasNXTConnected)
                {
                bool fResult = false;
                //
                Connection cxn = this.NewConnection();
                //
                string nxtName;
                fResult = cxn.ProbeForNXT(out nxtName);
                if (fResult)
                    {
                    this.HasNXTConnected = true;
                    if (nxtName.Length > 0)
                        {
                        this.NxtName = nxtName;
                        }
                    }
                else
                    this.HasNXTConnected = false;
                }
            //
            return (bool)this.HasNXTConnected;
            }

        public static int Compare(KnownNXT left, KnownNXT right)
            {
            return String.Compare(left.NxtName, right.NxtName);
            }

        // Return the names serial ports on this computer (likely) have a NXT attached
        public static List<KnownNXT> KnownNXTs { get {
            //
            List<KnownNXT> result = new List<KnownNXT>();
            //
            //TelemetryFTCUI.ShowWaitCursorWhile(() =>
            //    {
            //    // NB: at the moment, we can't actually successfully send telemetry out over
            //    // Samantha modules due to the fact that they can't parse the PollCommand
            //    // messages correctly, so we don't search for them.
            //    // result.AddRange(IPConnection.GetAvailableSamanthaConnectedNXTs());

            //    // USB and Bluetooth connections work fine, though
            //    result.AddRange(USBConnection.FindDeviceFromGuid(USBConnection.guidNxtMindstormsDeviceInstance));
            //    result.AddRange(BluetoothConnection.GetNXTBluetoothSerialPortNames());
            //    });
            //
            return result;
            }}

        }

    //------------------------------------------------------------------------------------------------
    // Connection
    //------------------------------------------------------------------------------------------------

    public abstract class Connection : WIN32, IDisposable
        {
        //--------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------
                  List<Byte>                rgbIncomingPolledData = new List<byte>();
        protected List<TelemetryRecord>     telemetryRecords      = new List<TelemetryRecord>();
        protected List<NxtMessage>          msgReplyTargets   = new List<NxtMessage>();
        protected bool                      fDisposed         = false;
        protected List<byte>                rgbBytesAvailable = new List<byte>();
        protected ThreadContext             readThread        = null;
        protected EventWaitHandle           readThreadRunning = new EventWaitHandle(false, EventResetMode.ManualReset);

        public List<TelemetryRecord>        TelemetryRecords  { get { return this.telemetryRecords; } }
        public EventWaitHandle              TelemetryRecordAvailableEvent  = new EventWaitHandle(false, System.Threading.EventResetMode.AutoReset);
        public Semaphore                    MessageToSendSemaphore = new Semaphore(0, int.MaxValue);

        ThreadContext                       postTelemetryMessagesThread;
        ThreadContext                       sendNxtMessagesThread;
        List<NxtMessage>                    nxtMessagesToSend = new List<NxtMessage>();

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        ~Connection()
            {
            Dispose(false);
            }

        void IDisposable.Dispose()
            {
            Dispose(true);
            GC.SuppressFinalize(this);
            }

        protected virtual void Dispose(bool fDisposing)
            {
            if (!this.fDisposed)
                {
                if (fDisposing)
                    {
                    // Free other state (managed objects).
                    }
                
                // Free own state (unmanaged objects)
                // Set large fields to null

                this.fDisposed = true;
                }
            }

        public abstract bool IsOpen { get; }
        public virtual bool Open(bool fTraceFailure=true)
            {
            if (null == this.sendNxtMessagesThread)
                {
                this.sendNxtMessagesThread = new ThreadContext((ctx) => SendNxtMessagesThread((ThreadContext)ctx));
                this.sendNxtMessagesThread.Start();
                }
            //
            if (null == this.postTelemetryMessagesThread)
                {
                this.postTelemetryMessagesThread = new ThreadContext((ctx) => PostTelemetryRecordsThread((ThreadContext)ctx));
                this.postTelemetryMessagesThread.Start();
                }
            //
            return true;
            }
        public virtual void Close()
            {
            if (null != this.sendNxtMessagesThread) this.sendNxtMessagesThread.Stop();
            if (null != this.postTelemetryMessagesThread) this.postTelemetryMessagesThread.Stop();

            this.sendNxtMessagesThread = null;
            this.postTelemetryMessagesThread = null;
            }

        //--------------------------------------------------------------------------
        // Utility
        //--------------------------------------------------------------------------

        public virtual bool ProbeForNXT(out string sNxtName)
            {
            bool fResult = false;
            sNxtName = "(unknown NXT)";

            Util.TraceDebug("opening {0}...", this);
            if (this.Open(false))
                {
                Util.TraceDebug("...opened {0}", this);
                //
                try {
                    GetDeviceInfoNxtMessage msg = new GetDeviceInfoNxtMessage();
                    if (this.Send(msg))
                        {
                        if (msg.AwaitReply())
                            {
                            fResult = true;
                            sNxtName = msg.NxtName;
                            }
                        else
                            {
                            Util.TraceDebug("...no reply");
                            }
                        }
                    }
                catch (System.TimeoutException)
                    {
                    }
                //
                this.Close();
                }
            else
                {
                Util.TraceDebug("...failed to open {0}", this);
                }

            return fResult;
            }

        //--------------------------------------------------------------------------
        // Data transmission
        //--------------------------------------------------------------------------

        public void SendJoystickMessage()
        // NOTE: must be called on the main thread because of joystick state acquisition needs
            {
            JoystickControllersAndGameControlNxtMessage msg = new JoystickControllersAndGameControlNxtMessage();  // captures current state of joystick
            this.QueueMessageForSending(msg);
            }

        public void SendTelemetryPollMessage()
            {
            PollNxtMessage msg = new PollNxtMessage(POLLTYPE.PollBuffer);
            this.QueueMessageForSending(msg);
            }

        public DeleteUserFlashNxtMessage SendDeleteUserFlashMessage()
            {
            DeleteUserFlashNxtMessage msg = new DeleteUserFlashNxtMessage();
            this.QueueMessageForSending(msg);
            return msg;
            }

        public void SendGetDeviceInfoMessage()
            {
            GetDeviceInfoNxtMessage msg = new GetDeviceInfoNxtMessage();
            this.QueueMessageForSending(msg);
            }

        public void QueryAvailableNXTData()
        // Dump status on what the NXT has available for us to read
            {
            /* find out how much poll data is available */
            PollLengthNxtMessage pollLength = new PollLengthNxtMessage(POLLTYPE.PollBuffer);
            this.QueueMessageForSending(pollLength);

            pollLength = new PollLengthNxtMessage(POLLTYPE.HighSpeedBuffer);
            this.QueueMessageForSending(pollLength);

            /* read each of the mailbox queues */
            for (byte mailbox=0; mailbox < 20; mailbox++)
                {
                NxtMessage msg = new MessageReadNxtMessage(mailbox);
                this.QueueMessageForSending(msg);
                }
            }

        //----------------------------------------------------------

        protected NxtMessage GetNextReplyTarget(byte replyCommand)
        // We've received a reply which is replying to a command of type replyCommand.
        // Find the message that we sent that that's a reply to.
            {
            NxtMessage result = null;

            System.DateTime now = System.DateTime.Now;

            lock (this.msgReplyTargets)
                {
                for (int imsg = 0; imsg < this.msgReplyTargets.Count; )
                    {
                    // A bit of robustness: keep going until we find a  message with the same 
                    // command as that of which we have received a reply. While not foolproof, this does
                    // handle the case where we thought the NXT *might* return a reply when in 
                    // fact it chose not to.
                    //
                    if (this.msgReplyTargets[imsg].Command == replyCommand)
                        {
                        // Found the reply
                        result = this.msgReplyTargets[imsg];
                        this.msgReplyTargets.RemoveAt(imsg);
                        break;
                        }
                    if (this.msgReplyTargets[imsg].Deadline < now)
                        {
                        // It's an old, stale, reply, remove it
                        this.msgReplyTargets.RemoveAt(imsg);
                        }
                    else
                        {
                        // Still a fresh reply, but not of the right flavor; move on
                        imsg++;
                        }
                    }
                }
            return result;
            }

        public NxtMessage QueueMessageForSending(NxtMessage msg)
        // Stick this message in the queue to be sent
            {
            lock (this.nxtMessagesToSend)
                {
                this.nxtMessagesToSend.Add(msg);
                this.MessageToSendSemaphore.Release();
                }

            return msg;
            }

        public abstract bool Send(NxtMessage msg);

        void SendNxtMessagesThread(ThreadContext ctx)
            {
            System.Threading.Thread.CurrentThread.Name = "Connection.SendNxtMessagesThread";

            WaitHandle[] waitHandles = new WaitHandle[2];
            waitHandles[0] = this.MessageToSendSemaphore;
            waitHandles[1] = ctx.StopEvent;
            //
            // We throttle the sends a little bit in order to avoid overwhelming a samantha.
            // Without this throttle, QueryAvailableNXTData has been observed to overwhelm the 
            // samantha at times and to drop messages and / or replies (not sure which).
            //
            // However (heuristic): we only throttle sends when we've got a bit of queue backed up
            //
            int msThrottle = (this.msgReplyTargets.Count > 3) ? 3 : 0;
            int msPrev = 0;
            //
            while (!ctx.StopRequest)
                {
                int iWait = WaitHandle.WaitAny(waitHandles);
                //
                switch (iWait)
                    {
                case 0: // MessageToSendSemaphore
                    {
                    NxtMessage msg = null;
                    lock (this.nxtMessagesToSend)
                        {
                        msg = this.nxtMessagesToSend[0];
                        this.nxtMessagesToSend.RemoveAt(0);
                        }
                    //
                    int msNow = System.Environment.TickCount;
                    int dms   = msPrev + msThrottle - msNow;
                    if (dms > 0)
                        {
                        System.Threading.Thread.Sleep(dms);
                        msNow = System.Environment.TickCount;
                        }
                    //
                    this.Send(msg);
                    // Util.Trace("sent {0}", msg);
                    msPrev = msNow;
                    break;
                    }
                case 1: // StopEvent
                    break;
                // end switch
                    }
                }
            }

        //--------------------------------------------------------------------------
        // Data reception
        //--------------------------------------------------------------------------

        public void AddIncomingPolledData(byte[] rgb)
        // We've got some more bytes from polling a NXT. Put those in our queue, and
        // dispatch any that now wholly form a complete record
            {
            rgbIncomingPolledData.AddRange(rgb);
            ProcessIncomingPolledData();
            }

        void ProcessIncomingPolledData()
        // If any of the poll data now form complete records, process them
            {
            while (rgbIncomingPolledData.Count > 0)
                {
                // First byte is bytecount and meta bit
                int cbRecord = rgbIncomingPolledData[0] & 0x7F;
                if (1+cbRecord <= rgbIncomingPolledData.Count)
                    {
                    // Remember the meta bit, discard this byte
                    bool fMetaRecord = (rgbIncomingPolledData[0] & 0x80) != 0;
                    rgbIncomingPolledData.RemoveAt(0);

                    // Accumulate the record proper. REVIEW: could this be faster?
                    byte[] rgbRecord = new byte[cbRecord];
                    for (int ib = 0; ib < cbRecord; ib++)
                        {
                        rgbRecord[ib] = rgbIncomingPolledData[ib]; 
                        }
                    rgbIncomingPolledData.RemoveRange(0, cbRecord);

                    // Process the record
                    ProcessIncomingPolledRecord(fMetaRecord, rgbRecord);
                    }
                else
                    {
                    // Record not fully arrived yet
                    break;
                    }
                }
            }

        // The data stream transmitted from the NXT consists of a sequence of records. Each record is a sequence of 
        // bytes, the first of which consists of:
        //  •	a 7-bit byte count (in the lower 7 bits) indicating the length of the record in bytes (exclusive of this first byte), and
        //  •	a 1-bit flag (the high bit) indicating whether the record is
        //      o	a payload record (high bit is zero), containing app-defined data to be accumulated by the receiver, or
        //      o	a meta record (high bit is one), containing dynamic communication from the NXT to the receiver
        //
        // A meta record of zero length is degenerate and contains no actual communication. A meta record of length 
        // greater than zero has semantics which depend upon the second byte cmd of the record (the byte immediately 
        // following the byte-count/flag byte) according to the following:
        //
        //  •	cmd==0: Polling Interval
        //      o	If length is exactly 3
        //          	Two bytes following cmd are taken as a little-endian unsigned integer.
        //          	Said integer is interpreted as the polling interval in milliseconds that the receiver should 
        //              use between polling requests (see below) by which new data in the data stream is retrieved. 
        //              A value of zero indicates that the receiver should poll as fast as reasonably possible. 
        //          	The default value of the polling interval is 30.
        //      o	If length is exactly 1
        //          	Polling is disabled
        //          	The receiver will not again poll the NXT until the Initialization Sequence is next executed.
        //
        //  •	cmd==1: Backchannel Mailbox
        //      o	If length is exactly 2
        //          	The byte following cmd is interpreted as the zero-origin mailbox number by which the receiver 
        //              can dynamically communicate with the NXT using the Message Write Direct Command.
        //          	Until and unless the NXT so informs the Samantha of this mailbox number, no receiver-to-NXT communication is possible.
        //          	The format and semantics of the receiver -to-NXT communications is defined below in Backchannel Communication
        //
        //  •	cmd==2: Zero Telemetry Data
        //      o	If length is exactly 1
        //          	Stored telemetry data is discarded
        //          	Persistent storage space is prepared for subsequent accumulation of payload records
        //  •	E.g.: Flash memory is zeroed
        //          	Note that in general this can take a significantly long time for the Samantha to execute.
        //  •	However, if no telemetry data is currently stored (i.e: no payload records have been observed since the 
        //      previous zeroing) this operation is relatively quick
        //
        // Any ill-formed meta records (e.g.: cmd==0 and length not 1 or 3, or cmd containing a value other than those 
        // listed above) are ignored by the receiver.

        void ProcessIncomingPolledRecord(bool fMetaRecord, byte[] rgbRecord)
            {
            if (fMetaRecord)
                {
                // Process a meta record: adjust the polling parameters, etc
                //
                if (rgbRecord.Length > 0)
                    {
                    // First byte of record is the telemetry meta command
                    switch (rgbRecord[0])
                        {
                    case (byte)TELEMETRY_META_COMMAND.POLLING_INTERVAL:
                        switch (rgbRecord.Length)
                            {

                        case 1: // disable polling
                            // Program.TheForm.NXTWantsTelemetryPolling = false;
                            break;

                        case 3: // enable polling at certain rate
                            //
                            int msPoll = (int)rgbRecord[1] + ((int)rgbRecord[2] << 8);
                            //Program.TheForm.TelemetryPollingInterval = msPoll;
                            //Program.TheForm.NXTWantsTelemetryPolling = true;
                            break;
                            }
                        break;

                    case (byte)TELEMETRY_META_COMMAND.BACKCHANNEL_MAILBOX:
                        //
                        // NYI. Backchannel has no current use
                        //
                        break;

                    case (byte)TELEMETRY_META_COMMAND.ZERO_TELEMETRY_DATA:
                        //
                        // NYI. Probably not semantically interesting for non-Samantha-telemetry-accumulation situations
                        //
                        break;
                        }
                    }
                }
            else
                {
                // Process an ordinary telemetry data record
                TelemetryRecord record = new TelemetryRecord(rgbRecord);
                lock (this.TelemetryRecords)
                    {
                    this.TelemetryRecords.Add(record);
                    this.TelemetryRecordAvailableEvent.Set(); 
                    }
                }
            }

        void PostTelemetryRecordsThread(ThreadContext ctx)
        // Forward incoming telemetry records on to Microsoft Excel
            {
            System.Threading.Thread.CurrentThread.Name = "Connection.PostTelemetryRecordsThread";

            bool fDone = false;
            //
            WaitHandle[] waitHandles = new WaitHandle[2];
            waitHandles[0] = this.TelemetryRecordAvailableEvent;
            waitHandles[1] = ctx.StopEvent;
            //
            for (; !ctx.StopRequest && !fDone ;)
                {
                int iWait = WaitHandle.WaitAny(waitHandles);

                switch (iWait)
                    {
                case 0: // TelemetryRecordAvailableEvent
                    {
                    // There's one or more records there to read. 
                    // Read all that we can.
                    for (;!fDone;)
                        {
                        TelemetryRecord telemetryRecord = null;
                        lock (this.TelemetryRecords)
                            {
                            if (0 == this.TelemetryRecords.Count)
                                break;
                            telemetryRecord = this.TelemetryRecords[0];
                            this.TelemetryRecords.RemoveAt(0);
                            }
                        telemetryRecord.Parse();
                        //
                        if (telemetryRecord.fEndOfRecordSet)
                            {
                            // If DATUM_TYPE.EOF has been transmitted then subsequent 
                            // telemetry records received will go to a new spreadsheet
                            //
                            // Program.TheForm.DisconnectTelemetryDestination();
                            }
                        else if (telemetryRecord.data.Count == 0)
                            {
                            // Empty records don't do anything
                            }
                        else
                            {
                            // Non-emptry records are posted to the appropriate worksheet
                            //
                            //TelemetryFTCUI.ShowWaitCursorWhile(() =>
                            //    { 
                            //    Program.TheForm.OpenTelemetryDestinationIfNecessary(); 
                            //    });
                            //telemetryRecord.PostToSheet();
                            }
                        }
                    break;
                    }
                case 1: // StopEvent
                    break;
                // end switch
                    }
                }
            }

        //------------------------------------------

        byte PopByte()
            {
            lock (this.rgbBytesAvailable)
                {
                byte result = this.rgbBytesAvailable[0];
                this.rgbBytesAvailable.RemoveAt(0);
                return result;
                }
            }

        public byte ReadByte()
            {
            if (this.rgbBytesAvailable.Count > 0)
                {
                return this.PopByte();
                }
            return InternalReadByte();
            }

        public byte[] ReadBytes(int cb)
            {
            byte[] rgb = new byte[cb];
            ReadBytes(rgb, 0, cb);
            return rgb;
            }

        public void ReadBytes(byte[] rgb, int ib, int cb)
            {
            lock (this.rgbBytesAvailable)
                {
                int cbAvail = Math.Min(cb, rgbBytesAvailable.Count);
                rgbBytesAvailable.CopyTo(0, rgb, ib, cbAvail);
                rgbBytesAvailable.RemoveRange(0, cbAvail);
                cb -= cbAvail;
                ib += cbAvail;
                }

            this.InternalReadBytes(rgb, ib, cb);
            }

        public void SkipBytes(int cb)
            {
            lock (rgbBytesAvailable)
                {
                int cbPutback = Math.Min(cb, rgbBytesAvailable.Count);
                rgbBytesAvailable.RemoveRange(0, cbPutback);
                cb -= cbPutback;
                }
            while (cb-- > 0)
                {
                this.InternalReadByte();
                }
            }

        protected void InternalReadBytes(byte[] rgb, int ib, int cb) { if (cb>0) { throw new NotImplementedException(); }}
        protected byte InternalReadByte()                            { throw new NotImplementedException(); }

        public void PutBack(byte b)
            {
            lock (rgbBytesAvailable)
                {
                this.rgbBytesAvailable.Insert(0, b);
                }
            }

        protected void RecordIncomingData(byte[] rgb, int cb)
            {
            lock (rgbBytesAvailable)
                {
                for (int ib = 0; ib < cb; ib++)         // REVIEW: works, but is slower than I'm sure we can manage to make it if we tried harder
                    {
                    this.rgbBytesAvailable.Add(rgb[ib]);    
                    }
                }
            }

        protected void ProcessIncomingPacket(byte[] rgb, int cb)
            {
            byte bCommandType = rgb[0];
            switch ((COMMAND_TYPE)bCommandType)
                {
            case COMMAND_TYPE.DIRECT_NO_REPLY_REQUIRED:
            // This is the NXT spontaneously transmitting a message
                {
                byte bCommand = rgb[1];    // the 'direct' command
                switch ((DIRECT_COMMAND)bCommand)
                    {
                case DIRECT_COMMAND.MESSAGE_WRITE:
                    //
                    byte mailbox   = rgb[2];    // the mailbox/queue to which the data was sent, should be iTelemetryMailbox. REVIEW: generalize for multiple telemetry streams
                    byte cbPayload = rgb[3];    // the cbDevBroadcastDeviceInterface of the packet that came from the NXT
                    //
                    TelemetryRecord msg = new TelemetryRecord(rgb.Slice(4, cbPayload));
                    lock (this.telemetryRecords)
                        {
                        this.telemetryRecords.Add(msg);
                        }
                    this.TelemetryRecordAvailableEvent.Set();
                    //
                    break;

                default:
                    // Other direct commands ignored
                    Util.TraceDebug("unknown direct command: {0}", bCommand);
                    break;
                    }
                }
                break;

            case COMMAND_TYPE.REPLY:
            // This is the NXT replying to one of our messages
                {
                byte bCommand = rgb[1];
                NxtMessage msgReplyTarget = GetNextReplyTarget(bCommand);
                if (null != msgReplyTarget)
                    {
                    msgReplyTarget.ProcessReply(rgb, this);
                    msgReplyTarget.NoteReplyValid();
                    }
                else
                    {
                    Util.TraceDebug("reply with no matching request");
                    }
                }
                break;

            default:
                // It's a command type we don't know how to process. Just skip the message and hope for the best.
                Util.TraceDebug("unknown command type: {0}", bCommandType);
                break;
                }
            }
        }

    //------------------------------------------------------------------------------------------------
    // IPConnection
    //------------------------------------------------------------------------------------------------

    class IPConnection : Connection
        {
        //--------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------

        IPEndPoint  epSamanthaTCP;      // the Samantha we are talking to; this will have port 2901
        IPEndPoint  epHTTP;             // same as epSamanthaTCP, but port 80 instead
        bool        fHaveExclusive;     // Whether we own the exclusivity of the Samanatha or not
        Socket      socket;             // when non-null, a socket connected to ep

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public IPConnection(string sConnectionParameter)
            {
            this.epSamanthaTCP = new IPEndPoint(IPAddress.Parse(sConnectionParameter), samanthaCommunicationPort);
            this.epHTTP        = new IPEndPoint(IPAddress.Parse(sConnectionParameter), 80);
            this.fHaveExclusive = false;
            }

        protected override void Dispose(bool fDisposing)
            {
            if (!this.fDisposed)
                {
                this.Close();
                }
            base.Dispose(fDisposing);
            }

        public override string ToString()
            {
            return this.epSamanthaTCP.Address.ToString();
            }

        //--------------------------------------------------------------------------
        // Discovery
        //--------------------------------------------------------------------------

        const int samanthaDiscoveryPort     = 30303;
        const int samanthaCommunicationPort = 2901;

        class SamanthaDiscovery
            {
            public bool fCanceled = false;
            }

        public static List<KnownNXT> GetAvailableSamanthaConnectedNXTs()
        // Samantha's implement the 'Microchip discovery standard': we broadcast a UDP packet
        // to port 30303 that has (at least) a capital 'D' as its first byte. Samantha's see
        // this and respond with a unicast packet back to us containing a payload that we 
        // ignore (but see comment in DoReceive below). But the fact that they respond makes
        // us aware of their existence.
        //
        // To communicate with a Samantha, one uses TCP over port 2901 (FWIW: 2901 is the FTC
        // team number of the mentor who wrote the Samantha software). However, before one 
        // does so, one must acquire exclusive access to the module using a series of http
        // requests (see AcquireExclusiveUse below).
        //
        // An interesting (apparent) artifact of how the exclusivity is implemented is that a
        // Samantha which is locked to a particular host IP address will no longer be discoverable
        // (on port 30303) by subsequent requests from that IP host until the host relinquishes 
        // the lock. This is goodness, for otherwise, there'd be issues between two different 
        // applications on that host simultaneously attempting to acquire exclusivity.
        //
        // Useful impl notes: http://www.microchip.com/forums/m475301-print.aspx
            {
            Util.TraceDebug("GetAvailableSamanthaConnectedNXTs");

            List<KnownNXT> result = new List<KnownNXT>();

            IPEndPoint epSend = new IPEndPoint(IPAddress.Broadcast, samanthaDiscoveryPort);
            IPEndPoint epRecv = new IPEndPoint(IPAddress.Any,       samanthaDiscoveryPort);
            byte[] rgbSend = System.Text.Encoding.ASCII.GetBytes("D");

            UdpClient udpClient         = new UdpClient();
            udpClient.EnableBroadcast   = true;
            udpClient.MulticastLoopback = false;
            udpClient.ExclusiveAddressUse = false;
            udpClient.Client.Bind(epRecv);

            int msSendInterval        = 250;
            int msWaitForNXTDiscovery = 1500 - msSendInterval;

            SamanthaDiscovery disc = new SamanthaDiscovery();
            DoReceive(udpClient, rgbSend, result, disc);

            udpClient.Send(rgbSend, rgbSend.Length, epSend);
            System.Threading.Thread.Sleep(msSendInterval);
            udpClient.Send(rgbSend, rgbSend.Length, epSend);

            // Wait a while for NXTs to get back to us. The duration used here has been 
            // only heuristically determined.
            System.Threading.Thread.Sleep(msWaitForNXTDiscovery);

            // Shut down the UDP client, synchronizing with the DoReceive
            lock (disc)
                {
                disc.fCanceled = true;
                udpClient.Close();
                Util.TraceDebug("udpClient closed");
                }
            
            return result;
            }

        static void DoReceive(UdpClient udpClient, byte[] rgbSent, List<KnownNXT> result, SamanthaDiscovery discParam)
            {
            udpClient.BeginReceive((IAsyncResult ar) => { 
                //
                // Util.Trace("IPConnection.DoReceive");
                //
                SamanthaDiscovery disc = ar.AsyncState as SamanthaDiscovery;
                lock (disc)
                    {
                    if (!disc.fCanceled)
                        {
                        // Get the bytes for the incoming packet
                        IPEndPoint epSender = null;
                        byte[] rgbReceived = udpClient.EndReceive(ar, ref epSender);
                
                        // Post another request so we see all responses
                        DoReceive(udpClient, rgbSent, result, discParam);
                
                        // Note that we see our own initial transmission as well as responses from 
                        // actual Samantha modules. We'd like to distinguish the former as being 
                        // send to the broadcast address, but it doesn't appear that we can get that
                        // info here. So we just compare to the packet we sent.

                        if (rgbSent.IsEqualTo(rgbReceived))
                            {
                            // It's just us 
                            Util.TraceDebug("samantha: saw our packet");
                            }
                        else
                            {
                            // It's a Samantha. The address of the Samantha we get from udpClient. The 
                            // payload, though isn't especially useful to us: it appears to be two text lines
                            // followed by a one-byte status code. The first line seems to be the Netbios name
                            // by which we could locate the module, and the second is a text form of the module's
                            // MAC address. Observed values for the status are 'A' for active, and 'O' for
                            // offline. So we'll just snarf the address away and go through the ProbeForNXT logic later
                            // like the other connection types.
                            //
                            // Update: we actually want to dig the name string out and parse it. May as well, just 
                            // in case the ProbeForNXT doesn't find anything.
                            //
                            Util.TraceDebug("samantha: saw {0}", epSender.Address.ToString());

                            string sReceived = (new System.Text.ASCIIEncoding()).GetString(rgbReceived);
                            string[] lines   = sReceived.Lines(StringSplitOptions.RemoveEmptyEntries);

                            // First three chars are 'NXT' prepended to the actual NXT name. So a brick named 'Foo'
                            // will show up as 'NXTFoo'.
                            string sNXT = lines[0].SafeSubstring(3).Trim();

                            lock (result)   // the delegate is called on a worker thread with (to us) unknown concurrency
                                {
                                KnownNXT.AddKnownNXT(result, new KnownNXT(KnownNXT.CONNECTIONTYPE.IP, epSender.Address.ToString(), sNXT));
                                }
                            }
                        }
                    }
                }, discParam);
            }

        //--------------------------------------------------------------------------
        // Connection Management
        //--------------------------------------------------------------------------

        static string Password { get
        // Return the password used by the this app on this user on this machine, creating same if necessary.
            {
            string result = "";

            // The Field Control System uses @"Software\FIRST\samofcs\Preferences", but we don't 
            // want to share that location, as we want a FCS and TelemetryFTC running on the same
            // machine to be locked out against each other. So we just make a small change to the path:
            // @"Software\FIRST\TelemetryFTC\Preferences".
            //
            // Nope: that's not actually necessary, as (see GetAvailableSamanthaConnectedNXTs above)
            // the discovery is disabled if another app on our host already has access. So we revert back
            // to using hte same password, as that makes a more usable system.
            //
            string sPath  = @"Software\FIRST\samofcs\Preferences";

            // Dig out the REG_SZ value under that key.
            string sValue = "Password";
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(sPath))
                {
                result = key.GetValue(sValue) as string;
                if (null == result)
                    {
                    // Value not there; make up a new password and remember it
                    string sPassword = new Random().Next(0, Int32.MaxValue).ToString();
                    key.SetValue(sValue, sPassword, RegistryValueKind.String);
                    result = key.GetValue(sValue) as string;
                    }
                }
            return result;
            }}

        bool AcquireExclusiveUse()
            {
            bool fResult = false;

            // Attempt to acquire exclusive access. The locking is granular to the IP address with which
            // we are communicating with the Samantha.
            string sResp = SendWebRequest(this.epHTTP, "/samantha/exclusiveUse.cgi?acquire=" + Password);

            // If we successfully acquired, then sResp is either 'none' or is the string
            // form of one of our IP addresses. REVIEW: 'none' rather indicates that we tried
            // to get it, but failed (perhaps due to a new password).
            if (IsLocalIPAddress(sResp))
                {
                this.fHaveExclusive = true;
                fResult = true;
                Util.TraceDebug("exclusive use of {0} acquired", this.epSamanthaTCP);
                }
            else
                {
                // Ask for more info as to what the problem is
                string sError = SendWebRequest(this.epHTTP, "/samantha/SAMANTHALastErrorByIP.htm");
                Util.ReportError("Unable to connect to Samantha module {0}: '{1}'", this.epSamanthaTCP.Address.ToString(), sError);
                }

            return fResult;
            }

        string SendWebRequest(IPEndPoint ep, string sRequest)
            {
            sRequest = "http://" + ep.Address.ToString() + sRequest;
            WebRequest  req  = WebRequest.Create(sRequest);
            WebResponse resp = req.GetResponse();
            System.IO.StreamReader reader = new System.IO.StreamReader(resp.GetResponseStream());
            string sResp = reader.ReadToEnd();
            return sResp;
            }

        bool IsLocalIPAddress(string host)
        // Is this host/ipAddress a name for the local host?
            {
            try
                { 
                IPAddress[] hostIPs  = Dns.GetHostAddresses(host);
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());

                foreach (IPAddress hostIP in hostIPs)
                    {
                    if (IPAddress.IsLoopback(hostIP)) 
                        return true;

                    foreach (IPAddress localIP in localIPs)
                        {
                        if (hostIP.Equals(localIP)) 
                            return true;
                        }
                    }
                }
            catch 
                { 
                }
            return false;
            }

        public override bool IsOpen { get 
            { 
            return this.socket != null;
            }}

        void SocketMonitorEnter()
            {
            Monitor.Enter(this);
            }
        void SocketMonitorExit()
            {
            Monitor.Exit(this);
            }
        void SocketMonitorLock(Action action)
            {
            try {
                this.SocketMonitorEnter();
                action.Invoke();
                }
            finally
                {
                this.SocketMonitorExit();
                }
            }

        public override bool Open(bool fTraceFailure=true)  
            { 
            try {
                if (this.AcquireExclusiveUse())
                    {
                    this.SocketMonitorLock(() =>
                        {
                        this.socket = new Socket(this.epSamanthaTCP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        this.socket.SendTimeout = 1000;
                        this.socket.Connect(this.epSamanthaTCP);
                        Util.TraceDebug("IP: opened: {0}", this.epSamanthaTCP);
                        });

                    this.readThread = new ThreadContext((ctx) => ReadThread((ThreadContext)ctx));
                    this.readThread.Start();
                    this.readThreadRunning.WaitOne();

                    // Program.TheForm.ConnectionWantsTelemetryPolling = true;
                    return base.Open(fTraceFailure);
                    }
                }
            catch (Exception)
                {
                }
            //
            this.Close();
            return false;
            }

        public override void Close()
        //  Close down resources we have acquired
            {
            base.Close();

            if (readThread != null)
                {
                readThread.Stop();
                readThreadRunning.Reset();
                }

            this.SocketMonitorLock(() => 
                {
                if (this.socket != null)
                    {
                    this.socket.Close();
                    this.socket = null;
                    Util.TraceDebug("socket {0} closed", this.epSamanthaTCP);
                    }
                });

            if (this.fHaveExclusive)
                {
                SendWebRequest(this.epHTTP, "/samantha/exclusiveUse.cgi?release");
                this.fHaveExclusive = false;
                Util.TraceDebug("exclusive use of {0} released", this.epSamanthaTCP);
                }
            }

        //--------------------------------------------------------------------------
        // Data transmission
        //--------------------------------------------------------------------------

        public override bool Send(NxtMessage msg)
            {
            bool fResult = false;
            this.SocketMonitorLock(() =>
                {
                if (this.IsOpen)
                    {
                    try
                        {
                        lock (this.msgReplyTargets)
                            {
                            this.msgReplyTargets.Add(msg);
                            }

                        byte[] rgbToSend = msg.DataForIPTransmission;

                        this.socket.Send(rgbToSend);

                        msg.NoteSent();
                        fResult = true;
                        }
                    catch (Exception e)
                        {
                        // Remove the msg if we have issues
                        //
                        Util.TraceDebug("IPConnection.Send({0}): exception thrown: {1}", this.epSamanthaTCP, e);
                        lock (this.msgReplyTargets)
                            {
                            for (int imsg = 0; imsg < this.msgReplyTargets.Count; imsg++)
                                {
                                if (this.msgReplyTargets[imsg] == msg)
                                    {
                                    this.msgReplyTargets.RemoveAt(imsg);
                                    break;
                                    }
                                }
                            }
                        }
                    }
                });
            return fResult;
            }

        //--------------------------------------------------------------------------
        // Data Reception
        //--------------------------------------------------------------------------

        class SignallingSocketEventArgs : SocketAsyncEventArgs
            {
            EventWaitHandle operationCompleteEvent;

            public SignallingSocketEventArgs(EventWaitHandle e)
                {
                this.operationCompleteEvent = e;
                }

            protected override void OnCompleted(SocketAsyncEventArgs ignored)
                {
                // Base impl does the following:
                //
                //    EventHandler<SocketAsyncEventArgs> completed = this.m_Completed;
                //    if (completed != null)
                //      {
                //      completed(e.m_CurrentSocket, e);
                //      }
                //
                this.operationCompleteEvent.Set();
                }
            }

        void ReadThread(ThreadContext ctx)
            {
            System.Threading.Thread.CurrentThread.Name = "IPConnection.ReadThread";

            EventWaitHandle      asyncReadCompleteEvent = new EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
            SocketAsyncEventArgs socketAsyncEventArgs   = new SignallingSocketEventArgs(asyncReadCompleteEvent);
            byte[]               rgbBuffer              = new byte[64];

            socketAsyncEventArgs.SetBuffer(rgbBuffer, 0, rgbBuffer.Length);

            try {
                WaitHandle[] waitHandles = new WaitHandle[2];
                waitHandles[0] = asyncReadCompleteEvent;
                waitHandles[1] = ctx.StopEvent;

                bool fStop = false;

                while (!fStop && !ctx.StopRequest)
                    {
                    // Issue an async read
                    //
                    asyncReadCompleteEvent.Reset();
                    bool fAsync = true;
                    this.SocketMonitorLock(() =>
                        {
                        fAsync = this.socket.ReceiveAsync(socketAsyncEventArgs);
                        });
                    if (!fAsync)
                        {
                        // IO operation completed synchronously
                        asyncReadCompleteEvent.Set();
                        }

                    this.readThreadRunning.Set();

                    // Wait until either the async read completes or we're asked to stop
                    int iWait = WaitHandle.WaitAny(waitHandles);
                
                    // Process according to which event fired
                    switch (iWait)
                        {
                    case 0: // Async read completed
                        {
                        if (socketAsyncEventArgs.BytesTransferred > 0)
                            {
                            if (socketAsyncEventArgs.SocketError == SocketError.Success)
                                {
                                // Util.Trace("IP Read: incoming packet");
                                ProcessIncomingPacket(rgbBuffer, socketAsyncEventArgs.BytesTransferred);
                                }
                            else
                                {
                                Util.TraceDebug("IP Read: unexpected async result: cb={0} err={1}", socketAsyncEventArgs.BytesTransferred, socketAsyncEventArgs.SocketError);
                                fStop = true;
                                }
                            }
                        else
                            {
                            // Util.Trace("IP Read: read completed with zero bytes");
                            }
                        }
                        break;
                    case 1: // StopEvent 
                        // Util.Trace("async read stop requested");
                        break;
                    // end switch
                        }
                    }
                }
            finally
                {
                }
            }

        }

    //------------------------------------------------------------------------------------------------
    // USBConnection
    //------------------------------------------------------------------------------------------------

    // http://msdn.microsoft.com/en-us/library/ff540046(v=VS.85).aspx#winusb

    class USBConnection : Connection
        {
        //--------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------

        string          sDevicePath;

        SafeFileHandle  hDevice;
        IntPtr          hWinUSB         = IntPtr.Zero;
        byte            bulkInPipe;
        byte            bulkOutPipe;
        byte            interruptInPipe;
        byte            interruptOutPipe;

        IntPtr          hDeviceNotify   = IntPtr.Zero;

        public static Guid guidNxtMindstormsDeviceInstance = new Guid("{761ED34A-CCFA-416b-94BB-33486DB1F5D5}");

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public USBConnection(string sDevicePath)
            {
            this.sDevicePath = sDevicePath;
            }

        protected override void Dispose(bool fDisposing)
            {
            if (!this.fDisposed)
                {
                this.Close();
                }
            base.Dispose(fDisposing);
            }

        public override string ToString()
            {
            return this.sDevicePath;
            }
        
        void OpenDeviceHandle(String devicePathName)
        // devicePathName is returned from SetupDiGetDeviceInterfaceDetail in an SP_DEVICE_INTERFACE_DETAIL_DATA structure
            {
            this.hDevice = SafeCreateFile
                (devicePathName,
                (GENERIC_WRITE | GENERIC_READ),
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);

            ThrowIfFail(!(this.hDevice.IsInvalid));
            }

        public void InitializeWinUSBDevice()
            {
            USB_INTERFACE_DESCRIPTOR ifaceDescriptor;
            ifaceDescriptor.bLength             = 0;
            ifaceDescriptor.bDescriptorType     = 0;
            ifaceDescriptor.bInterfaceNumber    = 0;
            ifaceDescriptor.bAlternateSetting   = 0;
            ifaceDescriptor.bNumEndpoints       = 0;
            ifaceDescriptor.bInterfaceClass     = 0;
            ifaceDescriptor.bInterfaceSubClass  = 0;
            ifaceDescriptor.bInterfaceProtocol  = 0;
            ifaceDescriptor.iInterface          = 0;

            WINUSB_PIPE_INFORMATION pipeInfo;
            pipeInfo.PipeType           = 0;
            pipeInfo.PipeId             = 0;
            pipeInfo.MaximumPacketSize  = 0;
            pipeInfo.Interval           = 0;

            ThrowIfFail(WinUsb_Initialize(this.hDevice, ref this.hWinUSB));
            ThrowIfFail(WinUsb_QueryInterfaceSettings(this.hWinUSB, 0, ref ifaceDescriptor));

            int msPipeTimeout = 0; // 0==infinite

            for (int i = 0; i < ifaceDescriptor.bNumEndpoints; i++)
                {
                ThrowIfFail(WinUsb_QueryPipe(this.hWinUSB, 0, System.Convert.ToByte(i), ref pipeInfo));
                    
                if (((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeBulk) && UsbEndpointDirectionIn(pipeInfo.PipeId)))
                    {
                    bulkInPipe = pipeInfo.PipeId;

                    SetPipePolicy(bulkInPipe, Convert.ToUInt32(POLICY_TYPE.IGNORE_SHORT_PACKETS),  Convert.ToByte(false));
                    SetPipePolicy(bulkInPipe, Convert.ToUInt32(POLICY_TYPE.PIPE_TRANSFER_TIMEOUT), msPipeTimeout);
                    }
                else if (((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeBulk) && UsbEndpointDirectionOut(pipeInfo.PipeId)))
                    {
                    bulkOutPipe = pipeInfo.PipeId;

                    SetPipePolicy(bulkOutPipe, Convert.ToUInt32(POLICY_TYPE.IGNORE_SHORT_PACKETS), Convert.ToByte(false));
                    SetPipePolicy(bulkOutPipe, Convert.ToUInt32(POLICY_TYPE.PIPE_TRANSFER_TIMEOUT), msPipeTimeout);
                    }
                else if ((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeInterrupt) && UsbEndpointDirectionIn(pipeInfo.PipeId))
                    {
                    interruptInPipe = pipeInfo.PipeId;

                    SetPipePolicy(interruptInPipe, Convert.ToUInt32(POLICY_TYPE.IGNORE_SHORT_PACKETS), Convert.ToByte(false));
                    SetPipePolicy(interruptInPipe, Convert.ToUInt32(POLICY_TYPE.PIPE_TRANSFER_TIMEOUT), msPipeTimeout);
                    }
                else if ((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeInterrupt) && UsbEndpointDirectionOut(pipeInfo.PipeId))
                    {
                    interruptOutPipe = pipeInfo.PipeId;

                    SetPipePolicy(interruptOutPipe, Convert.ToUInt32(POLICY_TYPE.IGNORE_SHORT_PACKETS),  Convert.ToByte(false));
                    SetPipePolicy(interruptOutPipe, Convert.ToUInt32(POLICY_TYPE.PIPE_TRANSFER_TIMEOUT), msPipeTimeout);
                    }
                }
            }

        private void SetPipePolicy(byte pipeId, uint policyType, byte value)
            {
            ThrowIfFail(WinUsb_SetPipePolicy(this.hWinUSB, pipeId, policyType, 1, ref value));
            }

        private void SetPipePolicy(byte pipeId, uint policyType, int value)
            {
            ThrowIfFail(WinUsb_SetPipePolicy1(this.hWinUSB, pipeId, policyType, 4, ref value));
            }

        private bool UsbEndpointDirectionIn(int addr)
            {
            return (addr & 0X80) == 0X80;
            }

        private bool UsbEndpointDirectionOut(int addr)
            {
            return !UsbEndpointDirectionIn(addr);
            }

        //--------------------------------------------------------------------------
        // Discovery
        //--------------------------------------------------------------------------

        public static List<KnownNXT> FindDeviceFromGuid(Guid guidDeviceInstance)
        // Find all connected instances of this kind of USB device
            {
            IntPtr hDeviceInfoSet = INVALID_HANDLE_VALUE;
            try
                {
                hDeviceInfoSet = SetupDiGetClassDevs(ref guidDeviceInstance, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
                if (INVALID_HANDLE_VALUE==hDeviceInfoSet)
                    ThrowWin32Error();

                SP_DEVICE_INTERFACE_DATA did = new SP_DEVICE_INTERFACE_DATA();
                did.Initialize();

                List<KnownNXT> result = new List<KnownNXT>();

                for (int iMember=0 ;; iMember++)
                    {
                    // Get did of the next interface
                    bool fSuccess = SetupDiEnumDeviceInterfaces
                        (hDeviceInfoSet,
                        IntPtr.Zero,
                        ref guidDeviceInstance,
                        iMember,
                        out did);

                    if (!fSuccess)
                        {
                        break;  // Done! no more 
                        }
                    else
                        {
                        // A device is present. Get details
                        SP_DEVICE_INTERFACE_DETAIL_DATA detail = new SP_DEVICE_INTERFACE_DETAIL_DATA();
                        detail.Initialize();

                        int cbRequired;
                        ThrowIfFail(SetupDiGetDeviceInterfaceDetail
                            (hDeviceInfoSet,
                            ref did,
                            ref detail,
                            Marshal.SizeOf(detail),
                            out cbRequired,
                            IntPtr.Zero));

                        result.Add(new KnownNXT(KnownNXT.CONNECTIONTYPE.USB, detail.DevicePath));
                        }
                    }

                return result;
                }
            finally
                {
                if (hDeviceInfoSet != IntPtr.Zero && hDeviceInfoSet != INVALID_HANDLE_VALUE)
                    {
                    SetupDiDestroyDeviceInfoList(hDeviceInfoSet);
                    }
                }
            }

        private void RegisterForDeviceNotifications()
            {
            DEV_BROADCAST_DEVICEINTERFACE_MANAGED devBDI = new DEV_BROADCAST_DEVICEINTERFACE_MANAGED();
            devBDI.dbcc_size        = Marshal.SizeOf(devBDI);
            devBDI.dbcc_devicetype  = DBT_DEVTYP_DEVICEINTERFACE;
            devBDI.dbcc_reserved    = 0;
            devBDI.dbcc_classguid   = guidNxtMindstormsDeviceInstance;
            devBDI.dbcc_name        = "";

            //IntPtr hwnd = Program.TheForm.Handle;

            //this.hDeviceNotify = RegisterDeviceNotification(hwnd, devBDI, DEVICE_NOTIFY_WINDOW_HANDLE);
            //if (IntPtr.Zero == this.hDeviceNotify)
            //    ThrowWin32Error();

            //Program.TheForm.DeviceRemoveComplete += OnDeviceRemoveComplete;
            }

        private void UnregisterDeviceNotifications()
            {
            //Program.TheForm.DeviceRemoveComplete -= OnDeviceRemoveComplete;

            //if (IntPtr.Zero != this.hDeviceNotify)
            //    {
            //    UnregisterDeviceNotification(this.hDeviceNotify);
            //    this.hDeviceNotify = IntPtr.Zero;
            //    }
            }

        void OnDeviceRemoveComplete(object sender, System.Windows.Forms.Message m)
            {
            bool fMe = DeviceNameMatch(m, this.sDevicePath);
            if (fMe)
                {
                // Program.TheForm.Disconnect();
                }
            }

        bool DeviceNameMatch(System.Windows.Forms.Message m, String mydevicePathName)
            {
            // The LParam parameter of Message is a pointer to a DEV_BROADCAST_HDR structure,
            // which is really a longer structure underneath
            //
            DEV_BROADCAST_HDR devBroadcastHeader = new DEV_BROADCAST_HDR();
            Marshal.PtrToStructure(m.LParam, devBroadcastHeader);
            //
            if ((devBroadcastHeader.dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE))
                {
                DEV_BROADCAST_DEVICEINTERFACE_MANAGED devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE_MANAGED();
                Marshal.PtrToStructure(m.LParam, devBroadcastDeviceInterface);

                return String.Compare(devBroadcastDeviceInterface.dbcc_name, mydevicePathName, true) == 0;
                }

            return false;
            }
        
        //--------------------------------------------------------------------------
        // Connection Management
        //--------------------------------------------------------------------------

        public override bool IsOpen  { get 
            { 
            return !this.hDevice.IsInvalid;
            }}

        public override bool Open(bool fTraceFailure=true)  
            { 
            try {
                this.OpenDeviceHandle(sDevicePath);
                this.InitializeWinUSBDevice();
                this.RegisterForDeviceNotifications();

                this.readThread = new ThreadContext((ctx) => ReadThread((ThreadContext)ctx));
                this.readThread.Start();
                this.readThreadRunning.WaitOne();

                // Program.TheForm.ConnectionWantsTelemetryPolling = true;

                return base.Open(fTraceFailure);
                }
            catch (Exception)
                {
                this.Close();
                return false;
                }
            }

        public override void Close()
        //  Closes the device handle obtained with CreateFile and frees resources.
            {
            base.Close();

            if (readThread != null)
                {
                readThread.Stop();
                readThreadRunning.Reset();
                }

            this.UnregisterDeviceNotifications();
            WinUsb_Free(this.hWinUSB);
            this.hWinUSB = IntPtr.Zero;          // avoid possible double free

            if (this.hDevice != null)
                {
                if (!(this.hDevice.IsInvalid))
                    {
                    this.hDevice.Close();
                    }
                }
            }


        //--------------------------------------------------------------------------
        // Data transmission
        //--------------------------------------------------------------------------

        public override bool Send(NxtMessage msg)
            {
            lock (this.msgReplyTargets)
                {
                this.msgReplyTargets.Add(msg);
                }

            byte[] rgbToSend = msg.DataForUSBTransmission;

            int cbWritten = 0;
            bool fSuccess = WinUsb_WritePipe(
                hWinUSB,
                bulkOutPipe,
                rgbToSend,
                rgbToSend.Length,
                ref cbWritten,
                IntPtr.Zero);

            if (fSuccess)
                {
                msg.NoteSent();
                }
            else
                {
                this.Close();
                }

            return fSuccess;
            }        

        //--------------------------------------------------------------------------
        // Data Reception
        //--------------------------------------------------------------------------

        // http://www.beefycode.com/post/Using-Overlapped-IO-from-Managed-Code.aspx

        #pragma warning disable 0618 // warning CS0618: 'System.Threading.WaitHandle.Handle' is obsolete: 'Use the SafeWaitHandle property instead.'

        unsafe void ReadThread(ThreadContext ctx)
            {
            System.Threading.Thread.CurrentThread.Name = "USBConnection.ReadThread";

            EventWaitHandle     asyncReadCompleteEvent = new EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
            NativeOverlapped*   pNativeOverlapped      = null;
            byte[]              rgbBuffer              = new byte[64];

            try {
                WaitHandle[] waitHandles = new WaitHandle[2];
                waitHandles[0] = asyncReadCompleteEvent;
                waitHandles[1] = ctx.StopEvent;
                //
                // If we get unexpected errors, we stop reading; likely these are caused by a device
                // in the process of disconnecting.
                //
                bool fStop = false;
                //
                while (!fStop && !ctx.StopRequest)
                    {
                    // Issue an async read
                    // 
                    asyncReadCompleteEvent.Reset();
                    Overlapped overlapped = new Overlapped(0, 0, asyncReadCompleteEvent.Handle, null);
                    pNativeOverlapped = overlapped.Pack(null, rgbBuffer);
                    int cbRead = 0;

                    // Util.Trace("issuing async read: 0x{0:08X} 0x{1:08X}", new IntPtr(pNativeOverlappedWrite), this.hWinUSB);
                    bool fSuccess = WinUsb_ReadPipe(
                        this.hWinUSB,
                        bulkInPipe,
                        rgbBuffer,
                        rgbBuffer.Length,
                        out cbRead,
                        new IntPtr(pNativeOverlapped));

                    this.readThreadRunning.Set();

                    if (!fSuccess)
                        {
                        int err = Marshal.GetLastWin32Error();
                        if (err != ERROR_IO_PENDING)
                            {
                            Util.TraceDebug("USB Read: WinUsb_ReadPipe=={0}", err);
                            fStop = true;
                            continue;
                            }
                        }

                    // Wait until either the async read completes or we're asked to stop
                    int iWait = WaitHandle.WaitAny(waitHandles);
                
                    // Process according to which event fired
                    switch (iWait)
                        {
                    case 0: // Async read completed
                        {
                        // Util.Trace("async read complete: 0x{0:08X} 0x{1:08X}", new IntPtr(pNativeOverlappedWrite), this.hWinUSB);
                        if (WinUsb_GetOverlappedResult(this.hWinUSB, new IntPtr(pNativeOverlapped), ref cbRead, System.Convert.ToByte(true)))
                            {
                            ProcessIncomingPacket(rgbBuffer, cbRead);
                            }
                        else
                            {
                            int err = Marshal.GetLastWin32Error();
                            Util.TraceDebug("USB Read: WinUsb_GetOverlappedResult=={0}", err);
                            fStop = true;
                            }
                        //
                        System.Threading.Overlapped.Free(pNativeOverlapped);
                        pNativeOverlapped = null;
                        }
                        break;
                    case 1: // StopEvent 
                        // Util.Trace("async read stop requested");
                        break;
                    // end switch
                        }
                    }
                }
            finally 
                {
                // Util.Trace("async cleanup: 0x{0:08X} 0x{1:08X}", new IntPtr(pNativeOverlappedWrite), this.hWinUSB);
                WinUsb_AbortPipe(this.hWinUSB, bulkInPipe);
                asyncReadCompleteEvent.Close();

                if (pNativeOverlapped != null)
                    {
                    System.Threading.Overlapped.Free(pNativeOverlapped);
                    pNativeOverlapped = null;
                    }
                }
            }

        #pragma warning restore 0618

        public int SynchronousReadBulk(byte[] rgb, int ib, int cbToRead)
            {
            byte[]  rgbBuffer = new byte[cbToRead];
            int     cbRead    = 0;

            try
                {
                ThrowIfFail(WinUsb_ReadPipe(
                    hWinUSB, 
                    System.Convert.ToByte(bulkInPipe), 
                    rgbBuffer, 
                    cbToRead, 
                    out cbRead, 
                    IntPtr.Zero));
                }
            catch (Exception)
                {
                Close();
                throw;
                }

            Array.Copy(rgbBuffer, 0, rgb, ib, cbRead);
            return cbRead;
            }

        }

    //------------------------------------------------------------------------------------------------
    // BluetoothConnection
    //
    // http://msdn.microsoft.com/en-us/library/aa363196(v=VS.85).aspx
    //------------------------------------------------------------------------------------------------

    class BluetoothConnection : Connection
        {
        //--------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------

        // 0 is used for joystick telemetryMessages
        // 1 is used by Samantha module
        public static int _iTelemetryMailbox = 2; // NXT BT protocol mailbox used to receive telemetry telemetryMessages (zero based)

        public static int iTelemetryMailbox { 
            get { return _iTelemetryMailbox; }
            set { if (value != _iTelemetryMailbox)
                    {
                    //if (Program.TheForm.SelectedNXT != null && Program.TheForm.SelectedNXT.ConnectionType == KnownNXT.CONNECTIONTYPE.BLUETOOTH)
                    //    {
                    //    Program.TheForm.Disconnect();
                    //    }
                    _iTelemetryMailbox = value;
                    }
                }}

        string  portName    = "COM1";
        IntPtr  hSerialPort = IntPtr.Zero;

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public BluetoothConnection(string portName)
            {
            this.portName = portName;
            }

        public override string ToString()
            {
            return this.portName;
            }

        //--------------------------------------------------------------------------
        // Connection Management
        //--------------------------------------------------------------------------

        public static IEnumerable<KnownNXT> GetNXTBluetoothSerialPortNames()
        // We figure that any attached Bluetooth device which is classified as a Major=Toy Minor=Robot is 
        // probably a NXT, and worthy of inclusion on our list.
            {
            List<KnownNXT> result = new List<KnownNXT>();

            WIN32.BLUETOOTH_DEVICE_SEARCH_PARAMS search = new WIN32.BLUETOOTH_DEVICE_SEARCH_PARAMS();
            WIN32.BLUETOOTH_DEVICE_INFO          info   = new WIN32.BLUETOOTH_DEVICE_INFO();
            search.Initialize();
            info.Initialize();

            search.fReturnConnected  = Convert.ToInt32(true);
            search.fReturnRemembered = Convert.ToInt32(true);

            IntPtr hFind = WIN32.BluetoothFindFirstDevice(ref search, ref info);
            try 
                {
                if (IntPtr.Zero != hFind)
                    {
                    bool fContinue = true;
                    do  {
                        if (info.ClassOfDeviceMajor == 0x08 /* toy */ && info.ClassOfDeviceMinor == 0x01 /* robot */)
                            {
                            string sComPort = COMPortNameFromBluetoothAddress(info.btAddress);
                            if (null != sComPort)
                                {
                                result.Add(new KnownNXT(KnownNXT.CONNECTIONTYPE.BLUETOOTH, sComPort, info.szName));
                                }
                            }
                        fContinue = WIN32.BluetoothFindNextDevice(hFind, ref info);
                        }
                    while (fContinue);
                    }
                }
            finally
                {
                WIN32.BluetoothFindDeviceClose(hFind);
                }

            result.Sort(KnownNXT.Compare);
            return result;
            }

        public override bool IsOpen { get 
            { 
            return this.SerialPortHandleIsValid; 
            }}

        bool SerialPortHandleIsValid { get 
            { 
            return this.hSerialPort != IntPtr.Zero && this.hSerialPort != new IntPtr(-1); 
            }}

        public override bool Open(bool fTraceFailure=true)  
            {
            try 
                {
                // Get a file handle to the serial port.
                // REVIEW: How can we control the timeout used in this operation?
                //
                this.hSerialPort = WIN32.CreateFile(@"\\.\" + this.portName, 
                    WIN32.GENERIC_READ|WIN32.GENERIC_WRITE, 
                    0,                                          // share mode
                    IntPtr.Zero,                                // security attributes
                    OPEN_EXISTING, 
                    FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, 
                    IntPtr.Zero
                    );
                if (this.SerialPortHandleIsValid)
                    {
                    // TO DO: call SetupComm 
                    // TO DO: consider COMMCONFIG, COMMPROP 

                    // Set the stuff we set using the DCB. Do a 'get' first
                    // so that we only change the stuff we're looking to actually change
                    //
                    WIN32.DCB dcb = new DCB(); dcb.Initialize();
                    ThrowIfFail(WIN32.GetCommState(this.hSerialPort, ref dcb));
                    dcb.BaudRate = 115200;                  // arbitrary/historical, but seems to work
                    dcb.SetFlag(WIN32.DCBFLAG.DISCARDNULL, 0);
                    ThrowIfFail(WIN32.SetCommState(this.hSerialPort, ref dcb));

                    // Set the timeouts. We specify all the fields do
                    // don't need to do a 'get' first.
                    WIN32.COMMTIMEOUTS timeouts = new COMMTIMEOUTS();
                    timeouts.ReadTotalTimeoutConstant    = 500;
                    timeouts.ReadTotalTimeoutMultiplier  = -1;
                    timeouts.ReadIntervalTimeout         = -1;
                    timeouts.WriteTotalTimeoutMultiplier = 0;
                    timeouts.WriteTotalTimeoutConstant   = 500;
                    ThrowIfFail(WIN32.SetCommTimeouts(this.hSerialPort, ref timeouts));

                    this.readThread = new ThreadContext((ctx) => ReadThread((ThreadContext)ctx));
                    this.readThread.Start();
                    this.readThreadRunning.WaitOne();
                    }
                else
                    WIN32.ThrowWin32Error();

                // Program.TheForm.ConnectionWantsTelemetryPolling = false;

                return base.Open(fTraceFailure);
                }
            catch (Exception)
                {
                if (fTraceFailure)
                    Util.ReportError("warning: can't open serial port {0} (is the NXT connected?)", this.portName);
                this.Close();
                return false;
                }
            }

        public override void Close()
            {
            base.Close();

            if (readThread != null)
                {
                readThread.Stop();
                readThreadRunning.Reset();
                }
            if (this.SerialPortHandleIsValid)
                {
                WIN32.CloseHandle(this.hSerialPort);
                this.hSerialPort = IntPtr.Zero;
                }
            }

        protected override void Dispose(bool fDisposing)
            {
            if (!this.fDisposed)
                {
                if (fDisposing)
                    {
                    }
                this.Close();
                }
            base.Dispose(fDisposing);
            }

        //--------------------------------------------------------------------------
        // Data transmission
        //--------------------------------------------------------------------------

        public override bool Send(NxtMessage msg)
            {
            bool fResult = false;
            if (this.IsOpen)
                {
                try {
                    lock (this.msgReplyTargets)
                        {
                        this.msgReplyTargets.Add(msg);
                        }

                    byte[] rgbToSend = msg.DataForBluetoothTransmission;
                    int cbSent = 0;

                    unsafe
                        {
                        EventWaitHandle     asyncWriteCompleteEvent = new EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
                        Overlapped          overlappedWrite         = new Overlapped(-1, -1, asyncWriteCompleteEvent.SafeWaitHandle.DangerousGetHandle(), null);
                        NativeOverlapped*   pNativeOverlappedWrite  = overlappedWrite.Pack(null, rgbToSend);
                        try 
                            {
                            bool fSuccess = WriteFile(this.hSerialPort, rgbToSend, rgbToSend.Length, out cbSent, new IntPtr(pNativeOverlappedWrite));
                            if (!fSuccess)
                                {
                                int err = Marshal.GetLastWin32Error();
                                if (ERROR_IO_PENDING == err)
                                    {
                                    asyncWriteCompleteEvent.WaitOne();
                                    }
                                else
                                    ThrowWin32Error(err);
                                }
                            }
                        finally
                            {
                            System.Threading.Overlapped.Free(pNativeOverlappedWrite);
                            }
                        }

                    msg.NoteSent();
                    fResult = true;
                    }
                catch (Exception)
                    {
                    }
                }
            return fResult;
            }

        //--------------------------------------------------------------------------
        // Data reception
        //--------------------------------------------------------------------------

        unsafe void ReadThread(ThreadContext ctx)
            {
            System.Threading.Thread.CurrentThread.Name = "BluetoothConnection.ReadThread";

            EventWaitHandle     asyncReadCompleteEvent = new EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
            NativeOverlapped*   pNativeOverlapped      = null;
            byte[]              rgbBuffer              = new byte[64];

            try {
                WaitHandle[] waitHandles = new WaitHandle[2];
                waitHandles[0] = asyncReadCompleteEvent;
                waitHandles[1] = ctx.StopEvent;
                //
                while (!ctx.StopRequest)
                    {
                    // Issue an async read
                    // 
                    asyncReadCompleteEvent.Reset();
                    Overlapped overlapped = new Overlapped(0, 0, asyncReadCompleteEvent.SafeWaitHandle.DangerousGetHandle(), null);
                    pNativeOverlapped = overlapped.Pack(null, rgbBuffer);
                    int cbRead = 0;

                    bool fSuccess = ReadFile(this.hSerialPort, rgbBuffer, rgbBuffer.Length, out cbRead, new IntPtr(pNativeOverlapped));
                    readThreadRunning.Set();
                    if (!fSuccess)
                        {
                        int err = Marshal.GetLastWin32Error();
                        if (err != ERROR_IO_PENDING)
                            ThrowWin32Error(err);
                        }

                    // Wait until either the async read completes or we're asked to stop
                    int iWait = WaitHandle.WaitAny(waitHandles);
                
                    // Process according to which event fired
                    switch (iWait)
                        {
                    case 0: // Async read completed
                        {
                        ThrowIfFail(GetOverlappedResult(this.hSerialPort, new IntPtr(pNativeOverlapped), ref cbRead, System.Convert.ToByte(true)));
                        // Util.Trace("async read complete: 0x{0:08X} 0x{1:08X} cb={2}", new IntPtr(pNativeOverlapped), this.hSerialPort, cbRead);

                        // Record the new data and process any packets that are now complete
                        this.RecordIncomingData(rgbBuffer, cbRead);
                        ProcessPacketIfPossible();

                        System.Threading.Overlapped.Free(pNativeOverlapped);
                        pNativeOverlapped = null;
                        }
                        break;
                    case 1: // StopEvent 
                        break;
                    // end switch
                        }

                    }
                }
            finally 
                {
                CancelIo(this.hSerialPort);
                asyncReadCompleteEvent.Close();

                if (pNativeOverlapped != null)
                    {
                    System.Threading.Overlapped.Free(pNativeOverlapped);
                    pNativeOverlapped = null;
                    }
                }
            }

        void ProcessPacketIfPossible()
        // If we've accumulated a full packet of serial data, then process it
            {
            // Note: the packet format here is described in the "Lego Mindstorms NXT Bluetooth Development Kit"
            // http://mindstorms.lego.com/en-us/support/files/default.aspx
            //
            lock (rgbBytesAvailable)
                {
                while (this.rgbBytesAvailable.Count >= 2)
                    {
                    int lsb = this.rgbBytesAvailable[0];
                    int msb = this.rgbBytesAvailable[1];
                    int cbMessage = (msb<<8) + lsb;     // #bytes following lsb & msb
                    if (this.rgbBytesAvailable.Count >= 2 + cbMessage)
                        {
                        this.SkipBytes(2);
                        byte[] rgb = ReadBytes(cbMessage);
                        ProcessIncomingPacket(rgb, cbMessage);
                        }
                    else
                        break;
                    }
                }
            }

        //-------------------------------------------------------------------------------------------------------
        // Registry navigation
        //-------------------------------------------------------------------------------------------------------

        public static string COMPortNameFromBluetoothAddress(WIN32.BLUETOOTH_ADDRESS btAddr)
            {
            string sBtAddr = btAddr.ToString();
            return COMPortNameFromBluetoothAddress(sBtAddr);
            }

        public static string COMPortNameFromBluetoothAddress(string sBtAddr)
            {
            // Our first task is to locate our address in the values in
            //      HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\BTHMODEM\Enum
            // where we might, find, e.g., the value named '2' with value
            //      BTHENUM\{00001101-0000-1000-8000-00805f9b34fb}_LOCALMFG&000a\7&8083581&0&001653005C38_C00000009
            //
            // Doing this will ensure we only use *current* uses of this Bluetooth address, not stale registry stuff.
            //
            string sDeviceInstance = null;
            string sPortName       = null;

            using (RegistryKey hkeyEnum = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\BTHMODEM\Enum"))
                {
                if (hkeyEnum != null)
                    {
                    string sTarget = "&" + sBtAddr + "_";
                    foreach (string sEnumValueName in hkeyEnum.GetValueNames())
                        {
                        string sEnumValueValue = hkeyEnum.GetValue(sEnumValueName) as string;
                        if (sEnumValueValue != null)
                            {
                            if (sEnumValueValue.Contains(sTarget))
                                {
                                sDeviceInstance = sEnumValueValue;
                                break;
                                }
                            }
                        }
                    }
                }
            //
            if (sDeviceInstance != null)
                {
                // Our next task is to look at the device instance as a subkey of
                //      HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum
                // wherefrom we then look at 'Device Parameters' and extract the 'PortName' value.
                // 
                using (RegistryKey hkeyDeviceParams = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + sDeviceInstance + @"\Device Parameters"))
                    {
                    if (hkeyDeviceParams != null)
                        {
                        sPortName = hkeyDeviceParams.GetValue("PortName") as string;
                        }
                    }
                }
            //
            return sPortName;
            }
        }
    }
