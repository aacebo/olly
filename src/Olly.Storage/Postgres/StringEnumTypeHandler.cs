using System.Data;
using System.Text.Json;

using Dapper;

using Npgsql;

using NpgsqlTypes;

using Olly.Storage.Models;

namespace Olly.Storage.Postgres;

public sealed class StringEnumTypeHandler<T> : SqlMapper.TypeHandler<T> where T : StringEnum
{
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        var p = (NpgsqlParameter)parameter;
        p.NpgsqlDbType = NpgsqlDbType.Text;
        p.Value = value is null ? DBNull.Value : value.ToString();
    }

    public override T? Parse(object value) => value switch
    {
        string s => JsonSerializer.Deserialize<T>(@$"""{s}"""),
        _ => default
    };
}