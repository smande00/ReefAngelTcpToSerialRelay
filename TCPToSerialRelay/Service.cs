using System;
using System.Configuration;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.ServiceProcess;
using System.Text;
using System.Threading;


namespace ReefAngelTCPRelay

{

    
    public class ReefAngelTCPRelayApp
    {
        private Socket tcpListener;
        private SocketPermission permission;
        private Socket tcpSocket;
        private IPEndPoint ipEndPoint;
        private SerialPort serialPort;

        public ReefAngelTCPRelayApp()
        {
            var ePermission = new EventLogPermission( );            
            ePermission.Demand();
            
            if (!EventLog.SourceExists("ReefAngel TCP Relay"))
                EventLog.CreateEventSource("ReefAngel TCP Relay", "Application");


        }

        private void LogInfo(string message)
        {
            EventLog.WriteEntry("ReefAngel TCP Relay" , message, EventLogEntryType.Information);
        }

        private void LogException(string methodName, Exception ex)
        {
            EventLog.WriteEntry("ReefAngel TCP Relay", methodName + ":" + ex.Message, EventLogEntryType.Error);
        }

        private void OnSerialPortData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var data = serialPort.ReadExisting();
                if(tcpSocket != null && tcpSocket.Connected)
                    tcpSocket.Send(Encoding.UTF8.GetBytes(data));

            }
            catch (Exception ex)
            {

                LogException("OnSerialPortData", ex);
            }

        }

        private void OnTCPAcceptEvent(IAsyncResult res)
        {            
            try
            {
                var listener = (Socket)res.AsyncState;
                tcpSocket = listener.EndAccept(res);

                var buffer = new byte[1024];
                var obj = new object[2];
                obj[0] = buffer;
                obj[1] = tcpSocket;

                tcpSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnTCPReceiveEvent, obj);
                AsyncCallback aCallback = OnTCPAcceptEvent;
                listener.BeginAccept(aCallback, listener);
            }
            catch (Exception ex)
            {
                LogException("OnTCPAcceptEvent", ex);
            }
        }

        private void OnTCPReceiveEvent(IAsyncResult res)
        {
            try
            {
                var obj = new object[2];
                obj = (object[])res.AsyncState;

                byte[] buffer = (byte[])obj[0];
                tcpSocket = (Socket)obj[1];

                var data = string.Empty;

                int bytesRead = tcpSocket.EndReceive(res);

                if (bytesRead > 0)
                {
                    data += Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    byte[] newBuffer = new byte[1024];
                    obj[0] = newBuffer;
                    obj[1] = tcpSocket;
                    tcpSocket.BeginReceive(newBuffer, 0, newBuffer.Length, SocketFlags.None, OnTCPReceiveEvent, obj);
                    OpenSerialPort();
                    serialPort.Write(data);
                }
            }
            catch (Exception ex)
            {
                LogException("OnTCPReceiveEvent", ex);
            }
        }

        private void OpenSerialPort()
        {
            try
            {
                if (!serialPort.IsOpen)
                {                    
                    serialPort.Open();
                }
            }
            catch (Exception ex)
            {
                LogException("OpenSerialPort", ex);
            }
        }

        public void Startup()
        {
            try
            {

                LogInfo("starting up");
                var tcpPort = Convert.ToInt32(ConfigurationManager.AppSettings["tcpPort"]);

                //permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", tcpPort);
                //permission.Demand();
                var IPHostEntry = Dns.GetHostEntry("");
                var ipAddr = IPHostEntry.AddressList[1];
                ipEndPoint = new IPEndPoint(ipAddr, tcpPort);
                tcpListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                
                tcpListener.Bind(ipEndPoint);
                tcpListener.Listen(5);

                
                tcpListener.BeginAccept(OnTCPAcceptEvent, tcpListener);
                LogInfo("listening on:" + ipEndPoint.ToString());
                serialPort = new SerialPort();
                serialPort.PortName = ConfigurationManager.AppSettings["comPort"].ToString();
                serialPort.BaudRate = Convert.ToInt32(ConfigurationManager.AppSettings["comBaud"]);
                serialPort.DataReceived += OnSerialPortData;
                serialPort.Open();
                if (serialPort.IsOpen)
                    LogInfo("serial open");

                

            }
            catch (Exception ex)
            {
                LogException("OnStart", ex);
            }
        }

        public void Shutdown()
        {
            serialPort.Dispose();
            tcpListener.Dispose();
            tcpListener.Dispose();
        }
    }


    public class ReefAngelTCPRelayService : ServiceBase
    {
        private ReefAngelTCPRelayApp app = new ReefAngelTCPRelayApp();
        private Thread worker;
        private bool stopFlag = false;
        public ReefAngelTCPRelayService()
        {
            ServiceName = "ReefAngel TCP Relay";
            CanHandlePowerEvent = true;
            CanHandleSessionChangeEvent = true;
            CanPauseAndContinue = true;
            CanShutdown = true;
            CanStop = true;

        }


        void WorkerThread()
        {
            app.Startup();
            while (!stopFlag)
            {
                
            }
            app.Shutdown();
        }

        protected override void OnStart(string[] args)
        {
            worker= new Thread(WorkerThread);
            worker.Start();
         
        }

        protected override void OnStop()
        {
            stopFlag = true;
            
        }
    }

}
