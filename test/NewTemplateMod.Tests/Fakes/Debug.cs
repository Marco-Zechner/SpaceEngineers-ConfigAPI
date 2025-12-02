using System;
using System.Text;
using mz.Config.Abstractions;
using NUnit.Framework;

namespace NewTemplateMod.Tests
{
    public class Debug : IDebug
    {
        public void Log(string message, string source = "cfg")
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

    public static class Logger
    {   
        private static readonly IDebug _instance = new Debug();
        
        public static void Log(string message, string source = "cfg")
        {
            _instance.Log(message, source);
        }
    }
}