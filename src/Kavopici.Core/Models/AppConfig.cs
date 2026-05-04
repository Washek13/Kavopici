using Kavopici.Models.Enums;

namespace Kavopici.Models;

public class AppConfig
{
    public int Id { get; set; }
    public Theme Theme { get; set; } = Theme.Coffee;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
