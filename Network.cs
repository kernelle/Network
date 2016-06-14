using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Network
{
    public class TCP
    {
        public TCP(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            Socket newsock = new Socket(AddressFamily.InterNetwork,
                                   SocketType.Stream, ProtocolType.Tcp);
            newsock.Bind(localEndPoint);
            newsock.Listen(10);
            socket = newsock.Accept();
        }

        public TCP(IPAddress ip, int port)
        {
            IPEndPoint ipep =
               new IPEndPoint(ip, port);
            socket = new Socket(AddressFamily.InterNetwork,
                             SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(ipep);
        }
        
        public Socket socket { get; set; }

        public void SendData(string data)
        {
            byte[] byData = null;
            byData = Encoding.ASCII.GetBytes(data);
            socket.Send(byData);
        }

        public void SendData(Bitmap data)
        {
            SendData("#PictureStart");
            string datatmp = "";
            while (!datatmp.Contains("#PictureStart"))
            { datatmp = RecieveDataString();}

            byte[] byData = null;
            byData = ImageToByte(data);
            socket.Send(byData);

            datatmp = "";
            while (!datatmp.Contains("#PictureOK"))
            {datatmp = RecieveDataString(); }

            SendData("#PictureOK");
        }

        public byte[] RecieveDataByte(int sizeBuffer = 9999999)
        {
            byte[] buffer = new byte[sizeBuffer];
            int iRx = socket.Receive(buffer);
            return buffer;
        }

        public string RecieveDataString(int sizeBuffer = 9999999)
        {
            byte[] buffer = new byte[sizeBuffer];
            int iRx = socket.Receive(buffer);
            char[] chars = new char[iRx];

            Decoder d = Encoding.UTF8.GetDecoder();
            int charLen = d.GetChars(buffer, 0, iRx, chars, 0);
            return (new string(chars));
        }

        public Bitmap RecieveDataImage(int sizeBuffer = 9999999)
        {
            string data = "";
            while (!data.Contains("#PictureStart"))
            {data = RecieveDataString(); }

            SendData("#PictureStart");

            Bitmap btmp;
            byte[] buffer = new byte[sizeBuffer];
            int iRx = socket.Receive(buffer);

            using (var ms = new MemoryStream(buffer))
            {btmp = new Bitmap(ms);}

            SendData("#PictureOK");
            data = "";
            while (!data.Contains("#PictureOK"))
            {data = RecieveDataString(); }
            
            return btmp;
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }
    }


    public class UDP
    {
        public UDP(IPAddress ip, int port)
        {
            socket = new UdpClient();
            IPEndPoint ep = new IPEndPoint(ip, port); // endpoint where server is listening
            socket.Connect(ep);
            // ex: 25252 UDP & 25254 TCP
            // TCP for buffer management
            socketTCPComms = new TCP(ip, port + 2);
        }

        public UDP(int port)
        {
            socket = new UdpClient(port);
            remoteEPPriv = new IPEndPoint(IPAddress.Any, port);
            // ex: 25252 UDP & 25254 TCP
            // TCP for buffer management
            socketTCPComms = new TCP(port + 2);
        }

        UdpClient socket { get; set; }
        TCP socketTCPComms { get; set; }
        private IPEndPoint remoteEPPriv { get; set; }

        public Bitmap RecieveImage()
        {
            var remoteEP = remoteEPPriv;

            string data = "";
            while (data == "")
            {
                data = socketTCPComms.RecieveDataString();
            }
            socketTCPComms.SendData("#PictureStart");

            byte[] iRx = socket.Receive(ref remoteEP);
            byte[] imageBytesFinal = null;
            if (iRx.Length <= 1500)
            {
                int recieved = iRx.Length;
                List<byte> tmpList = new List<byte>();
                for (int i = 0; i < iRx.Length; i++)
                {
                    tmpList.Add(iRx[i]);
                }
                int parts = 1;
                while (recieved >= 1500)
                {
                    parts++;
                    byte[] tmpData = socket.Receive(ref remoteEP);
                    for (int i = 0; i < tmpData.Length; i++)
                    {
                        tmpList.Add(tmpData[i]);
                    }
                    recieved = tmpData.Length;

                }

                imageBytesFinal = new byte[tmpList.Count];
                for (int i = 0; i < tmpList.Count; i++)
                {
                    imageBytesFinal[i] = tmpList[i];
                }

                data = "";
                while (data == "")
                {
                    data = socketTCPComms.RecieveDataString();
                }


                socketTCPComms.SendData("#PictureOK");

            }

            Bitmap btmp;
            using (var ms = new MemoryStream(imageBytesFinal))
            {
                btmp = new Bitmap(ms);
            }

            return btmp;

        }

        public void SendImage(Bitmap image)
        {

            socketTCPComms.SendData("#PictureStart");
            string data = "";
            while (data == "")
            {
                data = socketTCPComms.RecieveDataString();
            }

            byte[] byData = null;
            byData = TCP.ImageToByte(image);
            int parts = 0;

            int debugcount = 0;
            if (byData.Length >= 1500)
            {
                parts = byData.Length / 1500;
                int datalenth = byData.Length;
                parts += 1;

                for (int i = 0; i < parts; i++)
                {
                    int remaining = datalenth - (i * 1500);

                    int forloopremain = 1500;
                    if (remaining <= 1500)
                    {
                        forloopremain = remaining;
                    }
                    byte[] tmpData = new byte[forloopremain];
                    for (int i2 = 0; i2 < forloopremain; i2++)
                    {
                        tmpData[i2] = byData[i * 1500 + i2];
                    }
                    debugcount++;
                    socket.Send(tmpData, tmpData.Length);

                    System.Threading.Thread.Sleep(1);
                }
            }

            socketTCPComms.SendData("#PictureOK");

            data = "";
            while (data == "")
            {
                data = socketTCPComms.RecieveDataString();

            }
        }
    }
}
