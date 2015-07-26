//
// TelemetryRecord.cs
//
using System;
using System.Collections.Generic;
using System.Text;
// using Excel = Microsoft.Office.Interop.Excel;

namespace Org.SwerveRobotics.Tools.Library
    {
    // Values used to tag telemetry data coming from NXT
    public enum  DATUM_TYPE : byte
        {
        NONE = 0,
        ENDOFRECORDSET,
        INT8,
        INT16,
        INT32,
        UINT8,
        UINT16,
        UINT32,
        FLOAT,
        STRING,
        BOOL,
        CHAR,
        };

    // A telemetry record as received from the NXT
    public class TelemetryRecord
        {
        //--------------------------------------------------------------------------
        // State
        //--------------------------------------------------------------------------

        public const int    cbMessageMax = 59;                      // REVIEW: is this *exactly* right?
        public byte[]       rgbBuffer    = new byte[cbMessageMax];
        public List<object> data         = new List<object>();
        public bool         fEndOfRecordSet = false;

        //--------------------------------------------------------------------------
        // Construction
        //--------------------------------------------------------------------------

        public TelemetryRecord(byte[] rgbBuffer)
            {
            this.rgbBuffer = rgbBuffer;
            }

        //--------------------------------------------------------------------------
        // Reception
        //--------------------------------------------------------------------------

        delegate T PFNPARSE<T>(byte[] value);

        // Parse these bytes into the data that they represent
        void Parse<T>(ref int ib, int cb, PFNPARSE<T> converter) where T : struct
            {
            // Read the data
            byte[] rgb = new byte[cb];
            Buffer.BlockCopy(this.rgbBuffer, ib, rgb, 0, cb);

            // Convert and remember it
            T value = converter(rgb);
            this.data.Add(value);

            // Skip the data
            ib += cb;
            }

        // Parse the message contents. The message is a sequence of tagged data, where the tags are
        // drawn from DATUM_TYPE. Each type of data has its own data format, which is pretty straightfoward.
        // Note that only the lower four bits of the tags is significant. The upper four bits of the first
        // tag indicates the (zero-based) Sheet number which is to be logged to; the upper four bits of
        // remaining tags are currently unused.
        public void Parse()
            {
            this.data = new List<object>();

            bool fDone = false;

            // Parse the data in the message
            for (int ib = 0; !fDone && ib < this.rgbBuffer.Length; )
                {
                byte bTag = this.rgbBuffer[ib++];
                switch (bTag & 0x0F)
                    {
                case (byte)DATUM_TYPE.INT8:
                    Parse(ref ib, sizeof(sbyte), rgb => (sbyte)(rgb[0]));
                    break;
                case (byte)DATUM_TYPE.INT16:
                    Parse(ref ib, sizeof(short), rgb => BitConverter.ToInt16(rgb, 0));
                    break;
                case (byte)DATUM_TYPE.INT32:
                    Parse(ref ib, sizeof(int), rgb => BitConverter.ToInt32(rgb, 0));
                    break;

                case (byte)DATUM_TYPE.UINT8:
                    Parse(ref ib, sizeof(byte), rgb => (byte)(rgb[0]));
                    break;
                case (byte)DATUM_TYPE.UINT16:
                    Parse(ref ib, sizeof(ushort), rgb => (ushort)BitConverter.ToInt16(rgb, 0));
                    break;
                case (byte)DATUM_TYPE.UINT32:
                    Parse(ref ib, sizeof(uint), rgb => (uint)BitConverter.ToInt32(rgb, 0));
                    break;

                case (byte)DATUM_TYPE.FLOAT:
                    Parse(ref ib, sizeof(float), rgb => BitConverter.ToSingle(rgb, 0));
                    break;

                case (byte)DATUM_TYPE.BOOL:
                    this.data.Add((this.rgbBuffer[ib++] != 0).ToString());
                    break;
                case (byte)DATUM_TYPE.CHAR:
                    {
                    StringBuilder s = new StringBuilder();
                    s.Append((char)(this.rgbBuffer[ib++]));
                    this.data.Add(s.ToString());
                    }
                    break;

                case (byte)DATUM_TYPE.STRING:
                    {
                    int cch = rgbBuffer[ib++];
                    StringBuilder s = new StringBuilder();
                    for (int ich = 0; ich < cch; ich++)
                        {
                        s.Append((char)rgbBuffer[ib++]);
                        }
                    this.data.Add(s.ToString());
                    }
                    break;

                case (byte)DATUM_TYPE.ENDOFRECORDSET:
                    this.fEndOfRecordSet = true;
                    fDone = true;
                    break;

                default:
                    fDone = true;
                    break;
                    }
                }
            }

        int isheetIndex { get 
        // return the zero-based sheet index in which we are to post this data.
        // By default, this is the first sheet in the workbook, but the program
        // can optionally indicate a different choice.
            {
            if (this.rgbBuffer.Length > 0)
                {
                return ((this.rgbBuffer[0] >> 4) & 0x0F);
                }
            else
                {
                return 0;
                }
            }}

        //--------------------------------------------------------------------------
        // Excel communication
        //--------------------------------------------------------------------------

        // Return the name of this cell in "A1" notation. Note that iCol & iRow here are zero-based
        public static string CellName(int iRow, int iCol)
            {
            char ch = (char)((iCol % 26) + 'A');
            StringBuilder result = new StringBuilder();
            result.Append(ch);
            //
            while (iCol > 26)
                {
                result.Append(ch);
                iCol -= 26;
                }                
            //
            result.Append((iRow+1).ToString());
            return result.ToString();
            }

        // Send the parsed data to the appropriate sheet at the next location therein
        // as recorded by the sheet's cursor.
        public void PostToSheet()
            {
            //// Make sure we have the right sheet
            //if (null != Program.TelemetryContext.Sheet)
            //    {
            //    int jsheetIndex = this.isheetIndex + 1;
            //    if (Program.TelemetryContext.Sheet.Index != jsheetIndex)
            //        {
            //        Excel.Workbook wb = Program.TelemetryContext.Workbook;
            //        //
            //        while (wb.Worksheets.Count < jsheetIndex)
            //            {
            //            wb.Worksheets.Add(After: wb.Worksheets[wb.Worksheets.Count]);
            //            }
            //        //
            //        Program.TelemetryContext.Sheet = wb.Worksheets[jsheetIndex];
            //        }
            //    }

            //// Put the data on the sheet, and advance the cursor so that the 
            //// next record won't overwrite it
            //if (null != Program.TelemetryContext.Sheet)
            //    {
            //    TelemetryContext.Cursor cursor;
            //    if (!Program.TelemetryContext.Cursors.TryGetValue(Program.TelemetryContext.Sheet.Index, out cursor))
            //        {
            //        cursor = Program.TelemetryContext.InitCursor(0,0);
            //        }

            //    Excel.Range range = Program.TelemetryContext.Sheet.get_Range(
            //        CellName(cursor.iRow, cursor.iCol+0), 
            //        CellName(cursor.iRow, cursor.iCol+data.Count-1)
            //        );
            //    range.set_Value(value: data.ToArray());

            //    cursor.iRow++;
            //    }
            }
        }
    }
