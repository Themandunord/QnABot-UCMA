using System;
using System.Threading.Tasks;


namespace UCMA_IVR_Demo
{
    class Program
    {
        private static LyncServer _server;

        static void Main(string[] args)
        {
          AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;  //quick and dirty error handling :)
            _server = new LyncServer();
            _server.LyncServerReady += server_LyncServerReady;
            _server.IncomingCall += server_IncomingCall;
            Task t = _server.Start();
            

            Console.ReadLine();
            var stopping = _server.Stop();
            stopping.Wait();

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
        }

        private static void server_LyncServerReady(object sender, EventArgs e)
        {
            Console.WriteLine("Lync/SfB Server Ready");
        }
        
        static void server_IncomingCall(object sender, Microsoft.Rtc.Collaboration.CallReceivedEventArgs<Microsoft.Rtc.Collaboration.InstantMessagingCall> e)
        {
            var initialMessage = e.ToastMessage.Message;
             
            var qnaconvo = new QnAConversation();
            qnaconvo.Start(initialMessage, e.Call);

        }
                  
    
    }
}
