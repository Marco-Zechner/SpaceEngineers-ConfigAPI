using System;
using System.Text;
using NUnit.Framework;

namespace NewTemplateMod.Tests
{
    public static class Debug
    {
        public static void Log(string message)
        {
            TestContext.Out.WriteLine(
                new StringBuilder()
                    .Append(GetPrefix())
                    .Append(message)
                .ToString());
        }

        private static string GetPrefix()
        {
            var now = DateTime.Now;
            return $"[{now:HH:mm:ss.ffff}] ";
        }
    }
}