using System;
using System.Drawing;
using System.Net.Sockets;
using System.Windows.Forms;

namespace Client_Form
{
    public partial class Login : Form
    {
        TcpClient client;

        public Login()
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
            catch { }
        }

        private void ConnectToServer(object sender, EventArgs e)
        {
            if (ip.TextLength == 0 || port.TextLength == 0)
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }

            try
            {
                client = new TcpClient(ip.Text, int.Parse(port.Text));
            }
            catch
            {
                MessageBox.Show("Некорректный ip / номер порта");
                return;
            }
            Hide();
            ChatClient chat = new ChatClient(client, name.Text, BackColor);
            chat.ShowDialog();
            Close();
        }
    }
}
