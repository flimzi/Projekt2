using Projekt;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Projekt
{
    internal static class GuidExtensions
    {
        public static TModel? In<TModel>(this Guid id, IEnumerable<TModel> table)
            where TModel : Model
        {
            return table.Find(id);
        }

        public static bool NotIn<TModel>(this Guid id, IEnumerable<TModel> table)
            where TModel : Model
        {
            return table.Find(id) is null;
        }
    }

    internal static class EnumerableExtensions
    {
        public static TModel? Find<TModel>(this IEnumerable<TModel> table, Guid id)
            where TModel : Model
        {
            return table.FirstOrDefault(m => m.Id == id);
        }

        public static IEnumerable<TModel> Intersect<TModel, TOther>(this IEnumerable<TModel> table, IEnumerable<TOther> other, Func<TOther, Guid> keySelector)
            where TModel : Model
            where TOther : Model
        {
            var keys = other.Select(keySelector);

            return table.Where(t => keys.Contains(t.Id));
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> enumerable)
        {
            return enumerable.OfType<T>();
        }

        public static string Merge(this IEnumerable<char> enumerable, string separator = "")
        {
            return string.Join(separator, enumerable);
        }

        public static string Merge(this IEnumerable<string> enumerable, string separator = "")
        {
            return string.Join(separator, enumerable);
        }

        public static IEnumerable<T> Invoke<T>(this IEnumerable<Func<T>> enumerable)
        {
            return enumerable.Select(f => f());
        }

        public static IEnumerable<T> Before<T>(this IEnumerable<T> enumerable, T value)
        {
            foreach (var item in enumerable)
            {
                if (item?.Equals(value) is true)
                    yield break;

                yield return item;
            }
        }

        public static IEnumerable<T> After<T>(this IEnumerable<T> enumerable, T value)
        {
            foreach (var item in enumerable)
            {
                if (item?.Equals(value) is true)
                    continue;

                yield return item;
            }
        }

        public static TAccumulate IndexAggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, int, TAccumulate> func)
        {
            return source.Select((item, idx) => (item, idx)).Aggregate(seed, (a, b) => func(a, b.item, b.idx));
        }

        public static bool TryGetElementAt<T>(this IEnumerable<T> source, int index, out T? element)
        {
            element = default;

            if (source.ElementAtOrDefault(index) is not T selectedElement)
                return false;

            element = selectedElement;
            return true;
        }
    }

    internal static class ObjectExtensions
    {
        internal static T Or<T>(this T value, T defaultValue)
        {
            return value?.Equals(default(T)) is true ? defaultValue : value;
        }
    }

    internal static class HelperExtensions
    {
        public static bool IsOneOf<T>(this T value, params T[] values)
        {
            return values.Contains(value);
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal class ValueAttribute<TValue> : Attribute
    {
        public TValue Value { get; }

        public ValueAttribute(TValue value)
        {
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class NameAttribute : ValueAttribute<string>
    {
        public NameAttribute(string name)
            : base(name)
        { }
    }

    internal static class EnumExtensions
    {
        public static TValue? GetAttribute<TAttribute, TValue>(this Enum @enum, TValue? defaultValue = default)
            where TAttribute : ValueAttribute<TValue>
        {
            if (@enum.GetType().GetField(@enum.ToString()) is System.Reflection.FieldInfo field)
                if (Attribute.GetCustomAttribute(field, typeof(TAttribute)) is TAttribute customAttribute)
                    return customAttribute.Value;

            return defaultValue;
        }

        public static string GetAttribute<TAttribute>(this Enum @enum, string defaultValue = "")
            where TAttribute : ValueAttribute<string>
        {
            return @enum.GetAttribute<TAttribute, string>() ?? defaultValue;
        }

        public static TValue? GetValue<TValue>(this Enum @enum, TValue? defaultValue = default)
        {
            return @enum.GetAttribute<ValueAttribute<TValue>, TValue>(defaultValue);
        }

        public static string GetName(this Enum @enum)
        {
            return @enum.GetAttribute<NameAttribute>("");
        }
    }

    internal static class PersonExtensions
    {
        public static Person GetPerson(this Person person)
        {
            return person.Role switch
            {
                Person.Roles.Carer => new Carer(person),
                _ => new Person(person)
            };
        }
    }

    internal class CursorLinkedList<T> : LinkedList<T>
    {
        public LinkedListNode<T>? Cursor { get; protected set; }

        public new LinkedListNode<T> AddLast(T value)
        {
            var node = base.AddLast(value);

            Cursor ??= node;
            return node;
        }

        //public LinkedListNode<T> MoveCursorTo(LinkedListNode<T> node)
        //{
        //    // tutaj mozna przeniesc kursor
        //}

        public LinkedListNode<T>? MoveCursorLeft()
        {
            if (Cursor?.Previous is LinkedListNode<T> previousNode)
                Cursor = previousNode;

            return Cursor;
        }

        public bool MoveCursorLeft(out LinkedListNode<T>? cursor)
        {
            //var current = Cursor;
            //cursor = MoveCursorLeft();
            //return cursor != current;
        
            return Cursor != (cursor = MoveCursorLeft());
        }

        public LinkedListNode<T>? MoveCursorLeftTo(Func<T, bool> predicate)
        {
            var current = Cursor;

            while (MoveCursorLeft(out var cursor) && cursor is not null)
            {
                if (predicate(cursor.Value))
                    return cursor;
            }

            return Cursor = current;
        }

        public bool MoveCursorLeftTo(Func<T, bool> predicate, out LinkedListNode<T>? cursor)
        {
            return Cursor != (cursor = MoveCursorLeftTo(predicate));
        }

        public LinkedListNode<T>? MoveCursorRight()
        {
            if (Cursor?.Next is LinkedListNode<T> nextsNode)
                Cursor = nextsNode;

            return Cursor;
        }

        public bool MoveCursorRight(out LinkedListNode<T>? cursor)
        {
            return Cursor != (cursor = MoveCursorRight());
        }

        public LinkedListNode<T>? MoveCursorRightTo(Func<T, bool> predicate)
        {
            var current = Cursor;

            while (MoveCursorRight(out var cursor) && cursor is not null)
            {
                if (predicate(cursor.Value))
                    return cursor;
            }

            return Cursor = current;
        }

        public bool MoveCursorRightTo(Func<T, bool> predicate, out LinkedListNode<T>? cursor)
        {
            return Cursor != (cursor = MoveCursorRightTo(predicate));
        }

        public static IEnumerable<LinkedListNode<T>> NodesBeforeCursor(LinkedListNode<T>? cursor, bool includeCurrent = false)
        {
            var currentCursor = includeCurrent ? cursor : cursor?.Previous;

            while (currentCursor is LinkedListNode<T> previousCursor)
            {
                yield return previousCursor;
                currentCursor = previousCursor.Previous;
            }
        }

        public static IEnumerable<T> BeforeCursor(LinkedListNode<T>? cursor, bool includeCurrent = false)
        {
            return NodesBeforeCursor(cursor, includeCurrent)?.Select(n => n.Value) ?? Enumerable.Empty<T>();
        }

        public IEnumerable<T> BeforeCursor(bool includeCurrent = false)
        {
            return BeforeCursor(Cursor, includeCurrent);
        }

        public static IEnumerable<LinkedListNode<T>> NodesAfterCursor(LinkedListNode<T>? cursor, bool includeCurrent = false)
        {
            var currentCursor = includeCurrent ? cursor : cursor?.Next;

            while (currentCursor is LinkedListNode<T> nextCursor)
            {
                yield return nextCursor;
                currentCursor = nextCursor.Next;
            }
        }

        public static IEnumerable<T> AfterCursor(LinkedListNode<T>? cursor, bool includeCurrent = false)
        {
            return NodesAfterCursor(cursor, includeCurrent)?.Select(n => n.Value) ?? Enumerable.Empty<T>();
        }

        public IEnumerable<T> AfterCursor(bool includeCurrent = false)
        {
            return AfterCursor(Cursor, includeCurrent);
        }

        public IEnumerable<LinkedListNode<T>> Nodes => NodesAfterCursor(First, true);
    }

    internal static class LinkedListNodeExtensions
    {
        public static bool IsCurrent<T>(this LinkedListNode<T> node)
        {
            return node.List is CursorLinkedList<T> cursorLinkedList && cursorLinkedList.Cursor == node;
        }
    }

    internal static class DateTimeExtensions
    {
        public static double GetTotalSeconds(this DateTime dt)
        {
            return (dt - DateTime.MinValue).TotalSeconds;
        }
    }
}
