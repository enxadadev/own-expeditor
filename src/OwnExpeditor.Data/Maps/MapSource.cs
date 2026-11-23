using System.Text;
using OwnExpeditor.Domain.Maps;

namespace OwnExpeditor.Data.Maps;

/// <summary>
/// Entrega o <see cref="MapIndex"/> da instalação: usa o cache em disco quando a chave (versões + impressão dos <c>.scs</c> + mods)
/// bate e, senão, lê os setores e grava o cache. Falhas esperadas viram <see cref="MapStatus"/>; só o cancelamento é exceção.
/// </summary>
/// <remarks>
/// Fase 1a: os mods entram na chave, mas ainda não são lidos; seus nomes voltam em <see cref="MapLoadResult.SkippedMods"/>.
/// Só resultados <see cref="MapStatus.Ready"/> vão para o cache (o status parcial não é guardado no arquivo).
/// </remarks>
public sealed class MapSource : IMapSource
{
    private readonly Func<MapRequest, IScsFileSystem> _fsFactory;

    public MapSource(string cacheDirectory) : this(cacheDirectory, ScsFileSystem.Create)
    {
    }

    internal MapSource(string cacheDirectory, Func<MapRequest, IScsFileSystem> fsFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheDirectory);
        ArgumentNullException.ThrowIfNull(fsFactory);
        Cache = new MapIndexCache(cacheDirectory);
        _fsFactory = fsFactory;
    }

    internal MapIndexCache Cache { get; }

    public async Task<MapLoadResult> LoadAsync(MapRequest request, IProgress<double>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        var skipped = request.Mods.Select(m => m.Name).ToList();
        if (string.IsNullOrWhiteSpace(request.InstallDir) || !Directory.Exists(request.InstallDir))
            return new MapLoadResult(MapStatus.NoGameFolder, null, skipped);

        try
        {
            var key = ComputeKey(request);
            var cached = await Cache.TryReadAsync(key, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                progress?.Report(1.0);
                return new MapLoadResult(MapStatus.Ready, cached, skipped);
            }

            var result = await ReadAsync(request, progress, ct).ConfigureAwait(false);
            if (result.Status == MapStatus.Ready && result.Index is { } index)
                await TryWriteCacheAsync(key, index, ct).ConfigureAwait(false);
            return result with { SkippedMods = skipped };
        }
        catch (NotSupportedException)
        {
            return new MapLoadResult(MapStatus.UnsupportedVersion, null, skipped);   // pacote que não é HashFS v2
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            // pacote corrompido: InvalidDataException (validado) ou, como rede de segurança, ArgumentException de índice/offset ruim
            return new MapLoadResult(MapStatus.Failed, null, skipped);
        }
    }

    private async Task<MapLoadResult> ReadAsync(MapRequest request, IProgress<double>? progress, CancellationToken ct)
    {
        var fs = _fsFactory(request);
        try
        {
            return await new MapSectorReader().ReadAsync(fs, request.Detail, progress, ct).ConfigureAwait(false);
        }
        finally
        {
            (fs as IDisposable)?.Dispose();
        }
    }

    private async Task TryWriteCacheAsync(string key, MapIndex index, CancellationToken ct)
    {
        try
        {
            await Cache.WriteAsync(key, index, ct).ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // sem cache o índice continua válido; só a próxima abertura relê os setores
        }
    }

    /// <summary>
    /// Chave do cache: jogo, versão do esquema do cache, versão do formato de setor, nível de detalhe, impressão
    /// (nome/tamanho/data) de <c>base*.scs</c>, <c>def.scs</c> e <c>dlc_*.scs</c> e, em ordem, a de cada mod.
    /// O trecho antes do primeiro <c>|</c> (o jogo) agrupa os arquivos de cache para o limite por jogo.
    /// </summary>
    internal static string ComputeKey(MapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sb = new StringBuilder(256);
        sb.Append(request.Game).Append("|schema").Append(MapIndexCache.SchemaVersion)
          .Append("|sector").Append(SectorParser.SupportedVersion)
          .Append('|').Append(request.Detail);

        if (Directory.Exists(request.InstallDir))
            foreach (var file in ScsFileSystem.PackageFiles(request.InstallDir))
                Fingerprint(sb, "pkg", Path.GetFileName(file), file);

        foreach (var mod in request.Mods)
            Fingerprint(sb, "mod", $"{mod.Name}#{mod.Priority}", mod.Path);
        return sb.ToString();
    }

    private static void Fingerprint(StringBuilder sb, string kind, string name, string path)
    {
        sb.Append('|').Append(kind).Append(':').Append(name).Append(':');
        FileSystemInfo info = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
        if (!info.Exists)
        {
            sb.Append("missing");
            return;
        }
        var size = info is FileInfo file ? file.Length : 0;
        sb.Append(size).Append(':').Append(info.LastWriteTimeUtc.Ticks);
    }
}
