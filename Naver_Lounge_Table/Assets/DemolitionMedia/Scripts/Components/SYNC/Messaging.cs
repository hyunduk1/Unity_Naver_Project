namespace DemolitionStudios.DemolitionMedia
{
    using ClockType = System.Double;

    class MessageBase
    {
        public Protocol.PacketId type;
    }

    class SyncMessage : MessageBase
    {
        public SyncMessage()
        {
            type = Protocol.PacketId.Sync;
        }

        public ulong timestamp;
        public ClockType position;
    }

    class SpeedMessage : MessageBase
    {
        public SpeedMessage()
        {
            type = Protocol.PacketId.Speed;
        }

        public ClockType speed;
    }

    class PauseMessage : MessageBase
    {
        public PauseMessage()
        {
            type = Protocol.PacketId.Pause;
        }

        public bool pause;
    }
}
