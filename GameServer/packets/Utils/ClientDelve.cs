using System.Collections.Generic;
using System.Text;

namespace DOL.GS.PacketHandler
{
    public static class ClientDelve
    {
        public static DelveBuilder Create(string delveType)
        {
            return new(delveType);
        }
    }

    public class DelveBuilder
    {
        private static readonly HashSet<string> _ignoredValues = [string.Empty, "0"];
        private readonly StringBuilder _stringBuilder = new();

        internal DelveBuilder(string delveType)
        {
            _stringBuilder.Append($"({delveType} ");
        }

        public DelveBuilder AddElement(string name, object value)
        {
            if (value == null)
                return this;

            string stringValue = value.ToString();

            if (!_ignoredValues.Contains(stringValue))
                _stringBuilder.Append($"({name} \"{stringValue}\")");

            return this;
        }

        public DelveBuilder AddElement(string name, List<object> collection)
        {
            if (collection != null && collection.Count > 0)
                AddElement(name, string.Join(", ", collection));

            return this;
        }

        public DelveBuilder AddElement(string name, bool flag)
        {
            return AddElement(name, flag ? 1 : 0);
        }

        public DelveBuilder AddElementIf(bool condition, string name, object value)
        {
            if (condition)
                AddElement(name, value);

            return this;
        }

        public DelveBuilder AddElementIf(bool condition, string name, List<object> collection)
        {
            if (condition)
            {
                if (collection != null && collection.Count > 0)
                    AddElement(name, string.Join(", ", collection));
            }

            return this;
        }

        public string Finalize()
        {
            return _stringBuilder.Append(')').ToString();
        }
    }
}
