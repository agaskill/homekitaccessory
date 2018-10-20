namespace HomeKitAccessory.PairSetupStates
{
    class Verified : PairSetupState
    {
        public byte[] AccessoryToControllerKey {get; private set;}
        public byte[] ControllerToAccessoryKey {get; private set;}

        public Verified(
            Server server,
            byte[] accessoryToControllerKey,
            byte[] controllerToAccessoryKey)
            : base(server)
        {
            AccessoryToControllerKey = accessoryToControllerKey;
            ControllerToAccessoryKey = controllerToAccessoryKey;
        }
    }
}