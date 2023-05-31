using System.Net.Sockets;
using System.Net;
using System.Text;

namespace TramProcess
{
    public class Program
    {
        public static void Main()
        {
            TramProcess trams = new TramProcess();
            trams.Start();
        }
    }
}
