using System.IO;
using System.Text.Json;

namespace HotelSystem.Services;

public class AppSettings
{
    public string HotelName { get; set; } = "Hotel System";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public int LogRetentionDays { get; set; } = 30;
}

public class SettingsService
{
    private static readonly SettingsService _instance = new();
    public static SettingsService Instance => _instance;
    
    public AppSettings Settings { get; private set; }
    
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HotelSystem",
        "settings.json"
    );
    
    public event Action? SettingsChanged;
    
    private SettingsService()
    {
        Settings = LoadSettings();
    }
    
    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        
        return new AppSettings();
    }
    
    public void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            SettingsChanged?.Invoke();
        }
        catch { }
    }
    
    public void ResetSettings()
    {
        Settings = new AppSettings();
        SaveSettings();
    }
}

