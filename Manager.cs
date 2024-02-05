using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Projekt.Manager;

namespace Projekt
{
    internal interface ILabel
    {
        string GetLabel();
    }

    internal abstract class Model : ILabel
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public virtual string GetLabel() => string.Empty;
    }

    internal static class Manager
    {
        public record ScheduledActivity<T>(ActivitySchedule Schedule, T Activity) : ILabel
            where T : Activity
        {
            public string GetLabel() => $"{Schedule.Deadline}";
        }

        public record ScheduledActivity(ActivitySchedule Schedule, Activity Activity) 
            : ScheduledActivity<Activity>(Schedule, Activity);

        public record ScheduledDrugActivity(ActivitySchedule Schedule, DrugActivity Activity) 
            : ScheduledActivity<DrugActivity>(Schedule, Activity)
        {
            public new string GetLabel()
            {
                var sb = new StringBuilder();

                sb.AppendLine("tutaj można znalezc inofrmacje o lekatstwie co nadchodzi");
                sb.AppendLine($"data {Schedule.Deadline}");

                foreach (var drug in Activity.Drugs)
                    sb.AppendLine($"lekarstwo {drug.Drug?.Label} w ilosci {drug.Amount}");

                return sb.ToString();
            }
        }

        public static List<Person> People { get; } = [];
        public static List<Activity> Activities { get; } = [];
        public static List<Drug> Drugs { get; } = [];
        public static List<ActivityDrug> ActivityDrugs { get; } = [];
        public static List<ActivitySchedule> Schedule { get; } = [];
        public static List<PatientCarer> PatientCarers { get; } = [];

        public static IEnumerable<Person> Patients => People.Where(p => p.Role is Person.Roles.Patient);
        public static IEnumerable<Person> Carers => People.Where(p => p.Role is Person.Roles.Carer);
        
        public static IEnumerable<Person> AdoptedPatients => PatientCarers.Select(pc => pc.PatientId.In(Patients))
                                                                   .NotNull()
                                                                   .Distinct();

        public static IEnumerable<Person> PatientsForAdoption => Patients.Where(p => !AdoptedPatients.Contains(p));

        public static IEnumerable<ScheduledActivity<TActivity>> GetScheduledActivities<TActivity>()
            where TActivity : Activity
        {
            return Schedule.Join(Activities.OfType<TActivity>(), s => s.ActivityId, da => da.Id, (a, s) => new ScheduledActivity<TActivity>(a, s));
        }

        public static Person? Login(string email, string password)
        {
            var person = People.FirstOrDefault(p => p.Email == email);

            if (person is null || !VerifyPassword(password, person.Password))
                return null;

            return person;
        }

        public static bool Register(Person person)
        {
            person.Password = HashPassword(person.Password);
            People.Add(person);

            return true;
        }

        public static void Seed()
        {
            var admin = new Person { Role = Person.Roles.Admin, Email = "x", Password = HashPassword("x") };

            var carer1 = new Person
            {
                Role = Person.Roles.Carer,
                FirstName = "Józek",
                LastName = "Bezdomny",
                Email = "c",
                Password = HashPassword("c")
            };

            var patient1 = new Person
            {
                Role = Person.Roles.Patient,
                FirstName = "Staszek",
                LastName = "Skurwiel",
                Email = "p",
                Password = HashPassword("p")
            };

            var drug1 = new Drug { Label = "Viagra", Unit = Drug.Units.Pill };
            var drug2 = new Drug { Label = "Trucizna na słonie", Unit = Drug.Units.Liter };

            var activity1 = new DrugActivity(carer1, patient1);

            var activityDrug1 = new ActivityDrug(activity1, drug1, 150);

            var activityDrug2 = new ActivityDrug(activity1, drug2, 1);

            var activitySchedule1 = new ActivitySchedule
            {
                ActivityId = activity1.Id,
                Schedule = DateTime.Now.AddDays(2),
                Priority = ActivitySchedule.Priorities.Mandatory,
            };

            People.Add(admin);
            People.Add(carer1);
            People.Add(patient1);

            Drugs.Add(drug1);
            Drugs.Add(drug2);

            Activities.Add(activity1);
            ActivityDrugs.Add(activityDrug1);
            ActivityDrugs.Add(activityDrug2);

            Schedule.Add(activitySchedule1);
        }

        public static string HashPassword(string password)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256);
            byte[] salt = deriveBytes.Salt;
            byte[] hash = deriveBytes.GetBytes(32);

            byte[] combinedBytes = new byte[salt.Length + hash.Length];
            Buffer.BlockCopy(salt, 0, combinedBytes, 0, salt.Length);
            Buffer.BlockCopy(hash, 0, combinedBytes, salt.Length, hash.Length);

            return Convert.ToBase64String(combinedBytes);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            byte[] combinedBytes = Convert.FromBase64String(hashedPassword);

            byte[] salt = new byte[16];
            byte[] hash = new byte[combinedBytes.Length - 16];
            Buffer.BlockCopy(combinedBytes, 0, salt, 0, 16);
            Buffer.BlockCopy(combinedBytes, 16, hash, 0, hash.Length);

            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] newHash = deriveBytes.GetBytes(32);

            return StructuralComparisons.StructuralEqualityComparer.Equals(hash, newHash);
        }

        public static bool CheckRole(Person.Roles needed, Person.Roles? had)
        {
            return needed switch
            {
                Person.Roles.LoggedOut => had is null,
                _ when had is null => needed is Person.Roles.None,
                _ => had?.HasFlag(needed) is true,
            };
        }
    }
}
