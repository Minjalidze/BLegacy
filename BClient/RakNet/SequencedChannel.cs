using System;

namespace RakNet
{
    internal sealed class SequencedChannel : BaseChannel
    {
        private int _localSequence;
        private ushort _remoteSequence;
        private readonly bool _reliable;
        private NetPacket _lastPacket;
        private readonly NetPacket _ackPacket;
        private bool _mustSendAck;
        private readonly byte _id;
        private long _lastPacketSendTime;

        public SequencedChannel(NetPeer peer, bool reliable, byte id) : base(peer)
        {
            _id = id;
            _reliable = reliable;
            if (_reliable)
                _ackPacket = new NetPacket(PacketProperty.Ack, 0) {ChannelId = id};
        }

        protected override bool SendNextPackets()
        {
            if (_reliable && OutgoingQueue.Count == 0)
            {
                var currentTime = DateTime.UtcNow.Ticks;
                var packetHoldTime = currentTime - _lastPacketSendTime;
                if (packetHoldTime >= Peer.ResendDelay * TimeSpan.TicksPerMillisecond)
                {
                    var packet = _lastPacket;
                    if (packet != null)
                    {
                        _lastPacketSendTime = currentTime;
                        Peer.SendUserData(packet);
                    }
                }
            }
            else
            {
                lock (OutgoingQueue)
                {
                    while (OutgoingQueue.Count > 0)
                    {
                        var packet = OutgoingQueue.Dequeue();
                        _localSequence = (_localSequence + 1) % NetConstants.MaxSequence;
                        packet.Sequence = (ushort)_localSequence;
                        packet.ChannelId = _id;
                        Peer.SendUserData(packet);

                        if (_reliable && OutgoingQueue.Count == 0)
                        {
                            _lastPacketSendTime = DateTime.UtcNow.Ticks;
                            _lastPacket = packet;
                        }
                        else
                        {
                            Peer.NetManager.NetPacketPool.Recycle(packet);
                        }
                    }
                }
            }

            if (_reliable && _mustSendAck)
            {
                _mustSendAck = false;
                _ackPacket.Sequence = _remoteSequence;
                Peer.SendUserData(_ackPacket);
            }

            return _lastPacket != null;
        }

        public override bool ProcessPacket(NetPacket packet)
        {
            if (packet.IsFragmented)
                return false;
            if (packet.Property == PacketProperty.Ack)
            {
                if (_reliable && _lastPacket != null && packet.Sequence == _lastPacket.Sequence)
                    _lastPacket = null;
                return false;
            }
            var relative = NetUtils.RelativeSequenceNumber(packet.Sequence, _remoteSequence);
            var packetProcessed = false;
            if (packet.Sequence < NetConstants.MaxSequence && relative > 0)
            {
                if (Peer.NetManager.EnableStatistics) 
                {
                    Peer.Statistics.AddPacketLoss(relative - 1);
                    Peer.NetManager.Statistics.AddPacketLoss(relative - 1);
                }

                _remoteSequence = packet.Sequence;
                Peer.NetManager.CreateReceiveEvent(
                    packet, 
                    _reliable ? DeliveryMethod.ReliableSequenced : DeliveryMethod.Sequenced, 
                    NetConstants.ChanneledHeaderSize,
                    Peer);
                packetProcessed = true;
            }

            if (_reliable)
            {
                _mustSendAck = true;
                AddToPeerChannelSendQueue();
            }

            return packetProcessed;
        }
    }
}
