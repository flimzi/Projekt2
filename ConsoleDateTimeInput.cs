using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal class ConsoleDateTimeInput(DateTime? dateTime = null) : IEnumerable<ConsoleDateTimeInput.DateTimeSegment>
    {
        internal class DateTimeSegment(string formatString)
        {
            public string FormatString { get; } = formatString;

            public virtual string Format(DateTime dt)
            {
                return FormatString;
            }
        }

        internal abstract class AdjustableDateTimeSegment(string formatString) : DateTimeSegment(formatString)
        {
            // this can be abstracted by providing prop like () => Hour, name of function
            public abstract DateTime Reset(DateTime dt, DateTime changed);
            public abstract DateTime Forward(DateTime dt);
            public abstract DateTime Rewind(DateTime dt);

            public override string Format(DateTime dt)
            {
                return dt.ToString(FormatString);
            }
        }

        internal class MinuteSegment : AdjustableDateTimeSegment
        {
            public MinuteSegment() : base("mm")
            { }

            public override DateTime Reset(DateTime dt, DateTime changed)
            {
                // zeby zrobic looping to trzeba zrobic inaczej ale da sie
                return dt.Hour != changed.Hour ? dt : changed;
            }

            public override DateTime Forward(DateTime dt)
            {
                return Reset(dt, dt.AddMinutes(1));
            }

            public override DateTime Rewind(DateTime dt)
            {
                return Reset(dt, dt.AddMinutes(-1));
            }
        }

        internal class HourSegment : AdjustableDateTimeSegment
        {
            public HourSegment() : base("HH")
            { }

            public override DateTime Reset(DateTime dt, DateTime changed)
            {
                return dt.Day != changed.Day ? dt : changed;
            }

            public override DateTime Forward(DateTime dt)
            {
                return Reset(dt, dt.AddHours(1));
            }

            public override DateTime Rewind(DateTime dt)
            {
                return Reset(dt, dt.AddHours(-1));
            }
        }

        internal class DaySegment : AdjustableDateTimeSegment
        {
            public DaySegment() : base("dd")
            { }

            public override DateTime Reset(DateTime dt, DateTime changed)
            {
                return dt.Month != changed.Month ? dt : changed;
            }

            public override DateTime Forward(DateTime dt)
            {
                return Reset(dt, dt.AddDays(1));
            }

            public override DateTime Rewind(DateTime dt)
            {
                return Reset(dt, dt.AddDays(-1));
            }
        }

        internal class MonthSegment : AdjustableDateTimeSegment
        {
            public MonthSegment() : base("MM")
            { }

            public override DateTime Reset(DateTime dt, DateTime changed)
            {
                return dt.Year != changed.Year ? dt : changed;
            }

            public override DateTime Forward(DateTime dt)
            {
                return Reset(dt, dt.AddMonths(1));
            }

            public override DateTime Rewind(DateTime dt)
            {
                return Reset(dt, dt.AddMonths(-1));
            }
        }

        internal class YearSegment : AdjustableDateTimeSegment
        {
            public YearSegment() : base("yyyy")
            { }

            public override DateTime Reset(DateTime dt, DateTime changed)
            {
                return changed;
            }

            public override DateTime Forward(DateTime dt)
            {
                return Reset(dt, dt.AddYears(1));
            }

            public override DateTime Rewind(DateTime dt)
            {
                return Reset(dt, dt.AddYears(-1));
            }
        }

        protected CursorLinkedList<DateTimeSegment> Segments { get; } = new();
        public DateTime DateTime { get; protected set; } = dateTime ?? DateTime.Now;

        public override string ToString()
        {
            return string.Join("", Segments.Select(s => s.FormatString));
        }

        public void Erase()
        {
            foreach (var backspace in Enumerable.Repeat("\b", ToString().Length))
                Console.Write(backspace);
        }

        public void Write(DateTime dateTime, bool blink)
        {
            foreach (var segment in Segments.Nodes)
            {
                if (blink && segment.IsCurrent())
                    Console.ForegroundColor = ConsoleColor.Black;

                Console.Write(segment.Value.Format(dateTime));
                Console.ResetColor();
            }
        }

        // zmien DateTime tylko przy 
        public bool Run()
        {
            var blink = false;
            var dateTime = DateTime;

            while (Segments.Cursor is not null)
            {
                Write(dateTime, blink = !blink);
                Thread.Sleep(200);
                Erase();

                if (Console.KeyAvailable && Segments.Cursor.Value is AdjustableDateTimeSegment adjustableDateTimeSegment)
                {
                    var keyInfo = Console.ReadKey();

                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.UpArrow:
                            dateTime = adjustableDateTimeSegment.Forward(DateTime);
                            break;
                        case ConsoleKey.DownArrow:
                            dateTime = adjustableDateTimeSegment.Rewind(DateTime);
                            break;
                        case ConsoleKey.LeftArrow:
                            Segments.MoveCursorLeftTo(s => s is AdjustableDateTimeSegment);
                            break;
                        case ConsoleKey.RightArrow:
                            Segments.MoveCursorRightTo(s => s is AdjustableDateTimeSegment);
                            break;
                        case ConsoleKey.Enter:
                            DateTime = dateTime;
                            return true;
                        case ConsoleKey.Escape:
                            Console.WriteLine();
                            return false;
                    }
                }
            }

            return false;
        }

        public DateTime GetDate()
        {
            Run();
            return DateTime;
        }

        public void Add(DateTimeSegment segment)
        {
            Segments.AddLast(segment);
        }

        public IEnumerator<DateTimeSegment> GetEnumerator()
        {
            return Segments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Segments.GetEnumerator();
        }
    }
}
