using System.Reflection;

namespace OwnExpeditor.App.Services;

public static class AppInfo
{
    /// <summary>Versão do produto (M.X.Y), sem o hash de commit do SDK.</summary>
    public static string Version { get; } =
        (typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];
}
