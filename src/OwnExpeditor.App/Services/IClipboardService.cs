using System.Threading.Tasks;

namespace OwnExpeditor.App.Services;

public interface IClipboardService
{
    Task SetTextAsync(string text);
}
