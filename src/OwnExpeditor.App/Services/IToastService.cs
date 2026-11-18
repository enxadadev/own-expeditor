using System;
using OwnExpeditor.Core.Text;

namespace OwnExpeditor.App.Services;

/// <summary>Aviso curto e temporário (Toast), com uma ação opcional (ex.: Desfazer).</summary>
public interface IToastService
{
    void Show(TextRef text, TextRef? actionLabel = null, Action? action = null);
}
