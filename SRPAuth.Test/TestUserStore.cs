using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using SRPAuth;

namespace SRPAuth.Test
{
    class TestUserStore : IUserStore
    {
        private UserVerifier userVerifier;

        public TestUserStore(UserVerifier userVerifier)
        {
            this.userVerifier = userVerifier;
        }

        public UserVerifier Lookup(string identity)
        {
            return userVerifier;
        }
    }
}
