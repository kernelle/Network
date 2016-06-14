# Network
C# Network Communications made easy


Usage:

Server TCP:

 - TCP socket = new TCP(port);
 - socket.sendData(string);

Client TCP:

- TCP socket = new TCP(ip,port);
- string test = socket.RecieveDataString();


Server UDP:

- UDP socket = new UDP(port);
- socket.SendImage(bitmap);//i would recommend using tcp.SendImage for large images


Client UDP:

- UDP socket = new UDP(ip,port);
- Bitmap image = socket.RecieveImage();
