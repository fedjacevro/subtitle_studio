using System.Windows;

namespace SubtitleStudio.App.Helpers;

public class DownloadConsentService
{
    private readonly string _consentFilePath;

    public DownloadConsentService()
    {
        _consentFilePath = Path.Combine(Constants.GetAppDataPath(), "download_consent.json");
    }

    public bool EnsureConsent(string componentName, string sizeHint)
    {
        if (HasConsented(componentName))
            return true;

        var result = MessageBox.Show(
            $"Subtitle Studio needs to download {componentName} ({sizeHint}) from the internet.\n\n" +
            "Files are stored locally under %LocalAppData%\\SubtitleStudio\\ and are not bundled with the app.\n\n" +
            "Do you want to continue?",
            "Download Required",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return false;

        RecordConsent(componentName);
        return true;
    }

    private bool HasConsented(string componentName)
    {
        if (!File.Exists(_consentFilePath))
            return false;

        try
        {
            var json = File.ReadAllText(_consentFilePath);
            return json.Contains($"\"{componentName}\"", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private void RecordConsent(string componentName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_consentFilePath)!);
        var consents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(_consentFilePath))
        {
            try
            {
                var existing = System.Text.Json.JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_consentFilePath));
                if (existing != null)
                    consents = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch { /* reset */ }
        }

        consents.Add(componentName);
        File.WriteAllText(_consentFilePath,
            System.Text.Json.JsonSerializer.Serialize(consents.OrderBy(c => c).ToList()));
    }
}