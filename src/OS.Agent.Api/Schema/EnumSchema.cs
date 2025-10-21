using System.ComponentModel;

using OS.Agent.Storage.Models;

namespace OS.Agent.Api.Schema;

public class EnumSchema<TEnum> : EnumType<string> where TEnum : StringEnum
{
    protected override void Configure(IEnumTypeDescriptor<string> descriptor)
    {
        var type = typeof(TEnum);

        descriptor.Name(type.Name + "Enum");

        foreach (var property in type.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
        {
            var value = (TEnum?)property.GetValue(null) ?? throw new InvalidEnumArgumentException();
            descriptor.Value(value.Value).Name(property.Name);
        }
    }
}