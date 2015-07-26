//
// JoystickController.cs
//
// Functionality for reading state from the joystick controllers / game pads / whatever you wish to call them.
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
// using Excel = Microsoft.Office.Interop.Excel;

namespace Org.SwerveRobotics.Tools.Library
    {
    //------------------------------------------------------------------------------------------------
    // JoystickController
    //------------------------------------------------------------------------------------------------

    public class JoystickController
        {
        //--------------------------------------------------------------------------
        // Public State
        //--------------------------------------------------------------------------

        // The list of discovered joystick controllers attached to this computer
        public static List<JoystickController> Controllers = new List<JoystickController>();
        public static bool                     HasControllers() { return Controllers.Count != 0; }

        public int xLeft    { get { return this.state.x; }}
        public int yLeft    { get { return this.state.y; }}
        public int xRight   { get { return this.state.z; }}
        public int yRight   { get { return this.state.rotationZ; }}
        public int buttons  { get 
                                  {
                                  int result = 0; 
                                  result = NoteButton(result, 0, state.button0);
                                  result = NoteButton(result, 1, state.button1);
                                  result = NoteButton(result, 2, state.button2);
                                  result = NoteButton(result, 3, state.button3);
                                  result = NoteButton(result, 4, state.button4);
                                  result = NoteButton(result, 5, state.button5);
                                  result = NoteButton(result, 6, state.button6);
                                  result = NoteButton(result, 7, state.button7);
                                  result = NoteButton(result, 8, state.button8);
                                  result = NoteButton(result, 9, state.button9);
                                  result = NoteButton(result,10, state.button10);
                                  result = NoteButton(result,11, state.button11);
                                  return result;
                                  }
                            }
        private int NoteButton(int result, int iButton, byte buttonValue)
            {
            if (buttonValue != 0)
                {
                result |= (1<<iButton);
                }
            return result;
            }

        public int hat      { get
                                  {
                                  /* "The position is indicated in hundredths of a degree clockwise from 
                                   * north (away from the user). The center position is normally reported 
                                   * as - 1; but see Remarks. For indicators that have only five positions, 
                                   * the value for a controller is -1, 0, 9,000, 18,000, or 27,000." 
                                   * 
                                   * Remarks: Some drivers report the centered position of the POV indicator 
                                   * as 65,535. Determine whether the indicator is centered as follows: 
                                   * 
                                   *    BOOL POVCentered = (LOWORD(dwPOV) == 0xFFFF);
                                   * 
                                   * http://msdn.microsoft.com/en-us/library/microsoft.directx_sdk.reference.dijoystate2(VS.85).aspx
                                   */
                                  int value = state.pov;
                                  if ((value & 0xFFFF) == 0xFFFF)
                                     return -1;
                                  else
                                     return ((value + 2250)) / 4500;
                                  }
                            }

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public JoystickController(DirectInput dinput, Guid guid)
        // Initialize this Joystick controller
            {
            this.directInputDevice = dinput.CreateDevice(guid);

            foreach (DIDEVICEOBJECTINSTANCE ddoi in this.directInputDevice.EnumObjects(DIDFT.AXIS))
                {
                if ((ddoi.dwType & (int)DIDFT.AXIS) != 0)   // can omit?
                    {
                    this.directInputDevice.SetRange(ddoi, -128, 127);
                    }
                }

            // We stick formatDesc in a member variable because we're not sure whether
            // we're allowed to free it right after the SetDataFormat call or whether
            // we're responsible for keeping it alive so long as we use the IDirectInputDevice8
            // instance. 
            this.formatDesc = State.GetDataRetrievalDescriptor();
            this.directInputDevice.SetDataFormat(ref this.formatDesc);
                }

        ~JoystickController()
            {
            this.formatDesc.Free();
            }

        public static void FindJoystickControllers()
        // Populate the Controllers variable with all the attached controllers
            {
            DirectInput dinput = new DirectInput();
            foreach (DIDEVICEINSTANCEW device in dinput.EnumDevices(DI8DEVTYPE.CLASS_GAMECTRL, DIEDFL.ATTACHEDONLY))
                {
                JoystickController jyc = new JoystickController(dinput, device.guidInstance);
                JoystickController.Controllers.Add(jyc);
                }
            }

        //--------------------------------------------------------------------------
        // Reading
        //--------------------------------------------------------------------------

        public void ReadCurrentState()
        // Read the current state of the controller into our member variables
            {
            this.directInputDevice.Acquire();
            this.directInputDevice.Poll();
            unsafe
                {
                fixed (State* pState = &this.state)
                    {
                    this.directInputDevice.GetDeviceState(Marshal.SizeOf(state), pState);
                    }
                }
            }

        //--------------------------------------------------------------------------
        // Internal State
        //--------------------------------------------------------------------------

        State             state;
        DirectInputDevice directInputDevice;
        DIDATAFORMAT      formatDesc;

        // Structure into which our raw joystick state is read by DirectInput
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public struct State // must be multiple of 4 bytes in size
            {
            public int  x;   
            public int  y;   
            public int  z;   
            public int  rotationZ;  
            public int  pov;
            public byte button0;
            public byte button1;
            public byte button2;
            public byte button3;
            public byte button4;
            public byte button5;
            public byte button6;
            public byte button7;
            public byte button8;
            public byte button9;
            public byte button10;
            public byte button11;

            public static unsafe DIDATAFORMAT GetDataRetrievalDescriptor()
                {
                DIDATAFORMAT format = new DIDATAFORMAT();
                int cFields = typeof(State).GetFields().Length;
                format.Init(cFields);
                format.dwFlags    = (int)DIDF.ABSAXIS;
                format.dwDataSize = Marshal.SizeOf(typeof(State));
                //
                format.rgodf[0].Set(ref Guids.XAxis,  Marshal.OffsetOf(typeof(State), "x"),         DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.AXIS,    DIDOI.ASPECTPOSITION);
                format.rgodf[1].Set(ref Guids.YAxis,  Marshal.OffsetOf(typeof(State), "y"),         DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.AXIS,    DIDOI.ASPECTPOSITION);
                format.rgodf[2].Set(ref Guids.ZAxis,  Marshal.OffsetOf(typeof(State), "z"),         DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.AXIS,    DIDOI.ASPECTPOSITION);
                format.rgodf[3].Set(ref Guids.RZAxis, Marshal.OffsetOf(typeof(State), "rotationZ"), DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.AXIS,    DIDOI.ASPECTPOSITION);
                format.rgodf[4].Set(ref Guids.POV,    Marshal.OffsetOf(typeof(State), "pov"),       DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.POV,     DIDOI.NONE);
                format.rgodf[5].Set(null,             Marshal.OffsetOf(typeof(State), "button0"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[6].Set(null,             Marshal.OffsetOf(typeof(State), "button1"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[7].Set(null,             Marshal.OffsetOf(typeof(State), "button2"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[8].Set(null,             Marshal.OffsetOf(typeof(State), "button3"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[9].Set(null,             Marshal.OffsetOf(typeof(State), "button4"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[10].Set(null,            Marshal.OffsetOf(typeof(State), "button5"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[11].Set(null,            Marshal.OffsetOf(typeof(State), "button6"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[12].Set(null,            Marshal.OffsetOf(typeof(State), "button7"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[13].Set(null,            Marshal.OffsetOf(typeof(State), "button8"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[14].Set(null,            Marshal.OffsetOf(typeof(State), "button9"),   DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[15].Set(null,            Marshal.OffsetOf(typeof(State), "button10"),  DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                format.rgodf[16].Set(null,            Marshal.OffsetOf(typeof(State), "button11"),  DIDFT.MYSTERY|DIDFT.ANYINSTANCE|DIDFT.BUTTON,  DIDOI.NONE);
                //
                return format;
                }
            }
        }
    }
