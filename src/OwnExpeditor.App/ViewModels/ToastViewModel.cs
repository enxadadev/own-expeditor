using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OwnExpeditor.App.Services;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.ViewModels;

/// <summary>Toast do shell: some sozinho depois de 6 s (design/review-apply, decisão 3); um de cada vez.</summary>
public sealed partial class ToastViewModel : LocalizedViewModel, IToastService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(6);

    private readonly IUiDispatcher _ui;
    private readonly TimeProvider _time;
    private ITimer? _timer;
    private TextRef? _text;
    private TextRef? _actionLabel;
    private Action? _action;
    private int _version;

    public ToastViewModel(ILocalizer loc, IUiDispatcher ui, TimeProvider time) : base(loc)
    {
        _ui = ui;
        _time = time;
    }

    [ObservableProperty] private bool _isVisible;

    public string Text => _text is null ? "" : Loc.Format(_text);

    public string ActionText => _actionLabel is null ? "" : Loc.Format(_actionLabel);

    public bool HasAction => _actionLabel is not null && _action is not null;

    public void Show(TextRef text, TextRef? actionLabel = null, Action? action = null)
    {
        _ui.Post(() =>
        {
            _timer?.Dispose();
            _text = text;
            _actionLabel = actionLabel;
            _action = action;
            var version = ++_version;
            OnPropertyChanged(string.Empty);
            IsVisible = true;
            _timer = _time.CreateTimer(_ => _ui.Post(() => { if (version == _version) Hide(); }), null, Lifetime, Timeout.InfiniteTimeSpan);
        });
    }

    [RelayCommand]
    private void Hide()
    {
        _timer?.Dispose();
        _timer = null;
        IsVisible = false;
    }

    [RelayCommand]
    private void RunAction()
    {
        var action = _action;
        Hide();
        action?.Invoke();
    }

    protected override void OnDisposed() => _timer?.Dispose();
}
