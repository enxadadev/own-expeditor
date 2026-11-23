namespace OwnExpeditor.Data.Maps;

/// <summary>Sistema de arquivos em camadas sobre pacotes .scs (mod, DLC, base).</summary>
internal interface IScsFileSystem
{
    Stream? Open(string path);
    IEnumerable<string> List(string dir);
}
