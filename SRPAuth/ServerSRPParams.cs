using System;
using System.Collections.Generic;
using System.Text;

namespace SRPAuth
{
    public class ServerSRPParams
    {
        public byte[] Prime { get; set; }
        public byte[] Generator { get; set; }
        public byte[] Salt { get; set; }
        public byte[] ServerPublic { get; set; }
    }
}
