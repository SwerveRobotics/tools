//
// JoystickMessage.cs
// 
// Construction of the message sent to a NXT that carries the state of the joystick controllers.
//
namespace Org.SwerveRobotics.Tools.Library
    {
    //------------------------------------------------------------------------------------------------
    // Joystick related messages
    //------------------------------------------------------------------------------------------------

    // Corresponding bit in 'buttons' field is (1<<(joybtn-1)
    enum JOYBTN
        {
        JOYBTN_1                    =1,
        JOYBTN_2                    =2,
        JOYBTN_3                    =3,
        JOYBTN_4                    =4,
        JOYBTN_LEFTTRIGGER_UPPER    =5,
        JOYBTN_RIGHTTRIGGER_UPPER   =6,
        JOYBTN_LEFTTRIGGER_LOWER    =7,
        JOYBTN_RIGHTTRIGGER_LOWER   =8,
        JOYBTN_TOP_LEFT             =9,
        JOYBTN_TOP_RIGHT            =10,
        JOYBTN_JOYSTICK_LEFT        =11,
        JOYBTN_JOYSTICK_RIGHT       =12,
        };

    enum JOYHAT
        {
        JOYHAT_NONE      = -1,
        JOYHAT_UP        = 0,
        JOYHAT_UPRIGHT   = 1,
        JOYHAT_RIGHT     = 2,
        JOYHAT_DOWNRIGHT = 3,
        JOYHAT_DOWN      = 4,
        JOYHAT_DOWNLEFT  = 5,
        JOYHAT_LEFT      = 6,
        JOYHAT_UPLEFT    = 7,
        };

    enum IJOY
        {
        JOY_LEFT=0,
        JOY_RIGHT=1,
        };

    public class JoystickControllerMessage
    // The part of a JoystickNxtMessage which pertains to just one joystick controller
        {
        //--------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------

        public JoystickControllersAndGameControlNxtMessage msg;
        public int                ijyc;
        public const int          cb = 7;           // we use 7 bytes of msg

        public byte this[int ib]
            {
            get { return this.msg[JoystickControllersAndGameControlNxtMessage.dibJycFirst + this.ijyc * cb + ib]; }
            set {        this.msg[JoystickControllersAndGameControlNxtMessage.dibJycFirst + this.ijyc * cb + ib] = value ; }
            }

        //--------------------------------------------------------------------------
        // Access
        //--------------------------------------------------------------------------

        public int  xLeft          { get { return this[0];      } set { this[0] = (byte)(value);         }}
        public int  yLeft          { get { return this[1];      } set { this[1] = (byte)(value);         }}
        public int  xRight         { get { return this[2];      } set { this[2] = (byte)(value);         }}
        public int  yRight         { get { return this[3];      } set { this[3] = (byte)(value);         }}
        public int  buttons        { get 
                                         { 
                                         return (int)this[4] + (((int)this[5]) << 8);
                                         }
                                     set
                                         { 
                                         this[4] = (byte)(value & 0xFF);
                                         this[5] = (byte)((value >> 8) & 0xFF);
                                         }
                                   }
        public int  hat            { get { return this[6];      } set { this[6] = (byte)(value);         }}

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public JoystickControllerMessage(JoystickControllersAndGameControlNxtMessage msg, int ijyc)
            {
            this.msg  = msg;
            this.ijyc = ijyc;
            this.hat  = (int)JOYHAT.JOYHAT_NONE;
            }
        }

    public class JoystickControllersAndGameControlNxtMessage : MessageWriteNxtMessage
    // Class that encapsulates the data we send to the NXT for joystick control
        {
        //--------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------

        public const int dibJycFirst = 3;                   // offset from StartFunction of payload to the first byte of jyc[0]
        public const int cbPayload   = dibJycFirst + 2*JoystickControllerMessage.cb;
        public const int cbExtra     = 1;                   // we have to send a terminating NULL

        //--------------------------------------------------------------------------
        // Accessing
        //--------------------------------------------------------------------------

        public bool fTeleOp        { get { return this[1] != 0; } set { this[1] = (byte)(value ? 1 : 0); }}
        public bool fWaitForStart  { get { return this[2] != 0; } set { this[2] = (byte)(value ? 1 : 0); }}

        public const int cjyc = 2;
        public JoystickControllerMessage[] rgjyc = new JoystickControllerMessage[cjyc];

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public JoystickControllersAndGameControlNxtMessage() : base(0 /*mailbox*/, cbPayload + cbExtra )
        // Initialize the message and read the current state of the joystick controllers
        // NOTE: this MUST be called in the main STA thread due to COM apartment considerations.
            {
            // Initialize the message state
            //
            for (int ijyc = 0; ijyc < cjyc; ijyc++)
                {
                this.rgjyc[ijyc] = new JoystickControllerMessage(this, ijyc);
                }
            //
            this.fTeleOp       = true;
            this.fWaitForStart = false;
            //
            // Read the current values of the joysticks
            //
            for (int ijyc = 0; ijyc < System.Math.Min(JoystickController.Controllers.Count, cjyc); ijyc++)
                {
                JoystickController jyc = JoystickController.Controllers[ijyc];
                jyc.ReadCurrentState();
                Read(jyc, ijyc);
                }
            }

        public void Read(JoystickController controller, int ijyc)
            {
            this.rgjyc[ijyc].xLeft   = controller.xLeft;
            this.rgjyc[ijyc].yLeft   = controller.yLeft;
            this.rgjyc[ijyc].xRight  = controller.xRight;
            this.rgjyc[ijyc].yRight  = controller.yRight;
            this.rgjyc[ijyc].buttons = controller.buttons;
            this.rgjyc[ijyc].hat     = controller.hat;
            }
        }
    }
