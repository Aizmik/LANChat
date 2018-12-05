using System;
using System.Drawing;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace Chat
{
    public partial class ChatClient : Form
    {
        private string name;
        private NetworkStream clientStream;
        public delegate void SetTextCallback(string s);
        private Server owner;
        //public TcpClient СonnectedClient { get; set; }
        public ArrayList tcpClients = new ArrayList();

        #region Constructors
        public ChatClient()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор принимающий клиента
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        public ChatClient(Server parent, TcpClient tcpClient, string name_server, Color color)
        {
            InitializeComponent();

            BackColor = color;
            name = name_server;
            owner = parent;

            AddClients(tcpClient);

            richTextBox1.AppendText("Chat Log Here-------->\n");
        }
        #endregion

        #region Send and Recive Data from Scokets
        /// <summary>
        /// Отправляем данные
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendMessage(object sender, EventArgs e)
        {
            message.Text = message.Text.Replace("\r\n", string.Empty);

            if (message.TextLength == 0)
                return;

            byte[] bt;

            bt = Encoding.Default.GetBytes(name + ": " + message.Text);
            try
            { foreach (TcpClient client in tcpClients) client.Client.Send(bt); }
            catch
            {  }

            richTextBox1.SelectionColor = Color.IndianRed;
            richTextBox1.SelectedText = "\n" + name + ": " + message.Text;
            message.Text = "";

            richTextBox1.ScrollToCaret();
        }

        /// <summary>
        /// This method set text to richtextbox and paint it
        /// </summary>
        /// <param name="text"></param>
        private void SetText(string text)
        {
            // InvokeRequired сравнивает ID потоков, если они 
            // одинаковы - возвращает true, иначе - false
            if (richTextBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                Invoke(d, new object[] { text });
            }
            else
            {
                richTextBox1.SelectionColor = Color.Blue;
                richTextBox1.SelectedText = "\n" + text;
                richTextBox1.ScrollToCaret();
            }
        }

        public int fileSize;
        public string fileName;
        public string fileSender;
        bool acceptingFile = false;
        /// <summary>
        /// Асинхронныая Отвечающая функция которая получает данные
        /// </summary>
        /// <param name="ar"></param>
        public void OnRecieve(IAsyncResult ar)
        {
            string content = string.Empty;

            //Получить наш класс и сокет
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;
            int bytesRead;

            if(handler.Connected)
            {
                //Reading data
                try
                {
                    bytesRead = handler.EndReceive(ar);
                    if(bytesRead > 0)
                    {
                        if (acceptingFile)
                        {
                            MemoryStream file = new MemoryStream();
                            file.Write(state.buffer, 0, bytesRead);
                            fileSize -= bytesRead;
                            do
                            {
                                int received = handler.Receive(state.buffer);
                                file.Write(state.buffer, 0, received);
                                fileSize -= received;
                            } while (fileSize > 0);

                            SendFile(file.ToArray(), fileName, fileSender, handler);

                            DialogResult res = MessageBox.Show(fileSender + " хочет переслать вам файл " + fileName, "Файл", MessageBoxButtons.OKCancel);
                            if (res == DialogResult.OK)
                            {
                                File.WriteAllBytes(fileName, file.ToArray());
                            }

                            file.Close();
                            acceptingFile = false;
                        }
                        else
                        {
                            state.sb.Remove(0, state.sb.Length);
                            foreach (TcpClient client in tcpClients)
                            {
                                if (handler != client.Client)
                                    client.Client.Send(state.buffer);
                            }
                            state.sb.Append(Encoding.Default.GetString(
                                state.buffer, 0, bytesRead));
                            content = state.sb.ToString();
                        }


                        if(!content.StartsWith("Sf5}"))
                            SetText(content);
                        else
                        {
                            SetText("Лови файл!");
                            string[] names = content.Split(' ');
                            fileName = names[1];
                            fileSize = int.Parse(names[2]);
                            fileSender = names[3];
                            acceptingFile = true;
                        }
                        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize,
                            0, new AsyncCallback(OnRecieve), state);
                    }
                }
                catch (SocketException sE)
                {
                    if (sE.ErrorCode == 10054 || ((sE.ErrorCode != 10004) && (sE.ErrorCode != 10053)))
                    {
                        // Отключаемся от сервера
                        string remoteIP = ((IPEndPoint)handler.RemoteEndPoint).Address.ToString();
                        string remotePort = ((IPEndPoint)handler.RemoteEndPoint).Port.ToString();
                        owner.DisconnectClient(remoteIP, remotePort);

                        handler.Close();
                        handler = null;

                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }
        #endregion

        public void AddClients(TcpClient client)
        {
            tcpClients.Add(client);

            clientStream = client.GetStream();

            //Создаем экземпляр класса, чтобы считывать данные по ТСП
            StateObject state = new StateObject
            { workSocket = client.Client };

            //Вызов ассинхронной функции чтобы получать данные
            client.Client.BeginReceive(state.buffer, 0,
                StateObject.BufferSize, 0, new AsyncCallback(OnRecieve), state);
        }

        public void RemoveClients(int client)
        {
            tcpClients.Remove(client);
        }


        private void EnterPressed(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
                button1.PerformClick();
        }

        #region Color
        private void Bright(object sender, EventArgs e)
        {
            ChangeColor(Color.PeachPuff);
        }

        private void Dark(object sender, EventArgs e)
        {
            ChangeColor(Color.DarkSlateGray);
        }

        private static void ChangeColor(Color BGColor)
        {
            FormCollection F = Application.OpenForms;
           
            foreach(Form form in F)
            {
                var settextAction = new Action(() => { form.BackColor = BGColor; });
                if (form.InvokeRequired)
                    form.Invoke(settextAction);
                else
                    settextAction();
            }

            try
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter("Color.txt");
                file.Write(BGColor.ToString());
                file.Close();
            }
            catch {}
        }
        #endregion

        private void SendFile(byte[] bt, string fileName, string fileSender, Socket sender)
        {
            try
            {
                if (bt.Length == 0)
                {
                    openFileDialog1.ShowDialog();
                    bt = File.ReadAllBytes(openFileDialog1.FileName);
                }

                foreach (TcpClient client in tcpClients)
                {
                    if(client.Client != sender)
                    {
                        client.Client.Send(Encoding.Default.GetBytes("Sf5} " +
                            fileName.Replace(" ", "_") + " " + bt.Length + " " + fileSender));
                        client.Client.Send(bt);
                    }

                }
            }
            catch
            { MessageBox.Show("Ошибка"); }
        }
        private void SendFile()
        {
            try
            {

                openFileDialog1.ShowDialog();
                byte[] bt = File.ReadAllBytes(openFileDialog1.FileName);


                foreach (TcpClient client in tcpClients)
                {
                    client.Client.Send(Encoding.Default.GetBytes("Sf5} " +
                        openFileDialog1.SafeFileName.Replace(" ", "_") + " " + 
                            bt.Length + " " + name));

                    client.Client.Send(bt);
                }
                    
            }
            catch(Exception ex)
            { MessageBox.Show("Ошибка " + ex.ToString()); }
        }


        private void SendindFile_Click(object sender, EventArgs e)
        {
            SendFile();
        }
    }
}
