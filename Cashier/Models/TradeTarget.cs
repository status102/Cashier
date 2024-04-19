namespace Cashier.Models
{
    public class TradeTarget
    {
        public TradeTarget()
        {
        }

        public TradeTarget(uint id, string worldName, uint objectId, string name)
        {
            WorldId = id;
            WorldName = worldName;
            ObjectId = objectId;
            PlayerName = name;
        }

        public uint? WorldId { get; init; }

        public string? WorldName { get; init; }

        public uint ObjectId { get; init; } = 0xE0000000;

        public string? PlayerName { get; init; }

        public override bool Equals(object? obj)
        {
            if(obj is TradeTarget target) {
                return target.ObjectId == ObjectId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ObjectId.GetHashCode();
        }

        public static bool operator ==(TradeTarget? left, TradeTarget? right)
        {
            if (left is null) {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(TradeTarget? left, TradeTarget? right)
        {
            if (left is null) {
                return right is not null;
            }
            return !left.Equals(right);
        }
    }
}
