using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RFID.NativeInterface;
using RFID.Utils;
using System.Threading;
using System.Runtime.InteropServices;

namespace RFID
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Form1 : Form
    {
        RFIDReader mReader;

        public Form1()
        {
            InitializeComponent();
        }

        private Thread mSeekingThread;


        public void onJavaScriptCall(string message)
        {
            MessageBox.Show(message);
        }

        public void onJavaScriptWriteData(string value)
        {
            Log.Info("onJavaScriptWriteData:", value);

            mReader.EnableCheck = false;

            RFIDReader.Sleep(200);

            mReader.ConnectDevice();
            mReader.BindCard();
            mReader.ReadData(15, false);
            RFIDReader.Sleep(200);
            mReader.WriteDataToBlock(15, 0, false, "FFFFFFFFFFFFF", value);
            mReader.EnableCheck = true;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            webBrowser.Navigate("d:\\test.htm");
            webBrowser.ObjectForScripting = this;
            webBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);

            mReader = RFIDReader.OpenRFIDReader();
            if (mReader.ConnectDevice())
            {
                callJsSetReaderStatus("reader connected");
            }

            startThread();
        }

        void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            
        }

        private void startThread()
        {
            mSeekingThread = new Thread(delegate()
            {
                while (mReader.ConnectDevice())
                {

                    if (mReader.BindCard(true))
                    {

                        Log.Info(Thread.CurrentThread.Name, "reader binded");

                        RunInUIThread(new ThreadStart(delegate()
                        {
                            callJsSetCardNo(mReader.CurrentCardNo);
                        }));

                        for (int i = 15; i < 16; i++)
                        {
                            RFID.NativeInterface.RFIDReader.RFIDSectorData data = mReader.ReadData(i, false);
                            if (data == null)
                                continue;
                            Log.Info("", i.ToString() + "----------------------------");
                            Log.Info("", data.Block0);
                            Log.Info("", data.Block1);
                            Log.Info("", data.Block2);
                            Log.Info("", data.PasswordA + " " + data.Conrol + " " + data.PasswordB);

                            RunInUIThread(new ThreadStart(delegate()
                            {
                                callJsSetCardValue(data.Block0);
                            }));
                        }
                        //break;

                        mReader.CheckingCard();
                        RunInUIThread(new ThreadStart(delegate()
                        {
                            callJsSetCardNo("");
                        }));
                    }

                }

                Log.Info(Thread.CurrentThread.Name, "thread abort");
            });
            mSeekingThread.Name = "reader thread" + DateTime.Now.ToShortTimeString();
            mSeekingThread.Priority = ThreadPriority.Highest;
            mSeekingThread.Start();
        }

        private void RunInUIThread(Delegate method)
        {
            this.BeginInvoke(method);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mSeekingThread !=null && mSeekingThread.IsAlive)
            {
                mSeekingThread.Abort();
            }
            mReader.DisconnectDevice();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            callJS("onCSharpCall", "I am from C#");
            callJS("setReaderStatus", "I am from C#");
            callJS("setCardNo", "22222222222");
        }

        private void callJS(string methodName, string arg)
        {
            object[] objects = new object[1];
            objects[0] = arg;

            webBrowser.Document.InvokeScript(methodName, objects);
        }

        private void callJsSetReaderStatus(string arg)
        {
            callJS("setReaderStatus", arg);
        }

        private void callJsSetCardNo(string arg)
        {
            callJS("setCardNo", arg);
        }

        private void callJsSetCardStatus(string arg)
        {
            callJS("setCardStatus", arg);
        }

        private void callJsSetCardValue(string arg)
        {
            callJS("setCardValue", arg);
        }

    }
}
