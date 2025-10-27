using System.Text.Json.Serialization;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public class Query
{
    [JsonPropertyName("sort")]
    public Sort? Sort { get; set; }

    [JsonPropertyName("where")]
    public IList<Condition> Where { get; set; } = [];

    public Task<IEnumerable<T>> Invoke<T>(SqlKata.Query query, CancellationToken cancellationToken = default)
    {
        if (Sort is not null)
        {
            query = Sort.Direction == SortDirection.Asc ? query.OrderBy(Sort.Columns) : query.OrderByDesc(Sort.Columns);
        }

        foreach (var condition in Where)
        {
            query = query.Where(condition.Left, condition.Op, condition.Right);
        }

        return query.GetAsync<T>(cancellationToken: cancellationToken);
    }

    public static QueryBuilder Create() => new();

    public class QueryBuilder
    {
        private Sort? _sort;
        private readonly IList<Condition> _where = [];

        public QueryBuilder Sort(Sort sort)
        {
            _sort = sort;
            return this;
        }

        public QueryBuilder Sort(SortDirection direction, params string[] columns)
        {
            _sort = new Sort()
            {
                Columns = columns,
                Direction = direction
            };

            return this;
        }

        public QueryBuilder Where(Condition condition)
        {
            _where.Add(condition);
            return this;
        }

        public QueryBuilder Where(string left, object right)
        {
            _where.Add(new(left, right));
            return this;
        }

        public QueryBuilder Where(string left, string op, object right)
        {
            _where.Add(new(left, op, right));
            return this;
        }

        public Query Build()
        {
            return new()
            {
                Sort = _sort,
                Where = _where
            };
        }
    }
}

public class Sort
{
    [JsonPropertyName("by")]
    public string[] Columns { get; set; } = [];

    [JsonPropertyName("direction")]
    public SortDirection Direction { get; set; } = SortDirection.Desc;

    public static SortBuilder Create(params string[] columns) => new(columns);

    public class SortBuilder(params string[] columns)
    {
        private readonly IList<string> _columns = columns;
        private SortDirection _direction = SortDirection.Asc;

        public SortBuilder Columns(params string[] columns)
        {
            foreach (var column in columns)
            {
                if (!_columns.Contains(column)) continue;
                _columns.Add(column);
            }

            return this;
        }

        public SortBuilder Direction(SortDirection direction)
        {
            _direction = direction;
            return this;
        }

        public Sort Build()
        {
            return new()
            {
                Columns = [.. _columns],
                Direction = _direction
            };
        }
    }
}

public enum SortDirection
{
    [JsonStringEnumMemberName("asc")]
    Asc,

    [JsonStringEnumMemberName("desc")]
    Desc
}

public class Condition
{
    public string Left { get; }
    public string Op { get; }
    public object Right { get; }

    public Condition(string left, object right)
    {
        Left = left;
        Op = "=";
        Right = right;
    }

    public Condition(string left, string op, object right)
    {
        Left = left;
        Op = op;
        Right = right;
    }
}