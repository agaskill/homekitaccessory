using System.Collections.Generic;

namespace HomeKitAccessory
{
    public abstract class Accessory
    {
        public int Id {get;set;}
        public abstract IEnumerable<Characteristic> Characteristics {get;}
    }
}