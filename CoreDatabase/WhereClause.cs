using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DOL.Database
{
    public abstract class WhereClause
    {
        private string _parameterizedText;
        private QueryParameter[] _parameters;

        internal abstract List<IAtom> IntermediateRepresentation { get; }
        public static WhereClause Empty => EmptyWhereClause.Instance;

        public virtual string ParameterizedText
        {
            get
            {
                EnsureProcessed();
                return _parameterizedText;
            }
        }

        public virtual QueryParameter[] Parameters
        {
            get
            {
                EnsureProcessed();
                return _parameters;
            }
        }

        private void EnsureProcessed()
        {
            if (_parameterizedText != null)
                return;

            List<IAtom> representation = IntermediateRepresentation;

            if (representation.Count == 0)
            {
                _parameterizedText = string.Empty;
                _parameters = [];
                return;
            }

            QueryBuilder builder = new();
            builder.AddSqlText("WHERE");

            foreach (IAtom atom in representation)
                atom.Process(builder);

            _parameterizedText = builder.ParameterizedText;
            _parameters = builder.Parameters;
        }

        public virtual WhereClause And(WhereClause rightExpression)
        {
            return rightExpression.Equals(Empty) ? this : new ChainingExpression(this, "AND", rightExpression);
        }

        public virtual WhereClause Or(WhereClause rightExpression)
        {
            return rightExpression.Equals(Empty) ? this : new ChainingExpression(this, "OR", rightExpression);
        }

        public virtual WhereClause OrderBy(DBColumn column, bool descending = false, int limit = 0)
        {
            return new OrderLimitExpression(this, column, descending, limit);
        }
    }

    internal interface IAtom
    {
        void Process(IQueryBuilder builder);
    }

    internal class TextAtom : IAtom
    {
        private readonly string _val;

        public TextAtom(string val)
        {
            _val = val;
        }

        public void Process(IQueryBuilder builder)
        {
            builder.AddSqlText(_val);
        }
    }

    internal class ValueAtom<T> : IAtom
    {
        private readonly T _val;

        public ValueAtom(T val)
        {
            _val = val;
        }

        public void Process(IQueryBuilder builder)
        {
            builder.AddParameter(_val);
        }
    }

    internal sealed class EmptyWhereClause : WhereClause
    {
        public static readonly EmptyWhereClause Instance = new();

        internal override List<IAtom> IntermediateRepresentation { get; } = new();
        public override string ParameterizedText => string.Empty;

        private EmptyWhereClause() { }

        public override WhereClause And(WhereClause rightExpression)
        {
            return rightExpression;
        }

        public override WhereClause Or(WhereClause rightExpression)
        {
            return rightExpression;
        }

        public override bool Equals(object obj)
        {
            return obj is EmptyWhereClause;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal class FilterExpression<T> : WhereClause
    {
        private readonly string _columnName;
        private readonly string _op;
        private readonly T _val;

        internal override List<IAtom> IntermediateRepresentation =>
        [
            new TextAtom(_columnName),
            new TextAtom(_op),
            new ValueAtom<T>(_val)
        ];

        internal FilterExpression(string columnName, string op, T val)
        {
            _columnName = columnName;
            _op = op;
            _val = val;
        }

        public override bool Equals(object obj)
        {
            if (obj is not FilterExpression<T> other)
                return false;

            return other._op == _op &&
                other._columnName == _columnName &&
                EqualityComparer<T>.Default.Equals(other._val, _val);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal class PlainTextExpression : WhereClause
    {
        private readonly string _columnName;
        private readonly string _op;

        internal override List<IAtom> IntermediateRepresentation => [new TextAtom(_columnName), new TextAtom(_op)];

        internal PlainTextExpression(string columnName, string op)
        {
            _columnName = columnName;
            _op = op;
        }

        public override bool Equals(object obj)
        {
            if (obj is not PlainTextExpression other)
                return false;

            return other._op.Equals(_op) && other._columnName.Equals(_columnName);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal class InExpression<T> : WhereClause
    {
        private readonly string _columnName;
        private readonly IEnumerable<T> _val;
        private List<T> _valueList;

        internal InExpression(string columnName, IEnumerable<T> values)
        {
            _columnName = columnName;
            _val = values ?? [];
        }

        internal override List<IAtom> IntermediateRepresentation
        {
            get
            {
                _valueList ??= _val.ToList();

                if (_valueList.Count == 0)
                    return [new TextAtom("1=0")];

                List<IAtom> result =
                [
                    new TextAtom(_columnName),
                    new TextAtom("IN"),
                    new TextAtom("(")
                ];

                bool isFirst = true;

                foreach (T element in _valueList)
                {
                    if (!isFirst)
                        result.Add(new TextAtom(","));

                    result.Add(new ValueAtom<T>(element));
                    isFirst = false;
                }

                result.Add(new TextAtom(")"));
                return result;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is not InExpression<T> other)
                return false;

            return other._columnName == _columnName && other._val.SequenceEqual(_val);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal class ChainingExpression : WhereClause
    {
        private readonly WhereClause _left;
        private readonly WhereClause _right;
        private readonly string _chainingOperator;

        internal override List<IAtom> IntermediateRepresentation
        {
            get
            {
                if (_right.Equals(Empty))
                    return _left.IntermediateRepresentation;

                List<IAtom> result =
                [
                    new TextAtom("("),
                    .. _left.IntermediateRepresentation,
                    new TextAtom(_chainingOperator),
                    .. _right.IntermediateRepresentation,
                    new TextAtom(")"),
                ];

                return result;
            }
        }

        internal ChainingExpression(WhereClause left, string chainingOperator, WhereClause right)
        {
            _left = left;
            _right = right;
            _chainingOperator = chainingOperator;
        }

        public override bool Equals(object obj)
        {
            if (obj is not ChainingExpression other)
                return false;

            return other._left.Equals(_left) &&
                other._chainingOperator.Equals(_chainingOperator) &&
                other._right.Equals(_right);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal class OrderLimitExpression : WhereClause
    {
        private readonly WhereClause _left;
        private readonly DBColumn _column;
        private readonly bool _descending;
        private readonly int _limit;

        internal override List<IAtom> IntermediateRepresentation
        {
            get
            {
                List<IAtom> result = [.. _left.IntermediateRepresentation, new TextAtom("ORDER BY"), new TextAtom(_column.Name)];

                if (_descending)
                    result.Add(new TextAtom("DESC"));

                if (_limit > 0)
                {
                    result.Add(new TextAtom("LIMIT"));
                    result.Add(new ValueAtom<int>(_limit));
                }

                return result;
            }
        }

        internal OrderLimitExpression(WhereClause left, DBColumn column, bool descending, int limit)
        {
            _left = left;
            _column = column;
            _descending = descending;
            _limit = limit;
        }

        public override bool Equals(object obj)
        {
            if (obj is not OrderLimitExpression other)
                return false;

            return other._left.Equals(_left) &&
                other._column.Equals(_column) &&
                other._descending.Equals(_descending) &&
                other._limit.Equals(_limit);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    public class DB
    {
        public static DBColumn Column(string columnName)
        {
            return new DBColumn(columnName);
        }
    }

    public class DBColumn
    {
        public string Name { get; }

        internal DBColumn(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                throw new ArgumentException("ColumnName may not be null or empty.");

            Name = columnName;
        }

        public WhereClause IsEqualTo<T>(T val)
        {
            return new FilterExpression<T>(Name, "=", val);
        }

        public WhereClause IsNotEqualTo<T>(T val)
        {
            return new FilterExpression<T>(Name, "!=", val);
        }

        public WhereClause IsGreaterThan<T>(T val)
        {
            return new FilterExpression<T>(Name, ">", val);
        }

        public WhereClause IsGreaterOrEqualTo<T>(T val)
        {
            return new FilterExpression<T>(Name, ">=", val);
        }

        public WhereClause IsLessThan<T>(T val)
        {
            return new FilterExpression<T>(Name, "<", val);
        }

        public WhereClause IsLessOrEqualTo<T>(T val)
        {
            return new FilterExpression<T>(Name, "<=", val);
        }

        public WhereClause IsLike<T>(T val)
        {
            return new FilterExpression<T>(Name, "LIKE", val);
        }

        public WhereClause IsNull()
        {
            return new PlainTextExpression(Name, "IS NULL");
        }

        public WhereClause IsNotNull()
        {
            return new PlainTextExpression(Name, "IS NOT NULL");
        }

        public WhereClause IsIn<T>(IEnumerable<T> values)
        {
            return new InExpression<T>(Name, values);
        }
    }

    internal interface IQueryBuilder
    {
        void AddSqlText(string text);
        void AddParameter<T>(T value);
    }

    internal class QueryBuilder : IQueryBuilder
    {
        private const string ALPHABET = "abcdefghijklmnopqrstuvwxyz";
        private readonly StringBuilder _clauseBuilder = new();
        private readonly List<QueryParameter> _parameters = new();
        private int _parameterID = 0;
        private bool _needsSpace = false;

        public string ParameterizedText => _clauseBuilder.ToString();
        public QueryParameter[] Parameters => _parameters.ToArray();

        public void AddSqlText(string text)
        {
            AppendSpaceIfNeeded();
            _clauseBuilder.Append(text);
            _needsSpace = true;
        }

        public void AddParameter<T>(T value)
        {
            AppendSpaceIfNeeded();
            string placeholder = GetPlaceholder(_parameterID++);
            _clauseBuilder.Append(placeholder);
            _parameters.Add(new QueryParameter(placeholder, value));
            _needsSpace = true;
        }

        private void AppendSpaceIfNeeded()
        {
            if (_needsSpace)
                _clauseBuilder.Append(' ');
        }

        private static string GetPlaceholder(int id)
        {
            int radix = ALPHABET.Length;

            if (id < radix)
                return "@" + ALPHABET[id];

            StringBuilder placeholderBuilder = new(4);

            do
            {
                placeholderBuilder.Insert(0, ALPHABET[id % radix]);
                id /= radix;
            } while (id > 0);

            placeholderBuilder.Insert(0, '@');
            return placeholderBuilder.ToString();
        }
    }
}
