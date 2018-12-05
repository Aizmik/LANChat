using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Drawing;

namespace Chat
{
    public partial class Server : Form
    {
        private TcpListener tcpServer;
        private TcpClient tcpClient;
        private Thread th;
        private ChatClient dialog;
        private ArrayList formArray = new ArrayList();
        private ArrayList threadArray = new ArrayList();
        public delegate void ChangedEventHandler(object sender, EventArgs e);

        public Server()
        {
            InitializeComponent();

            try
            {
                System.IO.StreamReader file = new System.IO.StreamReader("Color.txt");

                if (file.ReadToEnd() == "Color [DarkSlateGray]")
                {
                    BackColor = Color.DarkSlateGray;
                }
                file.Close();
            }
            catch {}
        }


        #region Start/Stop server

        /// <summary>
        /// Create a new thread to connect
        /// </summary>
        public void StartServer()
        {
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            checkBox2.Enabled = false;
            th = new Thread(new ThreadStart(StartListen));
            th.Start();
        }

        /// <summary>
        /// Server begins to "listen" on given ip & port number
        /// </summary>
        public void StartListen()
        {
            string host = Dns.GetHostName();
            IPAddress ip = Dns.GetHostEntry(host).AddressList[1];

            switch (ip.AddressFamily)
            {
                case AddressFamily.InterNetworkV6:
                    ip = Dns.GetHostEntry(host).AddressList[2];
                    break;
                default:
                    break;
            }
                        
            var settextAction = new Action(() => { label3.Text = "Ваш ip: " + ip; });
            if (label3.InvokeRequired)
                label3.Invoke(settextAction);
            else
                settextAction();

            tcpServer = new TcpListener(ip,int.Parse(textBox1.Text));
            tcpServer.Start();

            // Keep accepting clients
            while (true)
            {
                Thread t = new Thread(new ParameterizedThreadStart(NewClient));
                tcpClient = tcpServer.AcceptTcpClient();
                t.SetApartmentState(ApartmentState.STA);
                t.Start(tcpClient);
            }
        }

        /// <summary>
        /// Stops all the sockets and abort connections
        /// </summary>
        public void StopServer()
        {
            if (tcpServer != null)
            {

                // Close all Socket connection
                foreach (ChatClient c in formArray)
                {
                    foreach (TcpClient client in c.tcpClients)
                    {
                        client.Client.Close();
                    }
                }

                // Abort All Running Threads
                foreach (Thread t in threadArray)
                {
                    t.Abort();
                }

                // Clear all ArrayList
                threadArray.Clear();
                formArray.Clear();

                // Abort Listening Thread and Stop listening
                tcpServer.Stop();
                th.Abort();
            }
            textBox1.Enabled = true;
            textBox2.Enabled = true;
            checkBox2.Enabled = true;
        }

        /// <summary>
        /// Turn ON/OFF the server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerOnOff(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                if (textBox2.TextLength == 0)
                    textBox2.Text = "Server";

                try
                {
                    if (textBox1.TextLength == 0)
                    {
                        MessageBox.Show("Введите номер порта!");
                        checkBox1.Checked = false;
                        return;
                    }

                    int port = int.Parse(textBox1.Text);

                    StartServer();
                }
                catch (Exception ex)
                {

                    MessageBox.Show("Не тот номер порта" + ex);
                    checkBox1.Checked = false;
                }
            }
            else
                StopServer();
        }
        #endregion

        #region Clients


        public void NewClient(object obj)
        {
            ClientAdded(this, new tcpEventArgs((TcpClient)obj));
        }


        /// <summary>
        /// When client is connected,
        /// Opens a dialog window to chat
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClientAdded(object sender, EventArgs e)
        {
            tcpClient = ((tcpEventArgs)e).clientSock;
            string clRemoteIP = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
            string clRemotePort = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port.ToString();

            if (!checkBox2.Checked || formArray.Count == 0)
            {
                dialog = new ChatClient(this, tcpClient, textBox2.Text, BackColor);
                formArray.Add(dialog);
                threadArray.Add(Thread.CurrentThread);
                dialog.ShowDialog();
            }
            else
            {
                (formArray[0] as ChatClient).AddClients(tcpClient);
            }

        }

        public void DisconnectClient(string remoteIP, string remotePort)
        {
            int count = 0;
            foreach (ChatClient ch in formArray)
            {
                foreach (TcpClient client in ch.tcpClients)
                {
                    string remoteIP1 = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    string remotePort1 = ((IPEndPoint)client.Client.RemoteEndPoint).Port.ToString();

                    if (remoteIP1.Equals(remoteIP) && remotePort1.Equals(remotePort))
                    {
                        (formArray[count] as ChatClient).RemoveClients(count);
                        break;
                    }
                    count++;
                }
            }
            //Closes dialogue
            ChatClient cd = (ChatClient)formArray[count];
            formArray.RemoveAt(count);
                       
            ((Thread)(threadArray[count])).Abort();
            threadArray.RemoveAt(count);
        }
        #endregion

        private void Server_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopServer();
        }
    }
}