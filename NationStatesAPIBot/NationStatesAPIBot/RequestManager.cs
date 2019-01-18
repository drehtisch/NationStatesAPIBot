﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NationStatesAPIBot
{
    public static class RequestManager
    {
        static readonly int apiVersion = 9;
        public static bool Initialized { get; private set; }
        static string clientKey;
        static string telegramID;
        static string secretKey;
        static string contact;
        static string UserAgent;
        public static void Initialize()
        {
            Logger.LogThreshold = (int)LogLevel.INFO;
            Initialized = LoadConfig();
        }

        private static bool LoadConfig()
        {
            Logger.Log(LogLevel.INFO, "Trying to load config file");
            string path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}keys.config";
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path).ToList();
                if (lines.Exists(cl => cl.StartsWith("clientKey=")) &&
                   lines.Exists(t => t.StartsWith("telegramID=")) &&
                   lines.Exists(s => s.StartsWith("secretKey=")) &&
                   lines.Exists(c => c.StartsWith("contact=")))
                {
                    clientKey = lines.Find(l => l.StartsWith("clientKey=")).Split("=")[1];
                    telegramID = lines.Find(l => l.StartsWith("telegramID=")).Split("=")[1];
                    secretKey = lines.Find(l => l.StartsWith("secretKey=")).Split("=")[1];
                    contact = lines.Find(c => c.StartsWith("contact=")).Split("=")[1];
                    if (lines.Exists(c => c.StartsWith("logLevel=")))
                    {
                        if (int.TryParse(lines.Find(c => c.StartsWith("contact=")).Split("=")[1], out int value))
                        {
                            Logger.LogThreshold = value;
                        }
                    }
                    UserAgent = $"NationStatesAPIBot (https://github.com/drehtisch/NationStatesAPIBot) {Program.versionString} contact: {contact}";
                    return true;
                }
                else
                {
                    Logger.Log(LogLevel.ERROR, "Not all required values where specified. Please refer to documentation for information about how to configure properly.");
                    return false;
                }

            }
            else
            {
                Logger.Log(LogLevel.ERROR, $"File {path} not found.");
                Console.Write("Create file now? (y/n)[n]");
                if(Console.ReadKey().Key == ConsoleKey.Y)
                {
                    clientKey = GetValue("Enter your clientKey: ");
                    telegramID = GetValue("Enter your telegramID: ");
                    secretKey = GetValue("Enter your secretKey: ");
                    contact = GetValue("Enter your contact: ");
                    File.WriteAllText(path,
                        $"clientKey={clientKey}{Environment.NewLine}" +
                        $"telegramID={telegramID}{Environment.NewLine}" +
                        $"secretKey={secretKey}{Environment.NewLine}" +
                        $"contact={clientKey}{Environment.NewLine}");
                }
                return false;
            }
        }

        private static string GetValue(string description)
        {
            Console.Write(description);
            var value = Console.ReadLine();
            Console.WriteLine();
            return value;
        }

        public static List<string> GetNewNations()
        {
            return new List<string>();
        }
    }
}