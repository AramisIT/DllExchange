using System;
using System.ServiceProcess;

namespace EmailSenderService
{
    static class Program
    {
        static void Main()
        {
            try
            {
                ServiceBase[] ServicesToRun = new ServiceBase[] {new Sender()};
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception){}
        }
    }
}
