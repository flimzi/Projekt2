using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal class ActivityDrug : Model
    {
        public Guid ActivityId { get; init; }
        public Guid DrugId { get; init; }
        public double Amount { get; init; }

        public ActivityDrug()
        { }

        public ActivityDrug(Activity activity, Drug drug, double amount)
        {
            ActivityId = activity.Id;
            DrugId = drug.Id;
            Amount = amount;
        }

        public Drug? Drug => DrugId.In(Manager.Drugs);
    }
}
