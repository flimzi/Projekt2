using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal class ActivitySchedule : Model
    {
        internal enum Priorities
        {
            None,
            Mandatory,
            Low,
            Normal,
            High,
        }

        public Guid ActivityId { get; init; }
        public DateTime Schedule { get; init; } // to jest absolute
        public DateTime? Deadline { get; init; } // to tez (chociaz to jest obojetne)
        public DateTime? Repeat { get; init; } // ale to jest delta (przy ukonczeniu lub przejsciu deadline tworzymy nowy) (datetimeoffset)
        public string Note { get; init; } = string.Empty;
        public bool Completed { get; init; }
        public Priorities Priority { get; init; }

        public ActivitySchedule(Activity activity)
        {
            ActivityId = activity.Id;
        }

        public ActivitySchedule()
        { }

        public Manager.ScheduledActivity? GetScheduledActivity()
        {
            if (ActivityId.In(Manager.Activities) is Activity activity)
                return new(this, activity);

            return null;
        }
    }
}
