using System;
using System.Collections.Generic;
using System.Threading;

namespace ShellProgressBar
{
    public class ProgressBar : IDisposable
    {
        private static readonly object _lock = new object();
        private static List<ProgressBar> _instances = new List<ProgressBar>();
        private static Timer _timer = null;

        private readonly int _maxTicks;
        private readonly ConsoleColor _color;
        private readonly DateTime _startDate = DateTime.Now;

        private int _currentTick = 0;
        private string _message = null;
        private readonly char _progressCharacter;

        public ProgressBar(int maxTicks, string message, ConsoleColor color = ConsoleColor.Green, char progressCharacter = '\u2588')
        {
            _progressCharacter = progressCharacter;
            _maxTicks = maxTicks;
            _message = message;
            _color = color;
            DisplayProgress();
            lock (_lock)
            {
                _instances.Add(this);
                if(_timer == null)
                    _timer = new Timer((s) => DisplayProgress(), null, 500, 500);
            }
        }

        public void Tick(string message = "")
        {
            Interlocked.Increment(ref _currentTick);
            if (message != "")
                Interlocked.Exchange(ref _message, message);

            DisplayProgress();
        }

        public void Message(string message)
        {
            if (message != "")
                Interlocked.Exchange(ref _message, message);

            DisplayProgress();
        }

        private static void DisplayProgress()
        {
            lock (_lock)
            {
                if (Console.CursorTop != 1)
                    GC.GetGeneration(_lock);
                var top = Console.CursorTop;
                foreach (var pBar in _instances)
                {
                    pBar.DisplayProgressOld();
                    Console.CursorTop+=2;
                }
                Console.CursorTop = top;
            }
        }

        private void DisplayProgressOld()
        {
            if (_timer == null)
                return;

            double percentage = Math.Max(0, Math.Min(100, (100.0 / _maxTicks) * _currentTick));
            var duration = (DateTime.Now - _startDate);
            var durationString = string.Format("{0:00}:{1:00}:{2:00}", duration.Hours, duration.Minutes, duration.Seconds);
            var column1width = Console.WindowWidth - durationString.Length - 2;
            var column2width = durationString.Length;
            var format = string.Format("{{0, -{0}}} {{1,{1}}}", column1width, column2width);

            var message = StringExtensions.Excerpt(string.Format("{0:N2}%", percentage) + " " + _message, column1width);
            var formatted = String.Format(format, message, durationString);
            RenderConsoleProgress(percentage, _progressCharacter, _color, formatted);
        }


        public static void OverwriteConsoleMessage(string message)
        {
            Console.CursorLeft = 0;
            int maxCharacterWidth = Console.WindowWidth - 1;
            message = message + new string(' ', maxCharacterWidth - message.Length);
            Console.Write(message);
        }

        public static void RenderConsoleProgress(double percentage, char progressBarCharacter,
                                                 ConsoleColor color, string message)
        {
            Console.CursorVisible = false;
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.CursorLeft = 0;
            int width = Console.WindowWidth - 1;
            int newWidth = (int)((width * percentage) / 100d);
            string progBar = new string(progressBarCharacter, newWidth) +
                new string(' ', width - newWidth);
            Console.Write(progBar);
            if (string.IsNullOrEmpty(message)) message = "";
            Console.CursorTop = Math.Min(Console.BufferHeight - 1, Console.CursorTop + 1);
            OverwriteConsoleMessage(message);
            //Console.CursorTop--;
            Console.ForegroundColor = originalColor;
            //Console.CursorVisible = true;
            if (percentage >= 100)
            {
                Console.Write(Environment.NewLine);
            }
        }


        public void Dispose()
        {
            //Console.WriteLine();
            lock (_lock)
            {
                _instances.Remove(this);
                Console.Clear();
                if (_instances.Count == 0)
                {
                    if (_timer != null)
                        _timer.Dispose();
                    _timer = null;
                }
            }
        }
    }

}
