# HomeKitAccessory

A C# implementation of the Apple [HomeKit protocol](https://developer.apple.com/support/homekit-accessory-protocol/)
for accessories

## Getting Started

### Requirements

HomeKitAccessory requires [.NET Core 3.1](https://dotnet.microsoft.com/download),
and requires [libsodium](https://doc.libsodium.org) to be installed for your platform.

On Macos with [Homebrew](https://brew.sh), run
```
brew install libsodium
```
You will also need to add /usr/local/lib to the LD search path:
```
export LD_LIBRARY_PATH=/usr/local/lib
```


### Building

Check out the code using
```
git clone https://github.com/agaskill/homekitaccessory.git
```
Change to the working directory
```
cd homekitaccessory
```
And build the solution
```
dotnet build
```

## Using HomeKitAccessory

HomeKitAccessory uses [NLog](https://nlog-project.org) for logging, so be sure to add that as a dependency
and configure it first according to your logging requirements.

Next you will need to load or create a pairing database
```c#
var pairingDb = new PairingDatabase();
pairingDb.LoadOrInitialize();
```
which will create or load a file named pairingdb.json in the working directory.  More secure
alternatives can be created by implementing the interface `IPairingDatabase`.

You will need to create an instance of `IBonjourProvider`.  Two implementations are included:
- `DnsSdBonjourProvider` works on macos and runs the `dns-sd` utility as a subprocess.
- `MockBonjourProvider` does nothing but write to the console what it _would_ provide as
  arguments to `dns-sd`

Example:
```c#
var bonjourProvider = new Net.DnsSdBonjourProvider();
```

You will need to create an instance of IUserStore to look up valid SRP verifiers.  Again
two implementations are provided:
- `DynamicSetupCodeUserStore` generates a new setup code for each pairing, and provides for displaying
  it to the user in whatever means are appropriate
- `StaticSetupCodeUserStore` is appropriate for a device that does not have any interface and instead
  uses an embedded setup code

Example:
```c#
var userStore = new Pairing.DynamicSetupCodeUserStore(code => Console.WriteLine("*** {0} ***", code));
```

Lastly you need to populate ServerInfo with how the accessory should appear on the network: name,
model, category type, port number.  Refer to the [HomeKit protocol specification](https://developer.apple.com/support/homekit-accessory-protocol/)
for appropriate category values.

Example:
```c#
var serverInfo = new ServerInfo
{
    Name = "MyTestDevice9",
    Model = "TestDevice",
    CategoryId = 1,
    Port = 5002
};
```

Finally the server instance can be created:
```c#
var server = new Server(pairingDb, serverInfo, bonjourProvider, userStore);
```

### Defining Accessories

Before running the server, all accessories need to be defined and registered.
Accessories definitions are outside the scope of this readme and should be
referred to the protocol specification.

### Implementing Accessories

Implementations of standard services like the Accessory Information service are provided, but
most functionality will require implementing service interfaces and registering them in the
service definition.

See the included TestAccessory project for an example of defining a simple switch service
and registering it as an accessory.
