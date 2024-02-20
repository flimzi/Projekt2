using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal record SelectItem<T>(string Label, T Value) : ILabel
    {
        public string GetLabel() => Label;
    }

    internal class SelectInput<T>(IEnumerable<T> items) : IEnumerable<T>
        where T : ILabel
    {
        public enum Result { Invalid, Success, Cancel }

        public List<T> Items { get; } = items.ToList();

        public virtual string GetOutput()
        {
            return Items.IndexAggregate(new StringBuilder(), (sb, item, idx) => sb.AppendLine($"[{++idx}] {item.GetLabel()}")).ToString(); 
        }

        public virtual Result GetInput(out int index)
        {
            index = default;

            if (!App.ReadLine(out var input))
                return Result.Cancel;

            if (!int.TryParse(input, out index))
                return Result.Invalid;

            return Result.Success;
        }

        public virtual Result GetItem(out T? item)
        {
            item = default;

            var inputResult = GetInput(out var index);

            if (inputResult is not Result.Success)
                return inputResult;

            if (!Items.TryGetElementAt(index, out item))
                return Result.Invalid;

            return Result.Success;
        }

        public virtual Result ShowAndGetItem(out T? item)
        {
            Console.WriteLine(GetOutput());
            return GetItem(out item);
        }

        public virtual T? ShowAndGetItem()
        {
            ShowAndGetItem(out var item);
            return item;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }

    internal class RepeatingSelectInput<T>(IEnumerable<T> items) : SelectInput<T>(items)
        where T : ILabel
    {
        public bool Repeat { get; init; }

        public override Result GetItem(out T? item)
        {
            Result result;

            do
            {
                result = base.GetItem(out item);
            }
            while (Repeat && result is Result.Invalid);

            return result;
        }
    }

    internal class CancellableSelectInput<T>(IEnumerable<T> items) : RepeatingSelectInput<T>(items)
        where T : ILabel
    {
        public bool Cancel { get; init; }

        public override string GetOutput()
        {
            if (!Cancel)
                return base.GetOutput();

            return base.GetOutput() + Environment.NewLine + $"[0] Anuluj";
        }

        public override Result GetInput(out int index)
        {
            var result = base.GetInput(out index);

            if (!Cancel)
                return result;

            if (base.GetInput(out index) is Result.Invalid && index is 0)
                return Result.Cancel;

            return result;
        }
    }

    internal class MenuInput(IEnumerable<MenuItem> menuItems) : CancellableSelectInput<MenuItem>(menuItems)
    {

    }
}
