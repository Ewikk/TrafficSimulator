using System.Net.Sockets;
using System.Net;
using System.Text;

namespace CarProcess
{
    public class Program
    {
        public static void Main()
        {
            Car connectionManager = new Car();
            connectionManager.Start();
        }
    }
}
