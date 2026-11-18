using System.Diagnostics;
using OwnExpeditor.Domain.Games;

namespace OwnExpeditor.Data.Games;

internal sealed class GameProcessMonitor(Func<IEnumerable<string>>? processNames = null) : IGameProcessMonitor
{
    private readonly Func<IEnumerable<string>> _names = processNames ?? SystemProcessNames;

    public bool IsRunning(GameKind game)
    {
        var wanted = SteamLocations.ProcessName(game);
        try
        {
            return _names().Any(n => Normalize(n).Equals(wanted, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception
                                      or NotSupportedException or UnauthorizedAccessException)
        {
            return false; // sem como listar processos: não bloqueia o usuário
        }
    }

    private static string Normalize(string name) =>
        name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;

    private static IEnumerable<string> SystemProcessNames()
    {
        foreach (var p in Process.GetProcesses())
        {
            using (p)
            {
                string name;
                try { name = p.ProcessName; }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { continue; }
                yield return name;
            }
        }
    }
}
