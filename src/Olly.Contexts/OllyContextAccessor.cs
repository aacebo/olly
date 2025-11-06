namespace Olly.Contexts;

public interface IOllyContextAccessor
{
    OllyContext Context { get; }
}

public class OllyContextAccessor(IServiceProvider provider) : IOllyContextAccessor
{
    private static readonly AsyncLocal<OllyContext?> Async = new();

    public OllyContext Context
    {
        get
        {
            Async.Value ??= new(provider);
            return Async.Value;
        }
        internal set
        {
            Async.Value = value;
        }
    }
}