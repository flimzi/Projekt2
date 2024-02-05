using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal class Activity : Model
    {
        internal enum Types
        {
            Regular,
            Drug,
        }

        public Types Type { get; init; }
        public Guid GiverId { get; init; }
        public Guid ReceiverId { get; init; }

        public Activity(Person giver, Person receiver, Types type)
        {
            GiverId = giver.Id;
            ReceiverId = receiver.Id;
            Type = type;
        }

        public ActivitySchedule Schedule(DateTime schedule, DateTime? deadline = null, DateTime? repeat = null, string note = "", ActivitySchedule.Priorities priority = default)
        {
            var activitySchedule = new ActivitySchedule(this)
            {
                Schedule = schedule,
                Deadline = deadline,
                Repeat = repeat,
                Note = note,
                Priority = priority,
            };

            Manager.Schedule.Add(activitySchedule);
            return activitySchedule;
        }

        public IEnumerable<Manager.ScheduledActivity> ScheduledActivities
            => Manager.Schedule.Where(s => s.ActivityId == Id)
                               .Select(s => new Manager.ScheduledActivity(s, this));
    }

    internal class DrugActivity : Activity
    {
        public DrugActivity(Person giver, Person receiver)
            : base(giver, receiver, Types.Drug)
        { }

        public void AddDrug(Drug drug, double amount)
        {
            Manager.ActivityDrugs.Add(new(this, drug, amount));
        }

        public IEnumerable<ActivityDrug> Drugs => Manager.ActivityDrugs.Where(ad => ad.ActivityId == Id);
    }
}
