using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal class Person : Model
    {
        [Flags]
        internal enum Roles
        {
            None,
            [Name("Użytkownik")]
            User = 1 << 0,
            [Name("Pacjent")]
            Patient = User | 1 << 1,
            [Name("Opiekun")]
            Carer = User | 1 << 2,
            [Name("Administrator")]
            Admin = Patient | Carer,
            LoggedOut = 420,
        }

        public Roles Role { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public virtual string FullName => $"{FirstName} {LastName}";
        public virtual string NameAndRole => $"{FullName} ({Role.GetName()})";

        public Person()
        { }

        public Person(Person person)
        {
            Id = person.Id;
            Role = person.Role;
            FirstName = person.FirstName;
            LastName = person.LastName;
            Email = person.Email;
            Password = person.Password;
        }
    }

    internal class Carer : Person
    {
        public Carer()
        {
            Role = Roles.Carer;
        }

        public Carer(Person person)
            : base(person)
        {
            Role = Roles.Carer;
        }

        public DrugActivity ScheduleDrugs(Patient patient, DateTime schedule, DateTime? deadline = null, DateTime? repeat = null)
        {
            var activity = new DrugActivity(this, patient);
            activity.Schedule(schedule, deadline, repeat);

            Manager.Activities.Add(activity);
            return activity;
        }

        public IEnumerable<Patient> Patients =>
            Manager.PatientCarers.Where(pc => Id == pc.CarerId)
                                 .Select(pc => pc.PatientId.In(Manager.People))
                                 .NotNull()
                                 .Where(p => p.Role is Roles.Patient)
                                 .Select(p => new Patient(p));
    }

    internal class Patient : Person
    {

        public Patient()
        {
            Role = Roles.Patient;
        }

        public Patient(Person person)
            : base(person)
        {
            Role = Roles.Patient;
        }

        public IEnumerable<Carer> Carers =>
            Manager.PatientCarers.Where(pc => Id == pc.PatientId)
                                 .Select(pc => pc.CarerId.In(Manager.People))
                                 .NotNull()
                                 .Where(p => p.Role is Roles.Carer)
                                 .Select(p => new Carer(p));

        public IEnumerable<Activity> Activities => Manager.Activities.Where(a => a.ReceiverId == Id);

        public IEnumerable<Manager.ScheduledActivity<DrugActivity>> UpcomingDrugs
            => Manager.GetScheduledActivities<DrugActivity>().Where(sa => sa.Activity.ReceiverId == Id);
    }
}
