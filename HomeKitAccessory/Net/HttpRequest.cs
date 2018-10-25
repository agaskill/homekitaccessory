using System.Collections.Generic;
using System.Collections.Specialized;

namespace HomeKitAccessory.Net
{
    class HttpRequest
        {
            public string Method {get;set;}
            public string Path {get;set;}
            public NameValueCollection QueryString {get;set;}
            public NameValueCollection RequestHeaders {get;set;}
            public byte[] Body {get;set;}
            public Dictionary<string, object> Context {get;set;}

            public HttpRequest()
            {
                QueryString = new NameValueCollection();
                RequestHeaders = new NameValueCollection();
                Context = new Dictionary<string, object>();
            }
        }
}