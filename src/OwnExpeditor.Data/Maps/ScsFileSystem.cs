using OwnExpeditor.Data.Scs;

namespace OwnExpeditor.Data.Maps;

/// <summary>
/// Sistema de arquivos em camadas sobre pacotes HashFS v2 da instalação: <c>base*.scs</c>, <c>def.scs</c> e <c>dlc_*.scs</c>
/// (prioridade crescente nessa ordem; dentro de cada grupo, ordem ordinal do nome). Mods ainda não são lidos (fase 1b).
/// </summary>
internal sealed class ScsFileSystem : IScsFileSystem, IDisposable
{
    private readonly List<HashFsArchive> _layers;   // maior prioridade primeiro

    private ScsFileSystem(List<HashFsArchive> layers) => _layers = layers;

    /// <summary>Pacotes que formam o mapa, em ordem de prioridade crescente (usado também pela chave do cache).</summary>
    public static IReadOnlyList<string> PackageFiles(string installDir)
    {
        var files = new List<string>();
        files.AddRange(Directory.EnumerateFiles(installDir, "base*.scs").Order(StringComparer.Ordinal));
        var def = Path.Combine(installDir, "def.scs");
        if (File.Exists(def)) files.Add(def);
        files.AddRange(Directory.EnumerateFiles(installDir, "dlc_*.scs").Order(StringComparer.Ordinal));
        return files;
    }

    /// <exception cref="NotSupportedException">Algum pacote não é HashFS v2.</exception>
    /// <exception cref="InvalidDataException">Algum pacote está corrompido.</exception>
    public static ScsFileSystem Create(MapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var layers = new List<HashFsArchive>();
        try
        {
            foreach (var file in PackageFiles(request.InstallDir)) layers.Add(HashFsArchive.Open(file));
        }
        catch
        {
            foreach (var l in layers) l.Dispose();
            throw;
        }
        layers.Reverse();
        return new ScsFileSystem(layers);
    }

    public Stream? Open(string path)
    {
        foreach (var layer in _layers)
            if (layer.OpenPooled(path) is { } stream) return stream;
        return null;
    }

    /// <summary>Caminhos completos dos filhos diretos (arquivos e subdiretórios), sem repetição entre camadas.</summary>
    public IEnumerable<string> List(string dir)
    {
        var prefix = dir.Trim('/');
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var layer in _layers)
        {
            var (dirs, files) = layer.List(prefix);
            foreach (var name in dirs) seen.Add(Join(prefix, name));
            foreach (var name in files) seen.Add(Join(prefix, name));
        }
        return seen.Order(StringComparer.Ordinal).ToList();
    }

    public void Dispose()
    {
        foreach (var l in _layers) l.Dispose();
    }

    private static string Join(string dir, string name) => dir.Length == 0 ? name : dir + "/" + name;
}
