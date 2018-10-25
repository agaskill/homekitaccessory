using Newtonsoft.Json.Linq;

namespace HomeKitAccessory.Net
{
    public class HapResponse
    {
        public int Status {get;set;}
        public JObject Body {get;set;}
    }
}