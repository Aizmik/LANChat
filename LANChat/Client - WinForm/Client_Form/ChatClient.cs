using System;
using System.Drawing;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.IO;

namespace Client_Form
{
    public partial class ChatClient : Form
    {
        private TcpClient client;
        private string name;
        public bool acceptingFile = false;
        public delegate void SetTextCallback(string s);

        #region Debug

        #region Constructors
        public ChatClient()
        {
            InitializeComponent();
        }

        public ChatClient(TcpClient client, string name, Color color)
        {
            InitializeComponent();
            this.name = name;
            this.client = client;
            BackColor = color;
            

            NetworkStream ns = client.GetStream();

            StateObject state = new StateObject
            {
                workSocket = client.Client
            };

            //Вызов ассинхронной функции чтобы получать данные
            client.Client.BeginReceive(state.buffer, 0,
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
            client.Client.Send(bt);
                       
            richTextBox1.SelectionColor = Color.IndianRed;
            richTextBox1.SelectedText = "\n" + name + ": " + message.Text;
            message.Text = "";
            richTextBox1.ScrollToCaret();
        }
        #endregion
        #endregion
        private void SendFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();

            byte[] bt = File.ReadAllBytes(openFileDialog1.FileName);
            
            try
            {
                client.Client.Send(Encoding.Default.GetBytes("Sf5} " +
                    openFileDialog1.SafeFileName.Replace(" ", "_") + " " + bt.Length + " " + name));
                client.Client.Send(bt);
            }
            catch
            { }
        }


        public int fileSize;
        public string fileName;
        public string fileSender;
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

            if (handler.Connected)
            {
                //Чтение данных
                try
                {
                    bytesRead = handler.EndReceive(ar);

                    if (bytesRead > 0)
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
                            state.sb.Append(Encoding.Default.GetString(
                                state.buffer, 0, bytesRead));
                            content = state.sb.ToString();
                        }

                        if (!content.StartsWith("Sf5}"))
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

                        handler.Close();
                        handler = null;

                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                }
            }
        }
        

        #region Debugg
        private void EnterPressed(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == Convert.ToChar(Keys.Enter))
                button1.PerformClick();
        }
        
        private void ChatClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                client.Client.Disconnect(false);
                client.Client.Close();
            }
            catch {}
        }
                     
        #region Drawing
        private void Bright(object sender, EventArgs e)
        {
            ChangeColor(Color.PeachPuff);
        }

        private void Dark(object sender, EventArgs e)
        {
            ChangeColor(Color.DarkSlateGray);
        }

        static private void ChangeColor(Color BGColor)
        {
            FormCollection F = Application.OpenForms;

            foreach (Form form in F)
            {
                var settextAction = new Action(() => { form.BackColor = BGColor; });
                if (form.InvokeRequired)
                    form.Invoke(settextAction);
                else
                    settextAction();
            }

            try
            {
                StreamWriter file = new StreamWriter("Color.txt");
                file.Write(BGColor.ToString());
                file.Close();
            }
            catch {}
        }
        #endregion
        #endregion
    }
}