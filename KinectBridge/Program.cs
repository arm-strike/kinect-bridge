using System;
using System.Threading;

namespace KinectBridge
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            BridgeLogger logger = new BridgeLogger();
            BridgeConfiguration configuration = BridgeConfiguration.CreateDefault();
            KinectBridgeService service = null;
            ManualResetEventSlim exitEvent = null;

            try
            {
                service = new KinectBridgeService(configuration, logger);
                exitEvent = new ManualResetEventSlim(false);

                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
                {
                    e.Cancel = true;
                    exitEvent.Set();
                };

                service.Start();
                logger.Info("Ctrl+C で終了します。");
                exitEvent.Wait();
                return 0;
            }
            catch (Exception ex)
            {
                logger.Error("Bridge の起動中に予期しない例外が発生しました: " + ex);
                return 1;
            }
            finally
            {
                if (service != null)
                {
                    service.Dispose();
                }

                if (exitEvent != null)
                {
                    exitEvent.Dispose();
                }
            }
        }
    }
}
