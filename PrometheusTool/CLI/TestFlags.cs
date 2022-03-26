using DragonLib.CLI;

namespace PrometheusTool.CLI;

public record TestFlags : ICLIFlags {
    [CLIFlag("test", Category = "Test Options")]
    public string? TestFlag { get; set; }
}
