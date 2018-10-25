using System;

namespace HomeKitAccessory
{
    public struct AccessoryCharacteristicId : IEquatable<AccessoryCharacteristicId>
    {
        public int AccessoryId;
        public int InstanceId;
        public AccessoryCharacteristicId(int accessoryId, int instanceId)
        {
            AccessoryId = accessoryId;
            InstanceId = instanceId;
        }

        public override string ToString()
        {
            return $"{{\"aid\":{AccessoryId},\"iid\":{InstanceId}}}";
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