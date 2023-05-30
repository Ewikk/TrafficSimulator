using System.Net.Sockets;
using System.Net;
using System.Text;

namespace PedestrianProcess
{
    public class Program
    {
        public static void Main()
        {
            PedestrianProcess pedestrians = new PedestrianProcess();
            pedestrians.Start();
        }
    }
}
