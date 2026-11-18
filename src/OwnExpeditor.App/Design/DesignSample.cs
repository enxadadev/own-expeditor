using System;
using System.Collections.Generic;
using OwnExpeditor.App.Design.Samples;

namespace OwnExpeditor.App.Design;

/// <summary>Um estado de tela alcançável sem disco nem jogo (usado por toolkit/release/screenshots.sh). Só existe em Debug.</summary>
public sealed record DesignSample(string Name, Func<object> CreateViewModel, double Width = 1280, double Height = 800);

public static class DesignCatalog
{
    /// <summary>Todas as amostras do shell (cada uma é um <c>ShellViewModel</c> já no estado desejado).</summary>
    public static IReadOnlyList<DesignSample> Samples { get; } = [.. ShellSamples.All(), .. JobsSamples.All()];
}
