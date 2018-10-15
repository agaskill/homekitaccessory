namespace HomeKitAccessory
{
    public static class TLVType
    {
        public const byte Method = 0x00;
        public const byte Identifier = 0x01;
        public const byte Salt = 0x02;
        public const byte PublicKey = 0x03;
        public const byte Proof = 0x04;
        public const byte EncryptedData = 0x05;
        public const byte State = 0x06;
        public const byte Error = 0x07;
        public const byte RetryDelay = 0x08;
        public const byte Certificate = 0x09;
        public const byte Signature = 0x0A;
    }
}