using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static Projekt.Menu;

namespace Projekt
{
    internal static class App
    {
        internal static Person? User { get; private set; }
        // to by moglo byc ustawione na carer jesli bylby to rekord
        // ale teraz w sumie tez by moglo byc bo to chyba nie ma znaczenia jak na razie
        internal static Patient? SelectedPatient { get; private set; }

        static App()
        {
            Manager.Seed();
        }

        public static void Run()
        {
            var menu1 = new Menu();

            var patientMenu = new Menu
            {
                { "Przepisz lekarstwo", ScheduleDrug }
            };

            patientMenu.Label = GetLabel;
            patientMenu.Role = Person.Roles.Carer;
            patientMenu.Condition = () => SelectedPatient is not null;

            var menu2 = new Menu
            {
                { "Wyświetl pacjentów", ListPatients, null, Person.Roles.Carer },
                // tego typu rzeczy też by chyba były łatwiejsze jakbysmy uzywali recordow
                // poniewaz moglibysmy stworzyc patientmenu z przypisanym pacjentem
                { "Wybierz pacjenta", SelectPatient(patientMenu), Person.Roles.Carer },
                { "Podejmij opiekę nad pacjentem bez opiekuna", ListPatientsForAdoption, Person.Roles.Carer },
                { "Dodaj lekarstwo", AddDrug, null, Person.Roles.Carer },
                { "Zobacz nadchodzące lekarstwa", ListDrugActivities, null, Person.Roles.Patient },
                { "Wyloguj się", Logout, menu1, default, true },
            };

            menu2.Required = true;
            menu2.Label = GetLabel;

            menu1.Add("Zaloguj się", Login, menu2);
            menu1.Add("Zarejestruj pacjenta", Register(Person.Roles.Patient), menu2);
            menu1.Add("Zarejestruj opiekuna", Register(Person.Roles.Carer), menu2, Person.Roles.Admin);

            menu1.Show();
        }

        public static Token Login()
        {
            Console.WriteLine("podaj dane logowania");

            if (!GetInput("email:", out var email) || !GetInput("password:", out var password))
                return default;

            if (Manager.Login(email, password) is Person user)
                User = user.GetPerson();

            if (User != null)
                return Token.Proceed;

            Console.WriteLine("nieprawidłowe dane");
            return Token.Repeat;
        }

        public static string GetLabel()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Zalogowany jako {User?.NameAndRole}");

            if (SelectedPatient is Person selectedPatient)
                sb.AppendLine($"Wybrany pacjent: {selectedPatient.FullName}");

            return sb.ToString();
        }

        public static Func<Token> Register(Person.Roles role)
        {
            return () =>
            {
                Console.WriteLine($"podaj dane nowego {role.GetName()}a");

                Console.WriteLine("email:");
                var email = Console.ReadLine() ?? string.Empty;

                Console.WriteLine("haslo:");
                var password = Console.ReadLine() ?? string.Empty;

                Console.WriteLine("imię:");
                var firstName = Console.ReadLine() ?? string.Empty;

                Console.WriteLine("nazwisko:");
                var lastName = Console.ReadLine() ?? string.Empty;

                var user = new Person
                {
                    Email = email,
                    Password = password,
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                };

                if (Manager.Register(user))
                    return Login();

                Console.WriteLine("nieprawidłowe dane");
                return Token.Repeat;
            };
        }

        public static Func<Token> AdoptPatient(Person patient)
        {
            return () =>
            {
                if (User is Person carer && carer.Role is Person.Roles.Carer)
                    Manager.PatientCarers.Add(new(carer, patient));

                Console.WriteLine($"Podjęto opiekę nad pacjentem {patient.FullName}");
                return Token.Return;
            };
        }

        public static Token Proceed()
        {
            return Token.Proceed;
        }

        public static Menu ListPatientsForAdoption()
        {
            return new Menu(Manager.PatientsForAdoption.Select(p => new MenuItem(p.FullName, AdoptPatient(p))));
        }

        public static Token ScheduleDrug()
        {
            if (User is not Carer carer)
                return default; // to trzeba usprawnic

            if (SelectedPatient is null)
                SelectPatient()?.Invoke();

            if (SelectedPatient is not Patient patient)
                return default;

            Console.WriteLine("Podaj datę przyjęcia lekarstwa");
            var schedule = GetDate();

            Console.WriteLine("Podaj deadline");
            var deadline = schedule + GetTimeSpan();

            Console.WriteLine("Podaj cykliczność");
            var repeat = schedule + GetTimeSpan();

            var drugActivity = carer.ScheduleDrugs(patient, schedule, deadline, repeat);

            while (new ModelMenu<Drug>(Manager.Drugs).ShowAndGetValue() is Drug drug)
                drugActivity.AddDrug(drug, drug.GetAmount());

            return default;
        }

        public static Token ListDrugActivities()
        {
            if (SelectedPatient is not Patient patient)
                return default;

            var drugMenu = ModelMenu.Create(patient.UpcomingDrugs);
            drugMenu.Show();

            return default;
        }

        public static Token ListPatients()
        {
            var patients = User is Carer carer ? carer.Patients : Manager.Patients;

            foreach (var patient in patients)
                Console.WriteLine(patient.FullName);

            return default;
        }

        public static Func<Menu?> SelectPatient(Menu? next = null)
        {
            return () =>
            {
                if (User is not Carer carer)
                    return null;

                var patientItems = carer.Patients.Select(p => (p.FullName, p));
                return new ActionMenu<Patient>(patientItems, patient => SelectedPatient = patient, next)
                {
                    Label = () => "pacjenci"
                };
            };
        }

        public static Token AddDrug()
        {
            Console.WriteLine("Podaj dane lekarstwa");

            // dobrze by bylo dla app zrobic eventy zeby nie tzreba bylo caly czas zwracac default
            // albo jakby bylo async getinput to mozna by bylo sprawdzic czy jakis zwroci false i wtedy return defualt;
            if (!GetInput("Nazwa:", out var name))
                return default;

            var unitMenu = new EnumMenu<Drug.Units>();
            unitMenu.Show(); // tutaj jest potencjalny problem ponieważ jeżeli damy esc to unit bedzie default wiec czasowy workaround to jest dac required na valuemenu

            Manager.Drugs.Add(new Drug { Label = name, Unit = unitMenu.Value });
            Console.WriteLine($"Dodano lekartswo {name}");

            return default;
        }

        public static Token Logout()
        {
            User = null;
            return Token.Return;
        }

        private static readonly object builderLock = new();

        public static bool ReadLine(out string line)
        {
            line = string.Empty;
            var sb = new StringBuilder(1024);

            while (true)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(50);
                    continue;
                }

                // maybe with readkey(true) which in combination with writing the keychar to the console turning out to be better than normal readkey
                // we would be able to make asensible async variant with TAsk.Delay(50) and lock on the stringbuilder 
                var keyInfo = Console.ReadKey(true);

                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        line = sb.ToString();
                        Console.WriteLine();
                        return true;
                    case ConsoleKey.Escape:
                        Console.WriteLine();
                        return false;
                    case ConsoleKey.Backspace:
                        sb.Length = Math.Max(0, sb.Length - 1);
                        Console.Write("\b \b"); // (int)consolekey
                        continue;
                    default:
                        sb.Append(keyInfo.KeyChar);
                        Console.Write(keyInfo.KeyChar);
                        continue;
                }
            }
        }

        public static bool GetInput(string output, out string input)
        {
            Console.WriteLine(output);
            return ReadLine(out input);
        }

        public record Input(bool Ok, string Text)
        { }

        public static Task<Input> ReadLineAsync()
        {
            return Task.Run(() => new Input(ReadLine(out string line), line));
        }

        public static async Task<Input> GetInputAsync(string output)
        {
            await Console.Out.WriteLineAsync(output);
            return await ReadLineAsync();
        }

        public static int? ReadIntLine()
        {
            return ReadLine(out var line) && int.TryParse(line, out var result) ? result : null;
        }

        public static int ReadIntLineOrDefault()
        {
            return ReadIntLine() ?? default;
        }

        public static DateTime GetDate(DateTime? dateTime = null)
        {
            var dtInput = new ConsoleDateTimeInput(dateTime)
            {
                new ConsoleDateTimeInput.DaySegment(),
                new ConsoleDateTimeInput.DateTimeSegment("."),
                new ConsoleDateTimeInput.MonthSegment(),
                new ConsoleDateTimeInput.DateTimeSegment("."),
                new ConsoleDateTimeInput.YearSegment(),
                new ConsoleDateTimeInput.DateTimeSegment(" "),
                new ConsoleDateTimeInput.HourSegment(),
                new ConsoleDateTimeInput.DateTimeSegment(":"),
                new ConsoleDateTimeInput.MinuteSegment(),
            };

            return dtInput.GetDate();
        }
    
        public static TimeSpan GetTimeSpan()
        {
            var items = new List<(string, Func<TimeSpan>)>
            {
                ("Wprowadź liczbę minut", () => TimeSpan.FromMinutes(ReadIntLineOrDefault())),
                ("Wprowadź liczbę godzin", () => TimeSpan.FromHours(ReadIntLineOrDefault())),
                ("Wprowadź liczbę dni", () => TimeSpan.FromDays(ReadIntLineOrDefault())),
                ("Nie ustalaj", () => default),
            };

            return new ActionResultMenu<TimeSpan>(items).GetResult();
        }
    }
}
