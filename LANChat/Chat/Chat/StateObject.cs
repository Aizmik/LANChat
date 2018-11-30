using System.Net.Sockets;
using System.Text;

namespace Chat
{
    /// <summary>
    /// Класс чтобы считывать данные с клиента
    /// </summary>
    public class StateObject
    {
        //Сокет клиента
        public Socket workSocket = null;
        //Размер отправляемого буфера
        public const int BufferSize = 1024;
        //Получаемый буфер
        public byte[] buffer = new byte[BufferSize];
        //Полученная строка
        public StringBuilder sb = new StringBuilder();
    }
}
