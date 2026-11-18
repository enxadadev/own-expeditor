using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace OwnExpeditor.App.Shell;

/// <summary>Liga cada ViewModel à sua View sem reflexão (compatível com trimming); também é um <c>IDataTemplate</c>.</summary>
public sealed class ViewRegistry : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> Map = [];
    private static readonly HashSet<Type> Views = [];

    public static void Register<TVm, TView>() where TView : Control, new()
    {
        Map[typeof(TVm)] = static () => new TView();
        Views.Add(typeof(TView));
    }

    /// <summary>A View é montada pelo registro (precisa de ViewModel com serviços; as capturas usam as amostras de design).</summary>
    public static bool IsViewRegistered(Type viewType) => Views.Contains(viewType);

    public static bool IsRegistered(Type viewModelType) => Map.ContainsKey(viewModelType);

    public static IReadOnlyCollection<Type> ViewModelTypes => Map.Keys;

    /// <summary>Cria a View com o <c>DataContext</c> já definido; <c>null</c> se o ViewModel não foi registrado.</summary>
    public static Control? Build(object vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        if (!Map.TryGetValue(vm.GetType(), out var create)) return null;
        var view = create();
        view.DataContext = vm;
        return view;
    }

    Control? ITemplate<object?, Control?>.Build(object? param) => param is null ? null : Build(param);

    bool IDataTemplate.Match(object? data) => data is not null && Map.ContainsKey(data.GetType());
}
