﻿using CorelLASERBot.Properties;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CorelLASERBot
{
    public partial class FormMain : Form
    {
        // Create CancellationTokenSource.
        CancellationTokenSource source;
        // ... Get Token from source.
        CancellationToken token;
        bool TEMPLATE_FOUND = false;
        bool AUTO_CLOSE = false;
        public FormMain()
        {
            InitializeComponent();
        }

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private async void buttonStart_Click(object sender, EventArgs e)
        {
            if (buttonStart.Tag.ToString() == "active")
            {
                buttonStart.Text = "Start!";
                buttonStart.Tag = "";
                source.Cancel();
                BackColor = Color.White;
                buttonCapture.Enabled = true;
                // timerMain.Enabled = false;
            }
            else
            {
                if (!File.Exists("template.bmp"))
                {
                    TEMPLATE_FOUND = false;
                }
                else
                {
                    TEMPLATE_FOUND = true;
                }

                if (MessageBox.Show("Jalankan Program dengan Lokasi {X: " + Settings.Default.CURSOR_X + "; Y:" + Settings.Default.CURSOR_Y + ";}? \n\nDefault {X: " + Settings.Default.Properties["CURSOR_X"].DefaultValue + "; Y:" + Settings.Default.Properties["CURSOR_Y"].DefaultValue + ";}", Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    TopMost = true;
                    BringToFront();
                    buttonStart.Text = "Stop";
                    buttonStart.Tag = "active";
                    buttonCapture.Enabled = false;
                    buttonStart.Enabled = false;
                    source = new CancellationTokenSource();
                    token = source.Token;
                    var task = Task.Run(() => DoSomething(token), token);
                    BackColor = Color.LawnGreen;
                    await Task.Run(() => { Thread.Sleep(1000); });

                    buttonStart.Enabled = true;
                    // timerMain.Enabled = true;
                }

            }
            labelStatus.Focus();
        }

        //This is a replacement for Cursor.Position in WinForms
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            var x = MousePosition.X;
            var y = MousePosition.Y;
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
            Thread.Sleep(100);
            SetCursorPos(x, y);

        }


        private void DoSomething(CancellationToken token)
        {
            // Do something important.
            while (true)
            {
                Invoke((MethodInvoker)delegate ()
                {
                    labelStatus.Text = "Running...";
                });
                // Wait a few moments.
                Thread.Sleep(1000);


                Bitmap screenCapture = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

                Graphics g = Graphics.FromImage(screenCapture);

                g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                 Screen.PrimaryScreen.Bounds.Y,
                                 0, 0,
                                 screenCapture.Size,
                                 CopyPixelOperation.SourceCopy);
                var bmpSmall = (Bitmap)Resources.template;
                if (TEMPLATE_FOUND)
                {
                    bmpSmall = (Bitmap)Image.FromFile("template.bmp");
                }

                Invoke((MethodInvoker)delegate ()
                {
                    labelStatus.Text = "Process IMG!";
                });

                //var test = IsInCapture(bmpSmall, screenCapture);

                Rectangle location = searchBitmap(bmpSmall, screenCapture, 0.1);
                var test = location.Width;
                if (test > 0)
                {
                    Invoke((MethodInvoker)delegate ()
                    {
                        labelStatus.Text = "Found!";
                    });
                    string app = "CorelDRW";
                    Process[] p = Process.GetProcessesByName(app);
                    //Process[] p = Process.GetProcessesByName("Notepad");

                    // Activate the first application we find with this name
                    if (p.Count() > 0)
                    {
                        //SetForegroundWindow(p[0].MainWindowHandle);
                        Thread.Sleep(1000);
                        LeftMouseClick(Settings.Default.CURSOR_X, Settings.Default.CURSOR_Y);
                        //SendKeys.SendWait("{ENTER}");
                        //var task = Task.Run(() => DoSomething(token), token);
                        //return;
                    }
                    else
                    {
                        Invoke((MethodInvoker)delegate ()
                        {
                            labelStatus.Text = "No " + app + "!";
                        });
                        Thread.Sleep(1000);
                    }
                }
                else
                {

                    Invoke((MethodInvoker)delegate ()
                    {
                        labelStatus.Text = "Continue...";
                    });
                    Thread.Sleep(1000);

                }

                // See if we are canceled from our CancellationTokenSource.
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Method1 canceled");
                    break;
                }
            }

            Invoke((MethodInvoker)delegate ()
            {
                labelStatus.Text = "Idle...";
            });
        }

        private Rectangle searchBitmap(Bitmap smallBmp, Bitmap bigBmp, double tolerance)
        {
            BitmapData smallData =
              smallBmp.LockBits(new Rectangle(0, 0, smallBmp.Width, smallBmp.Height),
                       System.Drawing.Imaging.ImageLockMode.ReadOnly,
                       System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            BitmapData bigData =
              bigBmp.LockBits(new Rectangle(0, 0, bigBmp.Width, bigBmp.Height),
                       System.Drawing.Imaging.ImageLockMode.ReadOnly,
                       System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            int smallStride = smallData.Stride;
            int bigStride = bigData.Stride;

            int bigWidth = bigBmp.Width;
            int bigHeight = bigBmp.Height - smallBmp.Height + 1;
            int smallWidth = smallBmp.Width * 3;
            int smallHeight = smallBmp.Height;

            Rectangle location = Rectangle.Empty;
            int margin = Convert.ToInt32(255.0 * tolerance);

            unsafe
            {
                byte* pSmall = (byte*)(void*)smallData.Scan0;
                byte* pBig = (byte*)(void*)bigData.Scan0;

                int smallOffset = smallStride - smallBmp.Width * 3;
                int bigOffset = bigStride - bigBmp.Width * 3;

                bool matchFound = true;

                for (int y = 0; y < bigHeight; y++)
                {
                    for (int x = 0; x < bigWidth; x++)
                    {
                        byte* pBigBackup = pBig;
                        byte* pSmallBackup = pSmall;

                        //Look for the small picture.
                        for (int i = 0; i < smallHeight; i++)
                        {
                            int j = 0;
                            matchFound = true;
                            for (j = 0; j < smallWidth; j++)
                            {
                                //With tolerance: pSmall value should be between margins.
                                int inf = pBig[0] - margin;
                                int sup = pBig[0] + margin;
                                if (sup < pSmall[0] || inf > pSmall[0])
                                {
                                    matchFound = false;
                                    break;
                                }

                                pBig++;
                                pSmall++;
                            }

                            if (!matchFound) break;

                            //We restore the pointers.
                            pSmall = pSmallBackup;
                            pBig = pBigBackup;

                            //Next rows of the small and big pictures.
                            pSmall += smallStride * (1 + i);
                            pBig += bigStride * (1 + i);
                        }

                        //If match found, we return.
                        if (matchFound)
                        {
                            location.X = x;
                            location.Y = y;
                            location.Width = smallBmp.Width;
                            location.Height = smallBmp.Height;
                            break;
                        }
                        //If no match found, we restore the pointers and continue.
                        else
                        {
                            pBig = pBigBackup;
                            pSmall = pSmallBackup;
                            pBig += 3;
                        }
                    }

                    if (matchFound) break;

                    pBig += bigOffset;
                }
            }

            bigBmp.UnlockBits(bigData);
            smallBmp.UnlockBits(smallData);

            return location;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            //MessageBox.Show("", "", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            //var task = Task.Run(() => tracker());
            buttonStart.Tag = "inactive";
            Location = new Point(Convert.ToInt32(Screen.PrimaryScreen.Bounds.Width * 0.6), 0);
            Size = new Size(215, 30);
            labelStatus.Focus();
            Run_AutoClose();

            //Settings.Default.Reset();
        }

        private void tracker()
        {
            while (true)
            {
                Invoke((MethodInvoker)delegate ()
                {
                    //labelMousePosition.Text = "{X: " + MousePosition.X + "; Y:" + MousePosition.Y + ";}";
                });
                Thread.Sleep(500);
            }
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void Form1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void labelMousePosition_Click(object sender, EventArgs e)
        {

        }

        private void labelStatus_Click(object sender, EventArgs e)
        {

        }

        private async void buttonCapture_Click(object sender, EventArgs e)
        {
            labelStatus.Focus();
            if(MessageBox.Show("Posisikan cursor pada lokasi yang akan di click. Software akan menunggu selama 5 detik kemudian menyimpan posisinya.\n\nTekan \"OK\" untuk mengubah posisi\nTekan \"Cancel\" untuk kembali", "Cursor Location Capture", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
            {
                await Task.Run(() => { Thread.Sleep(5000); });
                Settings.Default.CURSOR_X = MousePosition.X;
                Settings.Default.CURSOR_Y = MousePosition.Y;
                Settings.Default.Save();
                MessageBox.Show("Lokasi {X: " + Settings.Default.CURSOR_X + "; Y:" + Settings.Default.CURSOR_Y + ";} berhasil disimpan!", "Cursor Location Capture", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                if (MessageBox.Show("Reset ke nilai default?", "Cursor Location Capture", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {

                    Settings.Default.CURSOR_X = Convert.ToInt32(Settings.Default.Properties["CURSOR_X"].DefaultValue);
                    Settings.Default.CURSOR_Y = Convert.ToInt32(Settings.Default.Properties["CURSOR_Y"].DefaultValue);
                    Settings.Default.Save();
                    MessageBox.Show("Lokasi {X: " + Settings.Default.CURSOR_X + "; Y:" + Settings.Default.CURSOR_Y + ";} berhasil direset ke default!", "Cursor Location Capture", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void Run_AutoClose()
        {

            //Process[] p = Process.GetProcessesByName("CorelDRW");
            Process[] p = Process.GetProcessesByName("notepad");

            // Activate the first application we find with this name
            if (p.Count() > 0)
            {
                AUTO_CLOSE = true;
            }
            else
            {
                if (AUTO_CLOSE)
                    Application.Exit();
            }
        }
        private void timerMain_Tick(object sender, EventArgs e)
        {
            Run_AutoClose();
        }

    }

}

