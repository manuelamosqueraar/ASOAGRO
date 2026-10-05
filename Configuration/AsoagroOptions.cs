namespace Asoagro.Configuration;

public class AsoagroOptions
{
    public const string SectionName = "Asoagro";

    public AuditOptions Audit { get; set; } = new();
}

public class AuditOptions
{
    public string DefaultUser { get; set; } = "Sistema";
}
