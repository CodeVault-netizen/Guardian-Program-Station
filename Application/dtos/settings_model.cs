namespace Guardian.ProgramStation.Application.Dtos;

public sealed class SettingsModel
{
    public string Language { get; set; } = "en";

    public string ThemeId { get; set; } = "dark";

    public bool AutoIndexEnabled { get; set; }

    public string ScheduleInterval { get; set; } = "daily";

    public List<string> FavoriteFolders { get; set; } = new();

    public DateTime? LastIndexAt { get; set; }

    public int LastIndexFoundCount { get; set; }

    /// <summary>The folder the user last saved a tree to; the save dialog opens there next time.</summary>
    public string? LastSaveFolder { get; set; }
}
