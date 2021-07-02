using System;
using System.Collections.Generic;
using System.Threading;

namespace DrakesBasketballCourtServer
{
    public enum MessageType
    {
        Default,
        Warning,
        Error
    }

    public static class ServerConsole
    {
        static Thread _consoleUpdate;

        static int _hours;
        static int _minutes;
        static int _seconds;

        static List<string> _serverEventsLog = new List<string>();

        private static void TimerUpdate()
        {
            _seconds++;

            if (_seconds == 60)
            {
                _minutes++;
                _seconds = 0;
            }
            if (_minutes == 60)
            {
                _hours++;
                _minutes = 0;
            }
        }

        public static void Init()
        {
            Console.CursorVisible = false;

            _consoleUpdate = new Thread(() =>
            {
                while(true)
                {
                    Console.Clear();
                    Console.WriteLine($"Сервер находится в работе: {_hours}:{_minutes}:{_seconds}");
                    Console.WriteLine($"Игроков на сервере: {MainHub.playersCounter}");
                    Console.WriteLine($"Игровых комнат: {MainHub.roomsCounter}\n");
                    if(_serverEventsLog.Count != 0)
                    {
                        foreach (string logEntry in _serverEventsLog)
                        {
                            if (logEntry.Contains("[WARNING]")) Console.ForegroundColor = ConsoleColor.Yellow;
                            else if (logEntry.Contains("[ERROR]")) Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(logEntry);
                            Console.ResetColor();
                        }
                    }
                    Thread.Sleep(1000);
                    TimerUpdate();
                }
            });
            _consoleUpdate.Start();
        }

        public static void CreateLogMessage(string logString, MessageType messageType)
        {
            switch(messageType)
            {
                case MessageType.Default:
                    _serverEventsLog.Add($"[{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}] {logString}");
                    break;
                case MessageType.Warning:
                    _serverEventsLog.Add($"[{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}] [WARNING] {logString}");
                    break;
                case MessageType.Error:
                    _serverEventsLog.Add($"[{DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}] [ERROR] {logString}");
                    break;
            }
        }
    }
}
