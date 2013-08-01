using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RFID.Utils
{
    class Log
    {
        static public void Info(String tag, String message)
        {
            Console.WriteLine("{0}    :{1}", tag, message);
        }

        static public void Error(String tag, String message)
        {
            Console.WriteLine("{0}    :{1}", tag, message);
        }
    }
}
