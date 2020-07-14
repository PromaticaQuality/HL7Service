using System.ServiceProcess;
using HL7.Library;

namespace Promatica.PASService
{
    public partial class Service1 : ServiceBase
    {
        private int commandCode = 200;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            new PasService().InitialisePasService();
        }

        protected override void OnStop()
        {
            new PasService().StopServiceThreads();
        } 

        protected override void OnCustomCommand(int command)
        {
            if (command == 200)
            {
                commandCode = command;
                new PasService().ExecuteHl7BulkUpload();
            }
        }
    }
}
