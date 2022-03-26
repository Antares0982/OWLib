using System;

namespace PrometheusTool.Modes;

[AttributeUsage(AttributeTargets.Class)]
public class ModeAttribute : Attribute {
    public ModeAttribute(string tag, Type flags) {
        Tag = tag;
        Flags = flags;
    }

    public string Tag { get; }
    public string? Description { get; set; }
    public string? Group { get; set; }
    public Type Flags { get; }
    public bool IsSensitive { get; set; } = false;
    public bool UtilNoArchiveNeeded { get; set; } = false;
}
