using System.Collections.Generic;
using System.Threading;

namespace RakNet
{
    internal abstract class BaseChannel
    {
        protected readonly NetPeer Peer;
        protected readonly Queue<NetPacket> OutgoingQueue;
        private int _isAddedToPeerChannelSendQueue;

        public int PacketsInQueue
        {
            get { return OutgoingQueue.Count; }
        }

        protected BaseChannel(NetPeer peer)
        {
            Peer = peer;
            OutgoingQueue = new Queue<NetPacket>(64);
        }

        public void AddToQueue(NetPacket packet)
        {
            lock (OutgoingQueue)
            {
                OutgoingQueue.Enqueue(packet);
            }
            AddToPeerChannelSendQueue();
        }

        protected void AddToPeerChannelSendQueue()
        {
            if (Interlocked.CompareExchange(ref _isAddedToPeerChannelSendQueue, 1, 0) == 0)
            {
                Peer.AddToReliableChannelSendQueue(this);
            }
        }

        public bool SendAndCheckQueue()
        {
            var hasPacketsToSend = SendNextPackets();
            if (!hasPacketsToSend)
                Interlocked.Exchange(ref _isAddedToPeerChannelSendQueue, 0);

            return hasPacketsToSend;
        }

        protected abstract bool SendNextPackets();
        public abstract bool ProcessPacket(NetPacket packet);
    }
}
