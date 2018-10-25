using System;
using System.Collections.Generic;
using System.Text;

namespace SRPAuth
{
    public class ClientSRPSession
    {
        public byte[] PremasterSecret { get; set; }
        public byte[] ClientPublic { get; set; }
    }
}
