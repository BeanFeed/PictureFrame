using System.ComponentModel.DataAnnotations;

namespace UpdateManager.Database.Entities;

public class SystemConfiguration
{
    [Key]
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}