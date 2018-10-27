using System;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var testClient = new TestClient("127.0.0.1", 5002);
                testClient.PairSetup("547-07-173");
                testClient.PairVerify();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
