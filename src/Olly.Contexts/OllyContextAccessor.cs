namespace Olly.Contexts;

public interface IOllyContextAccessor
{
    OllyContext Context { get; }
}

public class OllyContextAccessor : IOllyContextAccessor
{
    private static readonly AsyncLocal<OllyContext?> Async = new();

    public OllyContext Context
    {
        get
        {
            Async.Value ??= new();
            return Async.Value;
        }
        internal set
        {
            Async.Value = value;
        }
    }
}