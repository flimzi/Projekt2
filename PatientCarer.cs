using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal class PatientCarer : Model
    {
        public Guid CarerId { get; init; }
        public Guid PatientId { get; init; }

        public PatientCarer(Person carer, Person patient)
        {
            CarerId = carer.Id;
            PatientId = patient.Id;
        }
    }
}
