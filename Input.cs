using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    // wrapper dla roznego rodzaju inputow pozwalacjacy okreslic czy byl invalid success czy cancel
    // mozna bedzie np zrobic logininput
    // potem dostosowac selectinput zeby inheritowal z tej klasy
    // bedzie mozna ustawic cancellable i repeat, menuitem bedzie mogl wtedy wziac jakis input, przetworzyc go i bedzie mozna ustawic .then(menuitem) lub cos w tym stylu
    internal abstract class Input<T>(T? value = default)
    {
        public enum Result { Invalid, Success, Cancel }
        protected record InputResult(Result Result, T? Value);

        public T? Value { get; protected set; } = value;
        public Result State { get; protected set; }
        public string? Label { get; init; }
        public bool Required { get; init; }
        public bool Hidden { get; init; }
        public bool Repeat { get; init; }

        protected abstract InputResult HandleInput();

        protected virtual void HandleOutput()
        {
            if (Label is not null)
                Console.WriteLine(Label);
        }

        public Input<T> Get()
        {
            HandleOutput();

            do
            {
                (State, Value) = HandleInput();
            }
            while (Repeat && State is Result.Invalid);

            return this;
        }

        protected static InputResult Invalid() => new(Result.Invalid, default);
        protected static InputResult Cancel() => new(Result.Invalid, default);
        protected static InputResult Success(T value) => new(Result.Success, value);

        public bool IsOk => State is Result.Success;
        public T ValueNotNull => Value ?? throw new ArgumentNullException(nameof(Value));
    }

    internal class LineInput : Input<string>
    {
        protected override InputResult HandleInput()
        {
            return App.ReadLine(out string line, Required, Hidden) ? Success(line) : Cancel();
        }
    }

    internal class IntegerInput : Input<int>
    {
        protected override InputResult HandleInput()
        {
            if (!App.ReadLine(out string line, Required))
                return Cancel();

            if (!int.TryParse(line, out var value))
                return Invalid();

            return Success(value);
        }
    }

    internal class DoubleInput : Input<double>
    {
        protected override InputResult HandleInput()
        {
            if (!App.ReadLine(out string line, Required))
                return Cancel();

            if (!double.TryParse(line.Replace(',', '.'), out var value))
                return Invalid();

            return Success(value);
        }
    }

    internal class LoginInput : Input<(string Email, string Password)>
    {
        protected override InputResult HandleInput()
        {
            var emailInput = new LineInput { Label = "email:" };
            var passwordInput = new LineInput { Label = "password:", Hidden = true };

            if (!emailInput.Get().IsOk || !passwordInput.Get().IsOk)
                return Cancel();

            return Success((emailInput.ValueNotNull, passwordInput.ValueNotNull));
        }
    }
}
