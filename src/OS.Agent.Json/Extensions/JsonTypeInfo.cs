using System.Text.Json.Serialization.Metadata;

namespace OS.Agent.Json.Extensions;

public static class JsonTypeInfoExtensions
{
    public static bool HasDerivedType(this JsonTypeInfo info, Type type)
    {
        return info.PolymorphismOptions is not null && info.PolymorphismOptions.DerivedTypes
            .Any(derivedType => derivedType.DerivedType == type);
    }

    public static int IndexOfDerivedType(this JsonTypeInfo info, Type type)
    {
        if (info.PolymorphismOptions is null)
        {
            return -1;
        }

        return info.PolymorphismOptions.DerivedTypes
            .ToList()
            .FindIndex(derivedType => derivedType.DerivedType == type);
    }

    public static void AddUniqueDerivedType(this JsonTypeInfo info, Type type, string typeDiscriminator)
    {
        if (info.HasDerivedType(type)) return;
        info.PolymorphismOptions?.DerivedTypes.Add(new(type, typeDiscriminator));
    }
}