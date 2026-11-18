namespace OwnExpeditor.Data;

/// <summary>Ajustes de localização no disco; tudo nulo = detectar no sistema (usado por testes para isolar).</summary>
public sealed record DataOptions
{
    /// <summary>Pasta que contém "Euro Truck Simulator 2"/"American Truck Simulator". Definida = só ela (mais Proton se houver <see cref="SteamRoots"/>).</summary>
    public string? DocumentsRoot { get; init; }

    /// <summary>Pasta onde fica <c>settings.json</c> (padrão: ApplicationData/OwnExpeditor).</summary>
    public string? SettingsDirectory { get; init; }

    /// <summary>Home do usuário (Linux); padrão: a do sistema.</summary>
    public string? HomeDirectory { get; init; }

    /// <summary>Raízes da Steam a usar em vez de detectar (registro, ~/.steam, Flatpak, padrões). Vazio = nenhuma.</summary>
    public IReadOnlyList<string>? SteamRoots { get; init; }

    /// <summary>Consulta o registro do Windows (só tem efeito no Windows).</summary>
    public bool UseRegistry { get; init; } = true;
}
