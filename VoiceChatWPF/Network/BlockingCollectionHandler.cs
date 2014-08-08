using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VoiceChatWPF.Network
{
    internal class BlockingCollectionHandler : IDisposable
    {
        private readonly Socket _socketClient;
        public BlockingCollection<byte[]> BufferCollection;
        private Task _dataCollectTask;

        public BlockingCollectionHandler(Socket socketClient)
        {
            BufferCollection = new BlockingCollection<byte[]>();
            _socketClient = socketClient;
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
                        if (bufBytes.Length == 0 || !_socketClient.Connected)
                            break;
                        _socketClient.Send(bufBytes);
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