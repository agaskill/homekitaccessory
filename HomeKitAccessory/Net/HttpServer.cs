namespace HomeKitAccessory.Net
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using HomeKitAccessory.Data;
    using HomeKitAccessory.Pairing.PairSetupStates;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NLog;

    class HttpServer
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private volatile bool running;
        private TcpListener tcpServer;
        private Server server;

        public HttpServer(Server server)
        {
            this.server = server;
        }

        public async Task Listen(int port)
        {
            tcpServer = new TcpListener(IPAddress.Any, port);
            tcpServer.Start();
            running = true;

            logger.Debug("HttpServer listening on port {0}", port);

            while (running) {
                TcpClient client;
                try
                {
                    client = await tcpServer.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                logger.Debug("Connection accepted from {0}", client.Client.RemoteEndPoint);

                var httpConnection = new HttpConnection(server, client);
                var clientThread = new Thread(() =>
                {
                    try
                    {
                        httpConnection.Start();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }
                });
                clientThread.IsBackground = true;
                clientThread.Start();
            }
        }

        public void Stop()
        {
            logger.Info("HttpServer.Stop");
            running = false;
            tcpServer.Stop();
        }
    }
}