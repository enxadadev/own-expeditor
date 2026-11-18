using System.Text;

namespace OwnExpeditor.Domain.Saves;

public static class ProfileNames
{
    /// <summary>
    /// Nome do perfil a partir do nome da pasta, que é o hex do nome em UTF-8
    /// ("4A2E204F776E657469" → "J. Owneti"). Se não for hex válido, devolve o próprio id.
    /// </summary>
    public static string FromFolderName(string folderName)
    {
        ArgumentNullException.ThrowIfNull(folderName);
        if (folderName.Length == 0 || folderName.Length % 2 != 0) return folderName;
        try
        {
            var bytes = Convert.FromHexString(folderName);
            var text = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
            return string.IsNullOrWhiteSpace(text) || text.Any(char.IsControl) ? folderName : text;
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            return folderName;
        }
    }
}
