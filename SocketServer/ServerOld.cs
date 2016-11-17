using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer
{
    class ServerOld
    {
        static TcpListener listener;
        const int LIMIT = 5;

        static void MainOld(string[] args)
        {

            listener = new TcpListener(Dns.GetHostAddresses(Dns.GetHostName())[1], 2055);
            listener.Start();

            Console.WriteLine("Server mounted, listening to port 2055");

            for (int i = 0; i < LIMIT; i++)
            {
                Thread t = new Thread(new ThreadStart(Service));
                t.Start();
            }


        }

        public static void Service()
        {
            while (true)
            {
                Console.WriteLine("Waiting for connections...");
                Socket soc = listener.AcceptSocket();
                //soc.SetSocketOption(SocketOptionLevel.Socket,
                //        SocketOptionName.ReceiveTimeout,10000);

                Stream s = new NetworkStream(soc);
                StreamReader sr = new StreamReader(s);
                StreamWriter sw = new StreamWriter(s);

                Console.WriteLine("Connected: {0}",
                                         soc.RemoteEndPoint);

                try
                {
                    sw.AutoFlush = true; // enable automatic flushing


                    //thread to read from server
                    ThreadObj obj = new ThreadObj();
                    obj.RemoteName = soc.RemoteEndPoint.ToString();
                    obj.SR = sr;
                    ParameterizedThreadStart ts = ReadClient;
                    Thread t = new Thread(ts);
                    t.Start(obj);

                    string sTextToSend = string.Empty;

                    while (true)
                    {
                        Console.Write($"{Environment.UserName}: ");
                        sTextToSend =
                            Console.ReadLine();
                        if (sTextToSend == "killclient")
                        {
                            obj.KillSwitch = true;
                            Console.WriteLine($"KillClient issued");
                            sw.WriteLine($"{sTextToSend.ToUpper()}");
                            break;
                        }
                        else if (sTextToSend == "")
                        {
                            //wait 1 second
                            Thread.Sleep(1000);
                            if (t.ThreadState == ThreadState.Stopped)
                                break;
                        }
                        sw.WriteLine($"{Environment.UserName} says: {sTextToSend}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    sw.Close();
                    sr.Close();
                    s.Close();
                }

                Console.WriteLine("Disconnected: {0}",
                                        soc.RemoteEndPoint);
                soc.Close();

            }
        }

        private static void ReadClient(object obj)
        {
            string sRemoteEndPoint = ((ThreadObj)obj).RemoteName;

            while (true)
            {
                try
                {
                    if (((ThreadObj)obj).SR == null) break;
                    string sRecText = ((ThreadObj)obj).SR.ReadLine();
                    if (sRecText == "quit")
                    {
                        throw new Exception("quit");
                    }

                    if (!string.IsNullOrEmpty(sRecText))
                    {
                        Console.WriteLine($"{Environment.NewLine}{sRecText}");
                        Console.Write($"{Environment.UserName}: ");
                    }
                }
                catch (Exception ex)
                {
                    var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                    if (ex.Message == "quit")
                    {
                        Console.WriteLine($"Client left session");
                        PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
                    }
                    else
                    {
                        if (((ThreadObj)obj).KillSwitch) break;
                        Console.WriteLine(ex.Message);
                        PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
                    }
                    break;
                }
            }

        }

        private class ThreadObj
        {
            public StreamReader SR { get; set; }
            public string RemoteName { get; set; }
            public bool KillSwitch { get; set; }
            public ThreadObj()
            {
                KillSwitch = false;
            }
        }

        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        const int VK_RETURN = 0x0D;
        const int WM_KEYDOWN = 0x100;
    }
}
