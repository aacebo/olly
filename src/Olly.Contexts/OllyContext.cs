namespace Olly.Contexts;

public class OllyContext(IServiceProvider provider)
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString();
    public IServiceProvider Provider { get; set; } = provider;
}