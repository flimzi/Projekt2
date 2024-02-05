using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Projekt
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var dtInput = new ConsoleDateTimeInput
            //{
            //    new ConsoleDateTimeInput.DaySegment(),
            //    new ConsoleDateTimeInput.DateTimeSegment("."),
            //    new ConsoleDateTimeInput.MonthSegment(),
            //    new ConsoleDateTimeInput.DateTimeSegment("."),
            //    new ConsoleDateTimeInput.YearSegment(),
            //    new ConsoleDateTimeInput.DateTimeSegment(" "),
            //    new ConsoleDateTimeInput.HourSegment(),
            //    new ConsoleDateTimeInput.DateTimeSegment(":"),
            //    new ConsoleDateTimeInput.MinuteSegment(),
            //};

            //Console.WriteLine("wybierz date:");

            //if (dtInput.Run())
            //    Console.WriteLine($"wybrano {dtInput.DateTime}");
            //else
            //    Console.WriteLine("nie wybrano");

            App.Run();
        }
    }
}
