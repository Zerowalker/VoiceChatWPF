using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VoiceChatWPF.Models
{
    internal class BlockingCollectionHandler : IDisposable
    {
        private readonly Socket _tcpClient;
        public BlockingCollection<byte[]> BufferCollection;
        private Task _dataCollectTask;

        public BlockingCollectionHandler(Socket tcpClient)
        {
            BufferCollection = new BlockingCollection<byte[]>();
            _tcpClient = tcpClient;
            //_dataCollectTask = TaskFactory( CancellationToken.None,TaskCreationOptions.LongRunning,TaskContinuationOptions.None, TaskScheduler.Default);
        }

        public void Dispose()
        {
            if (_dataCollectTask != null)
            {
                _dataCollectTask.Wait();
                _dataCollectTask.Dispose();
            }
            BufferCollection.Dispose();
        }

        private void Processing()
        {
            try
            {
                //using (var waveWriterYour = new WaveFileWriter(@"F:\Desktop\\Recordshit\\" + Environment.UserName + " - " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff") + ".wav", new WaveFormat(48000, 16, 2)))
                while (true)
                {
                    byte[] bufBytes;
                    if (BufferCollection.TryTake(out bufBytes, Timeout.Infinite))
                    {
                        if (bufBytes.Length == 0)
                            break;
                        _tcpClient.Send(bufBytes);
                        //waveWriterYour.Write(bufBytes, 0, bufBytes.Length);
                        //waveWriterYour.Flush();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                if (e is SocketException)
                    return;
                MessageBox.Show("Processing BlockingCollection: " + e.Message);
            }
        }

        public void BeginProcessing()
        {
            BufferCollection = new BlockingCollection<byte[]>();
            _dataCollectTask = Task.Factory.StartNew(param => Processing(), CancellationToken.None,
                TaskCreationOptions.LongRunning);
        }
    }
}