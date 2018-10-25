using System.Collections.Generic;

namespace HomeKitAccessory.Net.PairSetupStates
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

        public override void UpdateEnvironment(IDictionary<string, object> env)
        {
            env["hap.ReadKey"] = ControllerToAccessoryKey;
            env["hap.WriteKey"] = AccessoryToControllerKey;
        }
    }
}