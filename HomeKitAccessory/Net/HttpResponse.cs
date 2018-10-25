using System.Collections.Generic;
using System.Collections.Specialized;

namespace HomeKitAccessory.Net
{
    class HttpResponse
    {
        public int StatusCode {get;set;}
        public string StatusPhrase {get;set;}
        public NameValueCollection ResponseHeaders {get; private set;}
        public byte[] Body {get;set;}
        public Dictionary<string, object> Context {get; private set;}

        public HttpResponse()
        {
            Context = new Dictionary<string, object>();
            ResponseHeaders = new NameValueCollection();
        }
    }
}