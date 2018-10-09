using System.Collections.Generic;

namespace HomeKitAccessory
{
    public class Accessory
    {
        public int Id {get;set;}
        public List<Characteristic> Characteristics {get;set;}
    }
}