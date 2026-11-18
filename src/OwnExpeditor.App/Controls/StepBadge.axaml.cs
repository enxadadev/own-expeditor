using Avalonia;
using Avalonia.Controls;

namespace OwnExpeditor.App.Controls;

/// <summary>Número do passo (1 a 5) ao lado do rótulo de um campo: contorno cinza pendente, âmbar quando feito (design/fretes §2).</summary>
public partial class StepBadge : UserControl
{
    public static readonly StyledProperty<int> NumberProperty = AvaloniaProperty.Register<StepBadge, int>(nameof(Number));
    public static readonly StyledProperty<bool> IsDoneProperty = AvaloniaProperty.Register<StepBadge, bool>(nameof(IsDone));

    public StepBadge() => InitializeComponent();

    public int Number { get => GetValue(NumberProperty); set => SetValue(NumberProperty, value); }

    public bool IsDone { get => GetValue(IsDoneProperty); set => SetValue(IsDoneProperty, value); }
}
