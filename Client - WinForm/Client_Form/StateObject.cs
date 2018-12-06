using System.Net.Sockets;
using System.Text;

namespace Client_Form
{
    class StateObject
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
