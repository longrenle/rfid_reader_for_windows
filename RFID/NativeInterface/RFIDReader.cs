using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using RFID.Utils;

namespace RFID.NativeInterface
{
    class RFIDReader
    {
        /// <summary>
        /// Native Interface
        /// </summary>
        #region Native Interface

        [DllImport("kernel32.dll")]
        public static extern void Sleep(int dwMilliseconds);

        [DllImport("MasterRD.dll")]
        static extern int rf_beep(short icdev,char msec);

        /******** 功能：控制灯的颜色 *****************************/
        //  参数：icdev：通讯设备标识符
        //        color: 0 ,熄灯
        //               1 ,红灯
        //               2 ,绿灯
        //               3 ,黄灯
        //  返回：成功返回0
        /*********************************************************/
        [DllImport("MasterRD.dll")]
        static extern int rf_light(short icdev,char color);

        [DllImport("MasterRD.dll")]
        static extern int lib_ver(ref uint pVer);

        [DllImport("MasterRD.dll")]
        static extern int rf_init_com(int port, int baud);

        [DllImport("MasterRD.dll")]
        static extern int rf_ClosePort();

        [DllImport("MasterRD.dll")]
        static extern int rf_antenna_sta(short icdev, byte mode);

        [DllImport("MasterRD.dll")]
        static extern int rf_init_type(short icdev, byte type);

        [DllImport("MasterRD.dll")]
        static extern int rf_request(short icdev, byte mode, ref ushort pTagType);

        [DllImport("MasterRD.dll")]
        static extern int rf_anticoll(short icdev, byte bcnt, IntPtr pSnr, ref byte pRLength);

        [DllImport("MasterRD.dll")]
        static extern int rf_select(short icdev, IntPtr pSnr, byte srcLen, ref sbyte Size);

        [DllImport("MasterRD.dll")]
        static extern int rf_halt(short icdev);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_authentication2(short icdev, byte mode, byte secnr, IntPtr key);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_initval(short icdev, byte adr, Int32 value);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_increment(short icdev, byte adr, Int32 value);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_decrement(short icdev, byte adr, Int32 value);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_readval(short icdev, byte adr, ref Int32 pValue);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_read(short icdev, byte adr, IntPtr pData, ref byte pLen);

        [DllImport("MasterRD.dll")]
        static extern int rf_M1_write(short icdev, byte adr, IntPtr pData);

        #endregion

        /// <summary>
        /// Constant Definition
        /// </summary>
        #region Constant Definition

        int[] PORT_VALUES = {   1,
                                2,
                                3,
                                4,
                                5,
                                6,
                                7,
                                8};

        int[] BAUD_VALUES = {   9600,
                                14400,
                                19200,
                                28800,
                                38400,
                                57600,
                                115200};

        #endregion

        /// <summary>
        /// Date Converse Function
        /// </summary>
        #region Date Converse Function

        private static char[] hexDigits = { 
            '0','1','2','3','4','5','6','7',
            '8','9','A','B','C','D','E','F'};

        private static byte GetHexBitsValue(byte ch)
        {
            byte sz = 0;
            if (ch <= '9' && ch >= '0')
                sz = (byte)(ch - 0x30);
            if (ch <= 'F' && ch >= 'A')
                sz = (byte)(ch - 0x37);
            if (ch <= 'f' && ch >= 'a')
                sz = (byte)(ch - 0x57);

            return sz;
        }


        /// <summary>
        /// 单个字节转字字符.
        /// </summary>
        /// <param name="ib">字节.</param>
        /// <returns>转换好的字符.</returns>
        private static String byteHEX(Byte ib)
        {
            String _str = String.Empty;
            try
            {
                char[] Digit = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A',
			    'B', 'C', 'D', 'E', 'F' };
                char[] ob = new char[2];
                ob[0] = Digit[(ib >> 4) & 0X0F];
                ob[1] = Digit[ib & 0X0F];
                _str = new String(ob);
            }
            catch (Exception)
            {
                new Exception("System Error");
            }
            return _str;

        }

        private static string ToHexString(byte[] bytes)
        {
            String hexString = String.Empty;
            for (int i = 0; i < bytes.Length; i++)
                hexString += byteHEX(bytes[i]);

            return hexString;
        }

        public static byte[] ToDigitsBytes(string theHex)
        {
            byte[] bytes = new byte[theHex.Length / 2 + (((theHex.Length % 2) > 0) ? 1 : 0)];
            for (int i = 0; i < bytes.Length; i++)
            {
                char lowbits = theHex[i * 2];
                char highbits;

                if ((i * 2 + 1) < theHex.Length)
                    highbits = theHex[i * 2 + 1];
                else
                    highbits = '0';

                int a = (int)GetHexBitsValue((byte)lowbits);
                int b = (int)GetHexBitsValue((byte)highbits);
                bytes[i] = (byte)((a << 4) + b);
            }

            return bytes;
        }

        #endregion

        public class RFIDSectorData
        {
            public string Block0;
            public string Block1;
            public string Block2;

            public string PasswordA;
            public string Conrol;
            public string PasswordB;
        }

        static private string TAG = "RFID.NativeInterface.RFIDReader";

        private RFIDReader()
        {
 
        }

        short icdev = 0x0000;

        private static RFIDReader mReader = null;

        public static RFIDReader OpenRFIDReader()
        {
            if(mReader == null)
            {
                mReader = new RFIDReader();
            }
            return mReader;
        }

        private bool mIsDeviceConnected = false;
        private bool mIsCardBinded = false;
        private string mCurrentCardNo;

        private bool mEnableCheck = true;

        public bool EnableCheck
        {
            get { return mEnableCheck; }
            set { mEnableCheck = value; }
        }

        public String CurrentCardNo
        {
            get {return mCurrentCardNo; }
        }

        private enum ReaderLight
        {
            Idel,
            Close,
            Red,
            Green,
            RedAndGreen
        }

        private ReaderLight mCurrentLight = ReaderLight.Idel;
        private ReaderLight CurrentLight
        {
            get { return mCurrentLight; }
            set
            {
                if (mCurrentLight == value)
                {
                    return;
                }
                mCurrentLight = value;

                rf_light(icdev, (char)0);
                switch (mCurrentLight)
                {
                    case ReaderLight.Close:
                        break;
                    case ReaderLight.Red:
                        rf_light(icdev, (char)1);
                        break;
                    case ReaderLight.Green:
                        rf_light(icdev, (char)2);
                        break;
                    case ReaderLight.RedAndGreen:
                        rf_light(icdev, (char)1);
                        rf_light(icdev, (char)2);
                        break;
                    default:
                        break;
                }
                Sleep(10);
                rf_beep(icdev, (char)1);
            }
        }

        private bool IsCardBinded
        {
            get { return this.mIsCardBinded; }
            set
            {
                this.mIsCardBinded = value;
                if (this.mIsCardBinded)
                {
                    CurrentLight = ReaderLight.RedAndGreen;
                }
                else
                {
                    this.mCurrentCardNo = "";
                    CurrentLight = ReaderLight.Red;
                }
            }
        }

        public bool ConnectDevice()
        {
            int port = PORT_VALUES[3];
            int baud = BAUD_VALUES[2];
            int status;

            status = rf_init_com(port, baud);

            for (int i = 0; i < PORT_VALUES.Length && 0 != status; i++)
            {
                status = rf_init_com(PORT_VALUES[i], baud);
            }

            mIsDeviceConnected = (0 == status);
            if (mIsDeviceConnected)
            {
                CurrentLight = ReaderLight.Red;
            }
            return mIsDeviceConnected;
        }

        public void DisconnectDevice()
        {
            mIsDeviceConnected = false;
            CurrentLight = ReaderLight.Close;
            Sleep(10);
            rf_ClosePort();
        }

        public void CheckingCard()
        {
            short icdev = 0x0000;
            int status = 0;
            byte mode = 0x52;
            ushort TagType = 0;

            while (status == 0)
            {
                Log.Info(TAG, "checking card");
                Sleep(50);
                if (mEnableCheck)
                {
                    status = rf_request(icdev, mode, ref TagType);
                }
            }
            IsCardBinded = false;

        }

        public bool BindCard()
        {
            return BindCard(false);
        }

        public bool BindCard(bool isWaiting)
        {            
            short icdev = 0x0000;
            int status;
            byte type = (byte)'A';//mifare one 卡询卡方式为A
            byte mode = 0x52;
            ushort TagType = 0;
            byte bcnt = 0x04;//mifare 卡都用4
            IntPtr pSnr;
            byte len = 255;
            sbyte size = 0;

            if (!this.mIsDeviceConnected)
            {
                IsCardBinded = false;
                Log.Info(TAG, "Device not connect!");
                return false;
            }

            IsCardBinded = false;
            mCurrentCardNo = String.Empty;

            pSnr = Marshal.AllocHGlobal(1024);

            for (int i = 0; i < 2 && mCurrentCardNo == String.Empty; i++)
            {
                status = rf_antenna_sta(icdev, 0);//关闭天线
                if (status != 0)                
                    continue;
                
                Sleep(20);
                status = rf_init_type(icdev, type);
                if (status != 0)
                    continue;

                Sleep(20);
                status = rf_antenna_sta(icdev, 1);//启动天线
                if (status != 0)
                    continue;

                
                do
                {
                    Sleep(50);
                    status = rf_request(icdev, mode, ref TagType);//搜寻所有的卡
                    Log.Info(TAG, "waiting card");
                } while (isWaiting && status != 0);
                if (status != 0)
                    continue;


                Sleep(50);
                status = rf_anticoll(icdev, bcnt, pSnr, ref len);//返回卡的序列号
                if (status != 0)
                    continue;

                status = rf_select(icdev, pSnr, len, ref size);//锁定一张ISO14443-3 TYPE_A 卡
                if (status != 0)
                    continue;

                byte[] szBytes = new byte[len];
               
                for (int j = 0; j < len; j++)
                {
                    szBytes[j] = Marshal.ReadByte(pSnr, j);
                }

                String cardNo = String.Empty;

                for (int q = 0; q < len; q++)
                {
                    cardNo += byteHEX(szBytes[q]);
                }
                mCurrentCardNo = cardNo;
                Log.Info(TAG, "mCurrentCardNo:" + mCurrentCardNo);

                IsCardBinded = true;
            }

            Marshal.FreeHGlobal(pSnr);

            return mIsCardBinded;
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="sectorNo">扇区号</param>
        /// <param name="isKeyB">是否B密钥</param>
        /// <returns></returns>
        public RFIDSectorData ReadData(int sectorNo, bool isKeyB)
        {
            return ReadData(sectorNo, isKeyB, "FFFFFFFFFFFFF");
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="sectorNo">扇区号</param>
        /// <param name="isKeyB">是否B密钥</param>
        /// <param name="password">密钥</param>
        /// <returns></returns>
        public RFIDSectorData ReadData(int sectorNo,bool isKeyB, string password)
        {
            short icdev = 0x0000;
            int status;
            byte mode = 0x60;
            byte secnr = 0x00;

            if (!this.mIsDeviceConnected)
            {
                IsCardBinded = false;
                Log.Info(TAG, "Device not connect!");
                return null;
            }

            RFIDSectorData data = new RFIDSectorData();

            if (isKeyB)
                mode = 0x61; //密钥

            secnr = Convert.ToByte(sectorNo.ToString());

            IntPtr keyBuffer = Marshal.AllocHGlobal(256);

            byte[] bytesKey = ToDigitsBytes(password);
            for (int i = 0; i < bytesKey.Length; i++)
                Marshal.WriteByte(keyBuffer, i * Marshal.SizeOf(typeof(Byte)), bytesKey[i]);
            status = rf_M1_authentication2(icdev, mode, (byte)(secnr * 4), keyBuffer);
            Marshal.FreeHGlobal(keyBuffer);
            if (status != 0)
            {
                Log.Info("ReadData", "rf_M1_authentication2 failed!");
                return null;
            }

            //
            IntPtr dataBuffer = Marshal.AllocHGlobal(256);
            for (int i = 0; i < 4; i++)
            {
                int j;
                byte cLen = 0;
                status = rf_M1_read(icdev, (byte)((secnr * 4) + i), dataBuffer, ref cLen);

                if (status != 0 || cLen != 16)
                {
                    Marshal.FreeHGlobal(dataBuffer);
                    Log.Info(TAG, "rf_M1_read failed!");
                    this.IsCardBinded = false;
                    return null;
                }

                byte[] bytesData = new byte[16];
                for (j = 0; j < bytesData.Length; j++)
                    bytesData[j] = Marshal.ReadByte(dataBuffer, j);

                if (i == 0)
                    data.Block0 = ToHexString(bytesData);
                else if (i == 1)
                    data.Block1 = ToHexString(bytesData);
                else if (i == 2)
                    data.Block2 = ToHexString(bytesData);
                else if (i == 3)
                {
                    byte[] byteskeyA = new byte[6];
                    byte[] byteskey = new byte[4];
                    byte[] byteskeyB = new byte[6];

                    for (j = 0; j < 16; j++)
                    {
                        if (j < 6)
                            byteskeyA[j] = bytesData[j];
                        else if (j >= 6 && j < 10)
                            byteskey[j - 6] = bytesData[j];
                        else
                            byteskeyB[j - 10] = bytesData[j];
                    }

                    data.PasswordA = ToHexString(byteskeyA);
                    data.Conrol = ToHexString(byteskey);
                    data.PasswordB = ToHexString(byteskeyB);
                }
            }
            Marshal.FreeHGlobal(dataBuffer);
            Log.Info(TAG, data.ToString());
            return data;
        }

        /// <summary>
        /// 写卡
        /// </summary>
        /// <param name="sectorNo">扇区号</param>
        /// <param name="blockNo">扇区内快号</param>
        /// <param name="isKeyB">是否B密钥</param>
        /// <param name="password">密钥</param>
        /// <param name="data">快写入的数据</param>
        /// <returns></returns>
        public bool WriteDataToBlock(int sectorNo, int blockNo, bool isKeyB, string password, string data)
        {
            short icdev = 0x0000;
            int status;
            byte mode = 0x60;
            byte secnr = 0x00;
            byte adr;
            int i;

            if (!this.mIsDeviceConnected)
            {
                IsCardBinded = false;
                Log.Info(TAG, "Device not connect!");
                return false;
            }

            if (isKeyB)
                mode = 0x61; //密钥

            secnr = Convert.ToByte(sectorNo.ToString());
            adr = (byte)(Convert.ToByte(blockNo.ToString()) + secnr * 4);

            IntPtr keyBuffer = Marshal.AllocHGlobal(1024);

             

            byte[] bytesBlock = ToDigitsBytes(data);
            

            IntPtr dataBuffer = Marshal.AllocHGlobal(1024);

            for (i = 0; i < bytesBlock.Length; i++)
                Marshal.WriteByte(dataBuffer, i, bytesBlock[i]);
            status = rf_M1_write(icdev, adr, dataBuffer);
            Marshal.FreeHGlobal(dataBuffer);

            if (status != 0)
            {
                return false;
            }
            return true;
        }
    }
}
