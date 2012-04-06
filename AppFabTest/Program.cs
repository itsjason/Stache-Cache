namespace AppFabTest
{
    using System;
    using System.Text;

    class Program
    {
        static Random a = new Random();
        private static ICacheService _cache;

        static void Main(string[] args)
        {
            _cache = new CacheService(5, new[] {"10.194.240.28"}, "test1", "teeeeest", 5000, 10);

            var go = true;
            while (true)
            {
                go = DoIt(args);
            }
        }

        static bool DoIt(string[] args)
        {
            // get random number
            var length = a.Next(100, 10000);
            var key = RandomString(35);

            var res = _cache.Get(key, () => RandomString(length, false));

            Console.Write("{0}", _cache.UsingRemote ? "+" : ".");

            return true;
        }

        private static string RandomString(int size, bool lowerCase = false)
        {
            var builder = new StringBuilder();
            var random = new Random();
            for (int i = 0; i < size; i++)
            {
                char ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }
    }

    public class CommandObject
    {
        public string Server { get; set; }
        public int Timeout { get; set; }
        public bool ContinueOnFail { get; set; }
    }
}
