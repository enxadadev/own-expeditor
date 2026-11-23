using OwnExpeditor.Data.Maps;

namespace OwnExpeditor.Data.Tests.Maps;

/// <summary>FS em memória: caminhos com "/", <c>List</c> devolve caminhos completos dos filhos diretos.</summary>
internal sealed class FakeScsFileSystem : IScsFileSystem
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public Action<string>? OnOpen { get; set; }
    public int OpenCount { get; private set; }

    public FakeScsFileSystem AddSector(int sx, int sz, byte[] data, string map = "europe")
    {
        _files[$"map/{map}/sec{sx.ToString("+0000;-0000")}{sz.ToString("+0000;-0000")}.base"] = data;
        return this;
    }

    public Stream? Open(string path)
    {
        OpenCount++;
        OnOpen?.Invoke(path);
        return _files.TryGetValue(path, out var d) ? new MemoryStream(d, writable: false) : null;
    }

    public IEnumerable<string> List(string dir)
    {
        var prefix = dir.TrimEnd('/') + "/";
        return _files.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .Select(k => k[prefix.Length..].Split('/')[0])
            .Distinct()
            .Select(name => prefix + name)
            .ToList();
    }
}
