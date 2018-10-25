using System;

namespace HapAccessories.Serialization
{
    public class HapException : Exception
    {
        public ulong AccessoryId { get; private set; }
        public ulong CharacteristicId { get; private set; }
        public int ErrorCode { get; private set; }

        public HapException(ulong accessoryId, ulong characteristicId, int errorCode)
        {
            AccessoryId = accessoryId;
            CharacteristicId = characteristicId;
            ErrorCode = errorCode;
        }
    }

    public class ReadOnlyException : HapException
    {
        public ReadOnlyException(ulong accessoryId, ulong characteristicId) : base(accessoryId, characteristicId, -70404) { }
    }

    public class WriteOnlyException : HapException
    {
        public WriteOnlyException(ulong accessoryId, ulong characteristicId) : base(accessoryId, characteristicId, -70405) { }
    }

    public class NotificationNotSupportedException : HapException
    {
        public NotificationNotSupportedException(ulong accessoryId, ulong characteristicId) : base(accessoryId, characteristicId, -70406) { }
    }

    public class OutOfResourcesException : HapException
    {
        public OutOfResourcesException(ulong accessoryId, ulong characteristicId) : base(accessoryId, characteristicId, -70407) { }
    }

    public class TimeoutException : HapException
    {
        public TimeoutException(ulong accessoryId, ulong characteristicId) : base(accessoryId, characteristicId, -70408) { }
    }

    public class NotExistException : HapException
    {
        public NotExistException(ulong accessoryId, ulong characteristicId) : base(accessoryId, characteristicId, -70409) { }
    }

    public class InvalidValueException : HapException
    {
        public InvalidValueException(ulong accessoryId, ulong characteristicId) : base(accessoryId, characteristicId, -70410) { }
    }
}
