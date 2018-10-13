using Newtonsoft.Json.Linq;

namespace HomeKitAccessory
{
    public class HapResponse
    {
        public int Status {get;set;}
        public JObject Body {get;set;}
    }
}