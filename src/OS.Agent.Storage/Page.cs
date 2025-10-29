using System.Text.Json.Serialization;

using SqlKata.Execution;

namespace OS.Agent.Storage;

public class Page
{
    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;

    [JsonPropertyName("size")]
    public int Size { get; set; } = 20;

    [JsonPropertyName("sort")]
    public Sort? Sort { get; set; }

    [JsonPropertyName("where")]
    public IList<Condition> Where { get; set; } = [];

    [JsonIgnore]
    public QueryBuilderFactory? Factory { get; set; }

    public Task<PaginationResult<T>> Invoke<T>(SqlKata.Query query, CancellationToken cancellationToken = default)
    {
        if (Sort is not null)
        {
            query = Sort.Direction == SortDirection.Asc ? query.OrderBy(Sort.Columns) : query.OrderByDesc(Sort.Columns);
        }

        foreach (var condition in Where)
        {
            query = query.Where(condition.Left, condition.Op, condition.Right);
        }

        if (Factory is not null)
        {
            query = Factory(query);
        }

        return query.PaginateAsync<T>(Index + 1, Size, cancellationToken: cancellationToken);
    }

    public static PageBuilder Create() => new();

    public class PageBuilder
    {
        private int _index = 0;
        private int _size = 20;
        private Sort? _sort;
        private QueryBuilderFactory? _factory;
        private readonly IList<Condition> _where = [];

        public PageBuilder Index(int index)
        {
            _index = index;
            return this;
        }

        public PageBuilder Size(int size)
        {
            _size = size;
            return this;
        }

        public PageBuilder Sort(Sort sort)
        {
            _sort = sort;
            return this;
        }

        public PageBuilder Sort(SortDirection direction, params string[] columns)
        {
            _sort = new Sort()
            {
                Columns = columns,
                Direction = direction
            };

            return this;
        }

        public PageBuilder Where(Condition condition)
        {
            _where.Add(condition);
            return this;
        }

        public PageBuilder Where(string left, object right)
        {
            _where.Add(new(left, right));
            return this;
        }

        public PageBuilder Where(string left, string op, object right)
        {
            _where.Add(new(left, op, right));
            return this;
        }

        public PageBuilder Factory(QueryBuilderFactory factory)
        {
            _factory = factory;
            return this;
        }

        public Page Build()
        {
            return new()
            {
                Index = _index,
                Size = _size,
                Sort = _sort,
                Where = _where,
                Factory = _factory
            };
        }
    }
}