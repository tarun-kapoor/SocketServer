using System;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net;

namespace SocketTcpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            //LNGDAYL-7008290
            //IPEndPoint ipe = new IPEndPoint(Dns.GetHostAddresses(Dns.GetHostName())[1], 2055);
            //TcpClient client = new TcpClient(ipe);
            TcpClient client = null;
            
            try
            {
                client = new TcpClient("<ServerHostName>", 2055);
                Stream s = client.GetStream();
                StreamReader sr = new StreamReader(s);
                StreamWriter sw = new StreamWriter(s);
                sw.AutoFlush = true;
                Console.WriteLine("To quit - enter quit and hit enter. Say hi to begin chat :)");
                //Console.WriteLine(sr.ReadLine());

                //thread to read from server
                ParameterizedThreadStart ts = ReadServer;

                Thread t = new Thread(ts);
                t.IsBackground = true;
                t.Start(sr);

                string sTextToSend = string.Empty;

                while (true)
                {
                    Console.Write($"{Environment.UserName}: ");
                    sTextToSend = Console.ReadLine();

                    if (sTextToSend == "quit")
                    {
                        sw.WriteLine($"{sTextToSend}");
                        break;
                    }
                    else if (sTextToSend == "")
                    {
                        //wait 1 second
                        Thread.Sleep(1000);
                        if (t.ThreadState == ThreadState.Stopped)
                            break;
                    }

                    if (sTextToSend == "") continue;
                    sw.WriteLine($"{Environment.UserName} says: {sTextToSend}");
                    

                    //Console.Write("LNGDAYL - 7008290 says: ");
                    //Console.WriteLine(sr.ReadLine());
                }
                s.Close();
            }
            catch(SocketException ex) when (ex.ErrorCode == 10061)
            {
                Console.WriteLine("Check server is running and hostname specified is correct");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // code in finally block is guranteed 
                // to execute irrespective of 
                // whether any exception occurs or does 
                // not occur in the try block
                client?.Close();
            }
        }

        private static void ReadServer(object obj)
        {
            while (true)
            {
                try
                {
                    var s = ((StreamReader)obj).ReadLine();
                    if (s != null || !string.IsNullOrEmpty(s))
                    {
                        if (s.ToUpper() == "KILLCLIENT")
                        {
                            throw new Exception("KILLCLIENT");
                        }
                        Console.WriteLine($"{Environment.NewLine}{s}");
                        Console.Write($"{Environment.UserName}: ");
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == "KILLCLIENT")
                    {
                        Console.WriteLine($"Server terminated your session");
                        var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                        PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
                        break;
                    }
                }
            }
        }

        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        const int VK_RETURN = 0x0D;
        const int WM_KEYDOWN = 0x100;
    }
}
