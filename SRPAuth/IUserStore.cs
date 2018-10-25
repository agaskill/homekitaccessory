using System;
using System.Collections.Generic;
using System.Text;

namespace SRPAuth
{
    public interface IUserStore
    {
        UserVerifier Lookup(string identity);
    }
}
