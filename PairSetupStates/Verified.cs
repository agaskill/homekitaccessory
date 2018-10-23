namespace HomeKitAccessory.PairSetupStates
{
    class Verified : PairSetupState
    {
        public Sodium.Key AccessoryToControllerKey {get; private set;}
        public Sodium.Key ControllerToAccessoryKey {get; private set;}

        public Verified(
            Server server,
            Sodium.Key accessoryToControllerKey,
            Sodium.Key controllerToAccessoryKey)
            : base(server)
        {
            AccessoryToControllerKey = accessoryToControllerKey;
            ControllerToAccessoryKey = controllerToAccessoryKey;
        }
    }
}