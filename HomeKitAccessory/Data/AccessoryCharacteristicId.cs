using System;

namespace HomeKitAccessory.Data
{
    public struct AccessoryCharacteristicId : IEquatable<AccessoryCharacteristicId>
    {
        public ulong AccessoryId;
        public ulong InstanceId;
        public AccessoryCharacteristicId(ulong accessoryId, ulong instanceId)
        {
            AccessoryId = accessoryId;
            InstanceId = instanceId;
        }

        public override string ToString()
        {
            return $"{AccessoryId}.{InstanceId}";
        }

        public bool Equals(AccessoryCharacteristicId other)
        {
            return AccessoryId == other.AccessoryId
                && InstanceId == other.InstanceId;
        }

        public override bool Equals(object obj)
        {
            if (obj is AccessoryCharacteristicId other) {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return AccessoryId.GetHashCode() ^ InstanceId.GetHashCode();
        }
    }
}