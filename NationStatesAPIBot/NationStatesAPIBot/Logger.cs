﻿using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NationStatesAPIBot
{
    public class Logger
    {
        public LogSeverity SeverityThreshold { get; set; } = LogSeverity.Info;
        private StringBuilder loggingStringBuilder = new StringBuilder();
         bool fileLogging = false;
        public async Task LogAsync(LogSeverity logSeverity, string source, string text)
        {
            await Task.Run(() =>
            {
                if (logSeverity <= SeverityThreshold)
                {
                    if (!fileLogging)
                    {
                        StartFileLogging();
                    }
                    ConsoleColor color = ConsoleColor.Gray;
                    if (logSeverity == LogSeverity.Critical)
                    {
                        color = ConsoleColor.Red;
                    }
                    else if (logSeverity == LogSeverity.Error)
                    {
                        color = ConsoleColor.DarkRed;
                    }
                    else if (logSeverity == LogSeverity.Warning)
                    {
                        color = ConsoleColor.Yellow;
                    }
                    else if (logSeverity == LogSeverity.Info)
                    {
                        color = ConsoleColor.Magenta;
                    }
                    else if (logSeverity == LogSeverity.Debug)
                    {
                        color = ConsoleColor.Green;
                    }
                    else if (logSeverity == LogSeverity.Verbose)
                    {
                        color = ConsoleColor.Cyan;
                    }
                    Console.ForegroundColor = color;
                    string message = $"[{DateTime.Now} at {source}] {logSeverity} : {text}";
                    Console.WriteLine(message);
                    loggingStringBuilder.AppendLine(message);
                }
            });
        }

        private void StartFileLogging()
        {
            fileLogging = true;
            Task.Run(async () => await RunFileLogging());
        }

        private async Task RunFileLogging()
        {
            while (fileLogging)
            {
                string toWrite = loggingStringBuilder.ToString();
                loggingStringBuilder.Clear();
                try
                {
                    await File.AppendAllTextAsync($"log_{DateTime.UtcNow.Year}{DateTime.UtcNow.Month}{DateTime.UtcNow.Day}.txt", toWrite);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[{DateTime.UtcNow} at RunFileLogging] {LogSeverity.Critical} : {ex.ToString()}");
                    Console.ResetColor();
                }
                await Task.Delay(1000);
            }
        }

        public void StopFileLogging()
        {
            fileLogging = false;
        }
    }
}
