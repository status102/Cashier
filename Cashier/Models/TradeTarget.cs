namespace Cashier.Models
{
    public class TradeTarget
    {
        public TradeTarget()
        {
        }

        public TradeTarget(uint id, string worldName, nint objectId, string name)
        {
            WorldId = id;
            WorldName = worldName;
            ObjectId = objectId;
            PlayerName = name;
        }

        public uint? WorldId { get; init; }

        public string? WorldName { get; init; }

        public nint? ObjectId { get; init; }

        public string? PlayerName { get; init; }
    }
}
