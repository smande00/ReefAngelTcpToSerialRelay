using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

using ReefAngelTCPRelay;



namespace TCPToSerialRelay
{

    [RunInstaller(true)]
    public sealed class MyServiceInstallerProcess : ServiceProcessInstaller
    {
        public MyServiceInstallerProcess()
        {
            this.Account = ServiceAccount.NetworkService;
        }
    }

    [RunInstaller(true)]
    public sealed class MyServiceInstaller : ServiceInstaller
    {
        public MyServiceInstaller()
        {
            this.Description = "ReefAngel TCP to USB Relay";
            this.DisplayName = "ReefAngel TCP Relay";
            this.ServiceName = "ReefAngelTCPRelayService";
            this.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            
        }
    }

    

    internal class Program
    {
    
        static void Install(bool undo, string[] args)
        {
            try
            {
                Console.WriteLine(undo ? "uninstalling" : "installing");
                using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;
                    try
                    {
                        if (undo)
                        {                            
                            inst.Uninstall(state);
                        }
                        else
                        {
                            inst.Install(state);
                            inst.Commit(state);
                        }
                    }
                    catch
                    {
                        try
                        {
                            inst.Rollback(state);                            
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

     
        private static void Main(string[] args)
        {

            bool install = false, uninstall = false, console = false, rethrow = false;
            #  if DEBUG
                console=true;
            #endif
            try
            {
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case "-i":
                        case "-install":
                            install = true; break;
                        case "-u":
                        case "-uninstall":
                            uninstall = true; break;
                        case "-c":
                        case "-console":
                            console = true; break;
                        default:
                            console = true;                            
                            break;
                    }
                }

                if (uninstall)
                {
                    Install(true, args);
                }
                if (install && !console && !uninstall)
                {
                    Install(false, args);
                    ServiceController sc = new ServiceController();
                    sc.ServiceName = "ReefAngelTcpRelayService";
                    sc.Start();
                }
                if (console)
                {
                    ReefAngelTCPRelayApp app = new ReefAngelTCPRelayApp();
                    Console.WriteLine("Starting...");
                    app.Startup();
                    Console.WriteLine("System running; press any key to stop");
                    Console.ReadKey(true);
                    app.Shutdown();
                    Console.WriteLine("System stopped");
                }
                else 
                {
                    rethrow = true; // so that windows sees error...
                    ServiceBase[] services = { new ReefAngelTCPRelayService() };
                    ServiceBase.Run(services);
                    rethrow = false;
                }

                
            }
            catch (Exception ex)
            {                
                Console.Error.WriteLine(ex.Message);                
            }
        }
    }
}
