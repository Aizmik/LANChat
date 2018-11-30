using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace Chat
{
    public partial class ChatClient : Form
    {
        private string name;
        private NetworkStream clientStream;
        public delegate void SetTextCallback(string s);
        private Server owner;

        public TcpClient СonnectedClient { get; set; }

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
            //Берем поток
            СonnectedClient = tcpClient;
            clientStream = tcpClient.GetStream();

            //Создаем экземпляр класса, чтобы считывать данные по ТСП
            StateObject state = new StateObject
            {   workSocket = СonnectedClient.Client     };

            //Вызов ассинхронной функции чтобы получать данные
            СonnectedClient.Client.BeginReceive(state.buffer, 0,
                StateObject.BufferSize, 0, new AsyncCallback(OnRecieve), state);

            richTextBox1.AppendText("Chat Log Here-------->\n");
        }
        #endregion

        private void SetText(string text)
        {
            // InvokeRequired сравнивает ID потоков, если они 
            // одинаковы - возвращает true, иначе - false
            if (this.richTextBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                Invoke(d, new object[] { text });
            }
            else
            {
                richTextBox1.SelectionColor = Color.Blue;
                richTextBox1.SelectedText = "\n" + text;
            }
        }

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
            СonnectedClient.Client.Send(bt);

            richTextBox1.SelectionColor = Color.IndianRed;
            richTextBox1.SelectedText = "\n" + name + ": " + message.Text;
            message.Text = "";

            richTextBox1.ScrollToCaret();
        }
        
        /// <summary>
        /// Асинхронныая Отвечающая функция которая получает данные из Сервера
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
                //Чтение данных
                try
                {
                    bytesRead = handler.EndReceive(ar);
                    if(bytesRead > 0)
                    {
                        state.sb.Remove(0, state.sb.Length);

                        state.sb.Append(Encoding.Default.GetString(
                            state.buffer, 0, bytesRead));

                        content = state.sb.ToString();
                        SetText(content);

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

        private void EnterPressed(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
                button1.PerformClick();
        }


        private void Bright(object sender, EventArgs e)
        {
            ChangeColor(Color.PeachPuff);
        }

        private void Dark(object sender, EventArgs e)
        {
            ChangeColor(Color.DarkSlateGray);
        }

        private void ChangeColor(Color BGColor)
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

            System.IO.StreamWriter file = new System.IO.StreamWriter("Color.txt");
            file.Write(BGColor.ToString());
            file.Close();
        }
    }
}
