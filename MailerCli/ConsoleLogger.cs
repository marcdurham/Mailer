﻿using MailerCommon;

namespace MailerCli
{
    public class ConsoleLogger : ICustomLogger
    {
        public void LogInformation(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.sss")}: {message}");
        }
    }
}
