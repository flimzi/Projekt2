using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal class Drug : Model
    {
        internal enum Units
        {
            [Name("brak")]
            None,
            [Name("gram")]
            Gram,
            [Name("litr")]
            Liter,
            [Name("pigułka")]
            Pill
        }

        public Units Unit { get; init; }
        public string Label { get; init; } = string.Empty;

        public override string GetLabel()
        {
            return Label;
        }

        public double GetAmount()
        {
            // get amount based on unit
            return default;
        }

        // instructions? also there has to probably be another structure for units of drug
        // because the same drug can be in pills or powder or liquid etc
    }
}
