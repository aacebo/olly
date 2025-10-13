using System.Data;
using System.Text.Json;

using Npgsql;

using NpgsqlTypes;

using OS.Agent.Models;

namespace OS.Agent.Postgres;

public class SourceTypeHandler(JsonSerializerOptions? options = null) : Dapper.SqlMapper.TypeHandler<Tenant.Source>
{
    public override void SetValue(IDbDataParameter parameter, Tenant.Source? value)
    {
        var p = (NpgsqlParameter)parameter;
        p.NpgsqlDbType = NpgsqlDbType.Jsonb;
        p.Value = value is null ? DBNull.Value : JsonSerializer.Serialize(value, options);
    }

    public override Tenant.Source? Parse(object value) => value switch
    {
        string s => JsonSerializer.Deserialize<Tenant.Source>(s),
        JsonElement je => JsonSerializer.Deserialize<Tenant.Source>(je.GetRawText()),
        ReadOnlyMemory<byte> rom => JsonSerializer.Deserialize<Tenant.Source>(rom.ToString()),
        byte[] bytes => JsonSerializer.Deserialize<Tenant.Source>(bytes),
        _ => null
    };
}