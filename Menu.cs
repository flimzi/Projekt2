using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Projekt
{
    internal class Menu : IEnumerable<Menu.MenuItem>
    {
        internal enum Token
        {
            Nothing,
            Repeat,
            Return,
            Proceed,
        }

        internal class MenuItem
        {
            public string Label { get; init; }
            public Person.Roles Role { get; init; }
            public Func<Token> Action { get; init; }
            public Menu? Next { get; init; }
            public bool Clear { get; init; }

            public MenuItem(string label, Func<Token> action, Menu? next = null, Person.Roles role = default, bool clear = false)
            {
                Label = label;
                Role = role;
                Action = action;
                Next = next;
                Clear = clear;
            }

            public MenuItem(string label, Func<Menu?> next, Person.Roles role = default)
                : this(label, ConstructMenu(next), null, role)
            { }

            public void Run()
            {
                switch (Action())
                {
                    case Token.Repeat:
                        Run();
                        break;
                    case Token.Proceed when Next is Menu nextMenu:
                        nextMenu.Show();
                        break;

                }
            }

            public static Func<Token> ConstructMenu(Func<Menu?> next)
            {
                return () =>
                {
                    next()?.Show();
                    return default;
                };
            }
        }

        public List<MenuItem> Items { get; init; } = new();
        public Func<string>? Label { get; set; }
        public Person.Roles Role { get; set; }
        public bool Discard { get; set; }
        public bool Required { get; set; }
        public Func<bool>? Condition { get; set; }

        public Menu()
        { }

        public Menu(IEnumerable<MenuItem> items)
        {
            Items.AddRange(items);
        }

        public void Add(string label, Func<Token> action, Menu? next = null, Person.Roles role = default, bool clear = false)
        {
            Items.Add(new(label, action, next, role, clear));
        }

        public void Add(string label, Func<Menu?> next, Person.Roles role = default)
        {
            Items.Add(new(label, next, role));
        }

        protected Dictionary<int, MenuItem> Build()
        {
            var availableItems = Items.Where(item => Manager.CheckRole(item.Role, App.User?.Role)).ToList();

            return Enumerable.Range(1, availableItems.Count)
                             .Zip(availableItems)
                             .ToDictionary(pair => pair.First, pair => pair.Second);
        }

        protected static string GetSeparator(IEnumerable<MenuItem> items)
        {
            return Enumerable.Repeat("--", items.Select(i => i.Label.Length).Order().FirstOrDefault(15)).Merge();
        }

        protected Menu? Draw()
        {
            var items = Build();
            var separator = GetSeparator(items.Values);

            var menu = items.OrderBy(kvp => kvp.Key)
                            .Select(kvp => $"[{kvp.Key}] {kvp.Value.Label}") // to by moglo byc np item.Name jesli bylby to rekord
                            .DefaultIfEmpty("brak danych")
                            .Merge(Environment.NewLine);


            Console.WriteLine(separator);

            if (Label is not null)
                Console.WriteLine(Label());

            Console.WriteLine("wybierz opcję");
            Console.WriteLine(menu);

            if (!items.Any())
                return null;

            if (!Required)
            {
                var cancelItem = items[0] = new MenuItem("Anuluj", () => Token.Return);
                Console.WriteLine($"[0] {cancelItem.Label}");
            }

            Console.WriteLine(separator);

            return GetItem(items) is MenuItem item ? RunItem(item) : null;
        }

        public MenuItem? GetItem(Dictionary<int, MenuItem> items)
        {
            while (true)
            {
                if (!App.ReadLine(out var input))
                {
                    if (!Required)
                        return null;

                    continue;
                }

                if (!int.TryParse(input, out var index) || !items.TryGetValue(index, out var selectedOption))
                    Console.WriteLine("Niepoprawna opcja");
                else
                    return selectedOption;
            }
        }

        public Menu? RunItem(MenuItem item)
        {
            Token result;

            do
            {
                result = item.Action();

                if (item.Clear)
                    Console.Clear();
            }
            while (result is Token.Repeat);

            if (result is Token.Proceed && item.Next is Menu nextMenu)
                return nextMenu;

            if (result is Token.Return)
                return null;

            return this;
        }
        public void Show()
        {
            var tabStack = new Stack<Menu>();
            tabStack.Push(this);

            while (tabStack.TryPeek(out var menu))
            {
                if (Manager.CheckRole(menu.Role, App.User?.Role) && (menu.Condition is null || menu.Condition()) && menu.Draw() is Menu newMenu)
                {
                    if (newMenu != menu && !newMenu.Discard)
                        tabStack.Push(newMenu);
                }
                else
                    tabStack.Pop();
            }
        }

        public IEnumerator<MenuItem> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }

    internal readonly struct MenuItemOption<T>(string label, T value)
    {
        public string Label { get; } = label;
        public T Value { get; } = value;
    }

    internal class ValueMenu<T> : Menu
    {
        public T? Value { get; protected set; }

        public ValueMenu(IEnumerable<(string, T)> values, Menu? next = null)
        {
            Required = true; // workaround

            foreach (var (label, value) in values)
                Add(label, Select(value), next);
        }

        protected virtual Func<Token> Select(T value)
        {
            return () =>
            {
                Value = value;
                return Token.Return;
            };
        }

        public T? ShowAndGetValue()
        {
            Show(); // to powinno zwracac chyba bool xd
            return Value;
        }
    }

    internal class ModelMenu<T> : ValueMenu<T>
        where T : ILabel
    {
        public ModelMenu(IEnumerable<T> models, Menu? next = null)
            : base(models.Select(m => (m.GetLabel(), m)), next)
        { }
    }

    internal static class ModelMenu
    {
        public static ModelMenu<T> Create<T>(IEnumerable<T> models)
            where T : ILabel
        {
            return new ModelMenu<T>(models);
        }
    }

    internal class EnumMenu<T> : ValueMenu<T>
        where T : Enum
    {
        public EnumMenu()
            : base(Enum.GetValues(typeof(T)).OfType<T>().Select(e => (e.GetName(), e)))
        { }
    }

    internal class ActionMenu<T> : ValueMenu<T>
    {
        protected Action<T> Action { get; }

        public ActionMenu(IEnumerable<(string, T)> values, Action<T> action, Menu? next = null)
            : base(values, next)
        {
            Action = action;
        }

        protected override Func<Token> Select(T value)
        {
            return () =>
            {
                Action(value);
                return base.Select(value)();
            };
        }
    }

    internal class ActionResultMenu<T> : ValueMenu<Func<T>>
    {
        public ActionResultMenu(IEnumerable<(string, Func<T>)> values, Menu? next = null)
            : base(values, next)
        { }

        public T? GetResult()
        {
            return ShowAndGetValue() is Func<T> func ? func() : default;
        }
    }

    internal class YesNoCancelMenu(bool required = false) : ValueMenu<YesNoCancelMenu.Result>(GetItems(required))
    {
        public enum Result { Yes, No, Cancel }

        protected static IEnumerable<(string, Result)> GetItems(bool required = false)
        {
            yield return new("tak", Result.Yes);
            yield return new("nie", Result.No);

            if (!required)
                yield return new("anuluj", Result.Cancel);
        }

        public static bool Yes()
        {
            return new YesNoCancelMenu().ShowAndGetValue() is Result.Yes;
        }
    }
}
