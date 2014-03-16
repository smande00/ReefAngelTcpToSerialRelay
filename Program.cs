using System;
using System.Configuration;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace TCPToSerialRelay
{
    internal class Program
    {
        private static Socket tcpListener;
        private static SocketPermission permission;
        private static Socket tcpSocket;
        private static IPEndPoint ipEndPoint;
        private static SerialPort serialPort;

        private static void OnSerialPortData(object sender, SerialDataReceivedEventArgs e)
        {
            var data = serialPort.ReadExisting();
            try
            {
                tcpSocket.Send(Encoding.UTF8.GetBytes(data));
            
            }
            catch (Exception)
            {
                Console.WriteLine(data);                
             
            }

        }

        private static void OnTCPAcceptEvent(IAsyncResult res)
        {
            var listener = (Socket)res.AsyncState;
            tcpSocket = listener.EndAccept(res);

            var buffer = new byte[1024];
            var obj = new object[2];
            obj[0] = buffer;
            obj[1] = tcpSocket;

            tcpSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnTCPReceiveEvent,obj);
            AsyncCallback aCallback = OnTCPAcceptEvent;
            listener.BeginAccept(aCallback, listener); 
        }

        private static void OnTCPReceiveEvent(IAsyncResult res)
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
                    data += Encoding.ASCII.GetString(buffer, 0,bytesRead);

                    byte[] newBuffer = new byte[1024];
                    obj[0] = newBuffer;
                    obj[1] = tcpSocket;
                    tcpSocket.BeginReceive(newBuffer, 0, newBuffer.Length, SocketFlags.None, OnTCPReceiveEvent, obj);
                 
                    serialPort.Write(data);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        } 

        private static void Main(string[] args)
        {
            var tcpPort = Convert.ToInt32(ConfigurationManager.AppSettings["tcpPort"]);

            permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", tcpPort);
            permission.Demand();
            var IPHostEntry = Dns.GetHostEntry("");
            var ipAddr = IPHostEntry.AddressList[1];
            ipEndPoint = new IPEndPoint(ipAddr, tcpPort);
            tcpListener = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            tcpListener.Bind(ipEndPoint);
            tcpListener.Listen(5);

            AsyncCallback aCallback = OnTCPAcceptEvent;
            tcpListener.BeginAccept(aCallback, tcpListener);

            serialPort = new SerialPort();
            serialPort.PortName = ConfigurationManager.AppSettings["comPort"].ToString();
            serialPort.BaudRate = Convert.ToInt32(ConfigurationManager.AppSettings["comBaud"]);
            serialPort.DataReceived += OnSerialPortData;
            serialPort.Open();
            if (serialPort.IsOpen)
            {
                Console.WriteLine("opened port");
            }
            Console.ReadKey();
        }
    }
}
