using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using Network;

namespace networkPeripherals
{
    public partial class FormNetwork : Form
    {
        public FormNetwork()
        {
            InitializeComponent();
            /*this.BackColor = Color.Fuchsia;
            TransparencyKey = Color.Fuchsia;*/
            //SetupKeyboardHooks();labelStatus.Visible = true;labelMouse.Visible = true;
            try
            {
                //textBoxIp.Text = Dns.GetHostEntry(Dns.GetHostName()).AddressList[8].ToString();
            }
            catch (Exception)
            {

            }
            this.Focus();
            this.Select();
        }
        int x = Cursor.Position.X;
        int y = Cursor.Position.Y;

        private void EventSend(object sender, EventArgs e)
        {
            string data = sender.ToString();
            string tosend = "";
            switch (data)
            {
                case "WM_LBUTTONDOWN":
                    leftclick = true;
                    break;
                case "WM_LBUTTONUP":
                    leftclick = false;
                    break;
                case "WM_MOUSEWHEEL.up":
                    tosend += "#WM_MOUSEWHEEL.up";
                    break;
                case "WM_MOUSEWHEEL.down":
                    tosend += "#WM_MOUSEWHEEL.down";
                    break;
                case "WM_RBUTTONDOWN":
                    rightclick = true;
                    if (middledown)
                    {
                        toggle = !toggle;

                        if (toggle)
                        {
                            //this.Opacity = 0.5;
                            SetupKeyboardHooks();
                            labelStatus.Text = "Status: Enabled ";
                            DisableMouseClicks();
                            x = Cursor.Position.X;
                            y = Cursor.Position.Y;
                            // GoFullscreen(true);
                            Save(this);
                            Maximize(this);
                        }
                        else
                        {
                            this.Opacity = 1;
                            //GoFullscreen(false);
                            Restore(this);
                            EnableMouseClicks();
                            DisposeKeyboard();
                            labelStatus.Text = "Status: Disabled ";
                        }
                    }

                    break;
                case "WM_RBUTTONUP":
                    rightclick = false;
                    break;
                case "WM_MOUSEMOVE":

                    if (toggle)
                    {
                        if (xprev != xnull | yprev != ynull)
                        {
                            tosend += "#X:" + Convert.ToString(xnull) + " Y:" + Convert.ToString(ynull);
                            //this.Text = "#X:" + Convert.ToString(xnull) + " Y:" + Convert.ToString(ynull);
                        }

                        //Cursor.Position = new Point(x, y);

                        xprev = xnull;
                        yprev = ynull;
                        //sendData("#X:" + Convert.ToString(xnull) + "Y:" + Convert.ToString(ynull));
                    }

                    break;
                case "519":
                    middledown = true;
                    break;
                case "520":
                    middledown = false;
                    break;
                default:
                    //if (toggle) tosend += "#" + data;
                    break;
            }

            if (leftclick && toggle)
            {
                tosend += "#WM_LBUTTONDOWN";
            }
            else
            {
                tosend += "#WM_LBUTTONUP";
            }

            if (rightclick && toggle)
            {
                tosend += "#WM_RBUTTONDOWN";
            }
            else
            {
                tosend += "#WM_RBUTTONUP";
            }
            server.SendData(tosend + sendKeystrokes);
            sendKeystrokes = "";
            labelMouse.Text = "Mouse: " + sender.ToString();
        }
        bool toggle = false, control = false, alt = false, shift = false;
        string sendKeystrokes = "";
        private void OnKeyPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            /*if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown && e.KeyboardData.VirtualCode == 19)
            {
                toggle = !toggle;
            }*/
            KeysConverter kc = new KeysConverter();
            string keyChar = kc.ConvertToString(e.KeyboardData.VirtualCode);
            labelStatus.Text = "Status: Enabled " + e.KeyboardData.VirtualCode.ToString();//keyChar.ToString();

            switch (keyChar.ToString())
            {
                case "LControlKey":
                case "RControlKey":
                    control = !control;
                    break;
                case "LMenu":
                case "RMenu":
                    alt = !alt;
                    break;
                case "LShiftKey":
                case "RShiftKey":
                    shift = !shift;
                    break;
                default:
                    break;
            }

            if (toggle)
            {
                if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
                {
                    if (!control && !alt && !shift)
                    {

                        sendKeystrokes += "#K:" + e.KeyboardData.VirtualCode.ToString();
                        EventSend("WM_MOUSEMOVE", null);
                    }
                    else
                    {
                        if (control)
                        {
                            sendKeystrokes += "#K:" + "162+" + e.KeyboardData.VirtualCode.ToString();
                            EventSend("WM_MOUSEMOVE", null);

                        }

                        if (alt)
                        {
                            sendKeystrokes += "#K:" + "164+" + e.KeyboardData.VirtualCode.ToString();
                            EventSend("WM_MOUSEMOVE", null);
                        }

                        if (shift)
                        {
                            sendKeystrokes += "#K:" + "160+" + e.KeyboardData.VirtualCode.ToString();
                            EventSend("WM_MOUSEMOVE", null);
                        }
                    }
                }
                
            }
            else
            {
                labelStatus.Text = "Status: Disabled " + e.KeyboardData.VirtualCode.ToString();
            }
            
            e.Handled = true;
        }
        bool middledown = false;
        TCP server, client;

        public void DisposeKeyboard()
        {
            _globalKeyboardHook?.Dispose();
        }

        private void buttonListen_Click(object sender, EventArgs e)
        {
            buttonConnect.Enabled = false;
            buttonListen.Enabled = false;
            buttonConnect.Visible = false;
            buttonListen.Visible = false;
            textBoxIp.Visible = false;
            textBoxPort.Visible = false;
            
            server = new TCP(Convert.ToInt32(textBoxPort.Text));

            MouseHook.Start();
            MouseHook.MouseAction += new EventHandler(EventSend);
            portcount = Convert.ToInt32(textBoxPort.Text) + 10;

            // there is a problem with ports being reused, not recommended to add more
            // can be used to add more workers for more images per second
            workerLister.Add(new BackgroundWorker());
            workerLister.Last().DoWork += delegate (object s, DoWorkEventArgs e1) { listenerWorker(portcount + 1); };
            workerLister.Last().RunWorkerAsync();
        }

        public static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            buttonConnect.Enabled = false;
            buttonListen.Enabled = false;
            buttonConnect.Visible = false;
            buttonListen.Visible = false;
            textBoxIp.Visible = false;
            textBoxPort.Visible = false;
            
            client = new TCP(IPAddress.Parse(textBoxIp.Text), Convert.ToInt32(textBoxPort.Text));
            portcount = Convert.ToInt32(textBoxPort.Text) + 10;
            backgroundWorker1.RunWorkerAsync();
            //backgroundWorker3.RunWorkerAsync();

            workerConnector.Add(new BackgroundWorker());
            workerConnector.Last().DoWork += delegate (object s, DoWorkEventArgs e1) { senderWorker(portcount + 1); };
            workerConnector.Last().RunWorkerAsync();

        }
        List<BackgroundWorker> workerLister = new List<BackgroundWorker>();
        List<BackgroundWorker> workerConnector = new List<BackgroundWorker>();

        int portcount = 0;
        int imageCount = 0;
        Bitmap background;

        void listenerWorker(int port)
        {
            /*
            //UDP Part
            UDP server = new UDP(11000);

            while (true)
            {
                background = server.RecieveImage();

                this.Invoke(new MethodInvoker(delegate
                {
                    this.Text = "Image";
                    if (toggle)
                    {
                        pictureBox1.Image = background;

                    }
                    else
                    {
                        pictureBox1.Image = new Bitmap(100, 100);
                    }
                }));
            }*/
            //TCP Part
           // TCP works better because UDP is limited to 1500bytes per datagram
           // and more reliable
            TCP image1 = new TCP(port);
            while (true)
            {
                try
                {
                    background = image1.RecieveDataImage();
                    //background.Save("test.png");
                    imageCount++;
                    this.Invoke(new MethodInvoker(delegate
                    {
                        this.Text = "Image: " + imageCount;
                        if (toggle)
                        {
                            pictureBox1.Image = background;
                        }
                        else
                        {
                            pictureBox1.Image = new Bitmap(100, 100);
                        }
                    }));

                }
                catch (Exception)
                {

                }


            }
        }

        void senderWorker(int port)
        {
            /*
            //UDP Part
            UDP client = new UDP(IPAddress.Parse(textBoxIp.Text), port);
            try
            {
                while (true)
                {
                    client.SendImage(Screenshot());
                }
            }
            catch (Exception d)
            {
                MessageBox.Show(d.ToString());
                this.Close();
            }*/
            //TCP Part
            int x = 0;
            TCP image1 = new TCP(IPAddress.Parse(textBoxIp.Text), port);
            while (true)
            {
                try
                {
                    Bitmap tmpBitmap = Screenshot();
                    image1.SendData(tmpBitmap);
                    this.Invoke(new MethodInvoker(delegate
                    {
                        this.Text = "Count: " + x;
                        x++;

                    }));
                }
                catch (Exception)
                {

                }
            }
        }



        bool leftclick = false, rightclick = false;
        int lastKeyStroke = 0;
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (client.socket.Connected)
                {

                    string data = client.RecieveDataString(1024);


                    string[] split = data.Split('#');
                    foreach (string item in split)
                    {
                        if (item.Contains("X:"))
                        {
                            data = item;
                            try
                            {
                                int xnew = Convert.ToInt32(data.Substring(2, data.IndexOf(' ') - 1));
                                int ynew = Convert.ToInt32(data.Substring(data.IndexOf("Y:") + 2));

                                Cursor.Position = new Point(xnew, ynew);
                            }
                            catch (Exception)
                            {

                            }

                        }
                        InputSimulator inputSimulator = new InputSimulator();
                        inputSimulator.Keyboard.KeyUp((WindowsInput.Native.VirtualKeyCode)lastKeyStroke);
                        if (item.Contains("K:"))
                        {
                            if (item.Contains("+"))
                            {
                                string[] splitPlus = item.Substring(item.IndexOf("K:") + 2).Split('+');

                                int keystrokeBase = Convert.ToInt32(splitPlus[0]);
                                int keystroke1 = Convert.ToInt32(splitPlus[1]);

                                inputSimulator.Keyboard.KeyDown((WindowsInput.Native.VirtualKeyCode)keystrokeBase);
                                inputSimulator.Keyboard.KeyDown((WindowsInput.Native.VirtualKeyCode)keystroke1);
                                inputSimulator.Keyboard.KeyUp((WindowsInput.Native.VirtualKeyCode)keystrokeBase);
                                inputSimulator.Keyboard.KeyUp((WindowsInput.Native.VirtualKeyCode)keystroke1);
                            }
                            else
                            {
                                int keystroke = Convert.ToInt32(item.Substring(item.IndexOf("K:") + 2));
                                
                                inputSimulator.Keyboard.KeyDown((WindowsInput.Native.VirtualKeyCode)keystroke);
                                lastKeyStroke = keystroke;
                            }
                        }
                        if (item.Contains("WM_LBUTTONDOWN"))
                        {
                            if (!leftclick)
                            {
                                DoMouseClickDown();
                                leftclick = true;
                            }
                        }
                        if (item.Contains("WM_LBUTTONUP"))
                        {
                            if (leftclick)
                            {
                                DoMouseClickUp();
                                leftclick = false;
                            }
                        }
                        if (item.Contains("WM_MOUSEWHEEL.up"))
                        {
                            VScrollWheel(100);
                        }
                        if (item.Contains("WM_MOUSEWHEEL.down"))
                        {
                            VScrollWheel(-100);
                        }

                        if (item.Contains("WM_RBUTTONDOWN"))
                        {
                            if (!rightclick)
                            {
                                DoMouseClickRightDown();
                            }
                            rightclick = true;
                        }
                        if (item.Contains("WM_RBUTTONUP"))
                        {
                            if (rightclick)
                            {
                                DoMouseClickRightUp();
                            }
                            rightclick = false;
                        }
                    }

                    switch (data)
                    {
                        case "WM_LBUTTONDOWN":
                            break;
                        case "WM_LBUTTONUP":

                            break;
                        case "WM_MOUSEMOVE":
                            break;
                        case "WM_MOUSEWHEEL":
                            break;
                        case "WM_RBUTTONDOWN":
                            break;
                        case "WM_RBUTTONUP":

                            break;
                        default:
                            break;
                    }

                    this.Invoke(new MethodInvoker(delegate
                    {

                        //this.Text = data;
                    }));
                }
                this.Invoke(new MethodInvoker(delegate
                {

                    this.Text = "Stopped";
                }));
            }
            catch (Exception x)
            {
                MessageBox.Show(x.ToString());
                this.Close();
            }

        }
        int xnull = 0, ynull = 0;
        int xprev = 0, yprev = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (toggle)
            {
                xnull = Cursor.Position.X;
                ynull = Cursor.Position.Y;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void DisableMouseClicks()
        {
            if (this.Filter == null)
            {
                this.Filter = new MouseClickMessageFilter();
                Application.AddMessageFilter(this.Filter);
            }
        }

        private void EnableMouseClicks()
        {
            if ((this.Filter != null))
            {
                Application.RemoveMessageFilter(this.Filter);
                this.Filter = null;
            }
        }

        private MouseClickMessageFilter Filter;
        private const int LButtonDown = 0x201;
        private const int LButtonUp = 0x202;
        private const int LButtonDoubleClick = 0x203;

        public static Bitmap _resize(Image image, int width, int height)
        {
            Bitmap newImage = new Bitmap(width, height);
            //this is what allows the quality to stay the same when reducing image dimensions
            newImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(image, new Rectangle(0, 0, width, height));
            }
            return newImage;
        }

        public static Bitmap Screenshot()
        {
            var bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                               Screen.PrimaryScreen.Bounds.Height,
                               PixelFormat.Format32bppArgb);

            // Create a graphics object from the bitmap.
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            // Take the screenshot from the upper left corner to the right bottom corner.
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                        Screen.PrimaryScreen.Bounds.Y,
                                        0,
                                        0,
                                        Screen.PrimaryScreen.Bounds.Size,
                                        CopyPixelOperation.SourceCopy);
            return bmpScreenshot;
        }

        private void GoFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                this.Show();
                this.TopMost = true;
                this.WindowState = FormWindowState.Maximized;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.Bounds = Screen.PrimaryScreen.Bounds;
            }
            else
            {
                this.Hide();
                this.TopMost = false;
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                this.Size = new Size(279, 132);
            }
        }

        public static void VScrollWheel(int steps) { mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)steps, 0); }
        public static void HScrollWheel(int steps) { mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, (uint)steps, 0); }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern int SendMessage(int hWnd, int hMsg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessageW(IntPtr hWnd, int Msg,
            IntPtr wParam, IntPtr lParam);

        private void Mute()
        { SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle, (IntPtr)APPCOMMAND_VOLUME_MUTE); }
        private void VolDown()
        { SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle, (IntPtr)APPCOMMAND_VOLUME_DOWN); }
        private void VolUp()
        { SendMessageW(this.Handle, WM_APPCOMMAND, this.Handle, (IntPtr)APPCOMMAND_VOLUME_UP); }

        private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
        private const int APPCOMMAND_VOLUME_UP = 0xA0000;
        private const int APPCOMMAND_VOLUME_DOWN = 0x90000;
        public int WM_SYSCOMMAND = 0x0112;
        public int SC_MONITORPOWER = 0xF170;
        private const int WM_APPCOMMAND = 0x319;
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        public const int MOUSEEVENTF_WHEEL = 0x0800;
        public const int MOUSEEVENTF_HWHEEL = 0x1000;
        public const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        public const int MOUSEEVENTF_MIDDLEUP = 0x40;

        public void DoMouseClickDown()
        {
            //Call the imported function with the cursor's current position
            int X = Cursor.Position.X;
            int Y = Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)X, (uint)Y, 0, 0);
        }

        public void DoMouseClickUp()
        {
            //Call the imported function with the cursor's current position
            int X = Cursor.Position.X;
            int Y = Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTUP, (uint)X, (uint)Y, 0, 0);
        }

        public void DoMouseClickRightDown()
        {
            //Call the imported function with the cursor's current position
            int X = Cursor.Position.X;
            int Y = Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_RIGHTDOWN, (uint)X, (uint)Y, 0, 0);
        }

        public void DoMouseClickRightUp()
        {
            //Call the imported function with the cursor's current position
            int X = Cursor.Position.X;
            int Y = Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_RIGHTUP, (uint)X, (uint)Y, 0, 0);
        }

        private GlobalKeyboardHook _globalKeyboardHook;

        public void SetupKeyboardHooks()
        {
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyboardPressed += OnKeyPressed;
        }

        private FormWindowState winState;

        int scrolled = 0;
        private void Form1_Scroll(object sender, ScrollEventArgs e)
        {
            scrolled = e.NewValue;
        }

        private FormBorderStyle brdStyle;
        private bool topMost;
        private Rectangle bounds;

        

        private bool IsMaximized = false;


        public void Maximize(Form targetForm)
        {
            if (!IsMaximized)
            {
                IsMaximized = true;
                Save(targetForm);
                targetForm.WindowState = FormWindowState.Maximized;
                targetForm.FormBorderStyle = FormBorderStyle.None;
                targetForm.TopMost = true;
                WinApi.SetWinFullScreen(targetForm.Handle);
            }
        }

        public void Save(Form targetForm)
        {
            winState = targetForm.WindowState;
            brdStyle = targetForm.FormBorderStyle;
            topMost = targetForm.TopMost;
            bounds = targetForm.Bounds;
        }

        public void Restore(Form targetForm)
        {
            targetForm.WindowState = winState;
            targetForm.FormBorderStyle = brdStyle;
            targetForm.TopMost = topMost;
            targetForm.Bounds = bounds;
            IsMaximized = false;
        }
    }

    public class WinApi
    {
        [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
        public static extern int GetSystemMetrics(int which);

        [DllImport("user32.dll")]
        public static extern void
            SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
                         int X, int Y, int width, int height, uint flags);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private static IntPtr HWND_TOP = IntPtr.Zero;
        private const int SWP_SHOWWINDOW = 64; // 0x0040

        public static int ScreenX
        {
            get { return GetSystemMetrics(SM_CXSCREEN); }
        }

        public static int ScreenY
        {
            get { return GetSystemMetrics(SM_CYSCREEN); }
        }

        public static void SetWinFullScreen(IntPtr hwnd)
        {
            SetWindowPos(hwnd, HWND_TOP, 0, 0, ScreenX, ScreenY, SWP_SHOWWINDOW);
        }
    }

    public class MouseClickMessageFilter : IMessageFilter
    {
        private const int LButtonDown = 0x201;
        private const int LButtonUp = 0x202;
        private const int LButtonDoubleClick = 0x203;

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case LButtonDown:
                case LButtonUp:
                case LButtonDoubleClick:
                    return true;
            }
            return false;
        }
    }

    public static class MouseHook
    {
        public static event EventHandler MouseAction = delegate { };

        public static void Start()
        {
            _hookID = SetHook(_proc);
        }
        public static void stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                  GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            /*if (nCode >= 0 && MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
               
               // MouseAction(null, new EventArgs(),  );
            }*/
           
            MSLLHOOKSTRUCT w = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

            if (w.mouseData == 4287102976)
            {
                string wParamTmp = "WM_MOUSEWHEEL.down";
                MouseAction(wParamTmp, new EventArgs());
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            else if (w.mouseData == 7864320)
            {
                string wParamTmp = "WM_MOUSEWHEEL.up";
                MouseAction(wParamTmp, new EventArgs());
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            MouseAction((MouseMessages)wParam, new EventArgs());
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private const int WH_MOUSE_LL = 14;

        public enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


    }

    class GlobalKeyboardHookEventArgs : HandledEventArgs
    {
        public GlobalKeyboardHook.KeyboardState KeyboardState { get; private set; }
        public GlobalKeyboardHook.LowLevelKeyboardInputEvent KeyboardData { get; private set; }

        public GlobalKeyboardHookEventArgs(
            GlobalKeyboardHook.LowLevelKeyboardInputEvent keyboardData,
            GlobalKeyboardHook.KeyboardState keyboardState)
        {
            KeyboardData = keyboardData;
            KeyboardState = keyboardState;
        }
    }

    //Based on https://gist.github.com/Stasonix
    class GlobalKeyboardHook : IDisposable
    {
        public event EventHandler<GlobalKeyboardHookEventArgs> KeyboardPressed;

        public GlobalKeyboardHook()
        {
            _windowsHookHandle = IntPtr.Zero;
            _user32LibraryHandle = IntPtr.Zero;
            _hookProc = LowLevelKeyboardProc; // we must keep alive _hookProc, because GC is not aware about SetWindowsHookEx behaviour.

            _user32LibraryHandle = LoadLibrary("User32");
            if (_user32LibraryHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to load library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }



            _windowsHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, _user32LibraryHandle, 0);
            if (_windowsHookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to adjust keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // because we can unhook only in the same thread, not in garbage collector thread
                if (_windowsHookHandle != IntPtr.Zero)
                {
                    if (!UnhookWindowsHookEx(_windowsHookHandle))
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Win32Exception(errorCode, $"Failed to remove keyboard hooks for '{Process.GetCurrentProcess().ProcessName}'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                    }
                    _windowsHookHandle = IntPtr.Zero;

                    // ReSharper disable once DelegateSubtraction
                    _hookProc -= LowLevelKeyboardProc;
                }
            }

            if (_user32LibraryHandle != IntPtr.Zero)
            {
                if (!FreeLibrary(_user32LibraryHandle)) // reduces reference to library by 1.
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode, $"Failed to unload library 'User32.dll'. Error {errorCode}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}.");
                }
                _user32LibraryHandle = IntPtr.Zero;
            }
        }

        ~GlobalKeyboardHook()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private IntPtr _windowsHookHandle;
        private IntPtr _user32LibraryHandle;
        private HookProc _hookProc;

        delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// The SetWindowsHookEx function installs an application-defined hook procedure into a hook chain.
        /// You would install a hook procedure to monitor the system for certain types of events. These events are
        /// associated either with a specific thread or with all threads in the same desktop as the calling thread.
        /// </summary>
        /// <param name="idHook">hook type</param>
        /// <param name="lpfn">hook procedure</param>
        /// <param name="hMod">handle to application instance</param>
        /// <param name="dwThreadId">thread identifier</param>
        /// <returns>If the function succeeds, the return value is the handle to the hook procedure.</returns>
        [DllImport("USER32", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

        /// <summary>
        /// The UnhookWindowsHookEx function removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
        /// </summary>
        /// <param name="hhk">handle to hook procedure</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("USER32", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hHook);

        /// <summary>
        /// The CallNextHookEx function passes the hook information to the next hook procedure in the current hook chain.
        /// A hook procedure can call this function either before or after processing the hook information.
        /// </summary>
        /// <param name="hHook">handle to current hook</param>
        /// <param name="code">hook code passed to hook procedure</param>
        /// <param name="wParam">value passed to hook procedure</param>
        /// <param name="lParam">value passed to hook procedure</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [DllImport("USER32", SetLastError = true)]
        static extern IntPtr CallNextHookEx(IntPtr hHook, int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct LowLevelKeyboardInputEvent
        {
            /// <summary>
            /// A virtual-key code. The code must be a value in the range 1 to 254.
            /// </summary>
            public int VirtualCode;

            /// <summary>
            /// A hardware scan code for the key. 
            /// </summary>
            public int HardwareScanCode;

            /// <summary>
            /// The extended-key flag, event-injected Flags, context code, and transition-state flag. This member is specified as follows. An application can use the following values to test the keystroke Flags. Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected. If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event was injected from a process running at lower integrity level.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The time stamp stamp for this message, equivalent to what GetMessageTime would return for this message.
            /// </summary>
            public int TimeStamp;

            /// <summary>
            /// Additional information associated with the message. 
            /// </summary>
            public IntPtr AdditionalInformation;
        }

        public const int WH_KEYBOARD_LL = 13;
        //const int HC_ACTION = 0;

        public enum KeyboardState
        {
            KeyDown = 0x0100,
            KeyUp = 0x0101,
            SysKeyDown = 0x0104,
            SysKeyUp = 0x0105
        }

        public const int VkSnapshot = 0x2c;
        //const int VkLwin = 0x5b;
        //const int VkRwin = 0x5c;
        //const int VkTab = 0x09;
        //const int VkEscape = 0x18;
        //const int VkControl = 0x11;
        const int KfAltdown = 0x2000;
        public const int LlkhfAltdown = (KfAltdown >> 8);

        public IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool fEatKeyStroke = false;

            var wparamTyped = wParam.ToInt32();
            if (Enum.IsDefined(typeof(KeyboardState), wparamTyped))
            {
                object o = Marshal.PtrToStructure(lParam, typeof(LowLevelKeyboardInputEvent));
                LowLevelKeyboardInputEvent p = (LowLevelKeyboardInputEvent)o;

                var eventArguments = new GlobalKeyboardHookEventArgs(p, (KeyboardState)wparamTyped);

                EventHandler<GlobalKeyboardHookEventArgs> handler = KeyboardPressed;
                handler?.Invoke(this, eventArguments);

                fEatKeyStroke = eventArguments.Handled;
            }

            return fEatKeyStroke ? (IntPtr)1 : CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }
    }
}
