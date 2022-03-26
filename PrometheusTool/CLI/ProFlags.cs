using DragonLib.CLI;
using PrometheusTool.Models.Enums;

namespace PrometheusTool.CLI;

public record ProFlags : ICLIFlags {
    [CLIFlag("directory", Category = "General Options", Positional = 0, IsRequired = true, Help = "Overwatch Directory")]
    public string OverwatchDirectory { get; set; } = null!;

    [CLIFlag("mode", Category = "General Options", Positional = 1, IsRequired = true, Help = "Extraction Mode")]
    public string Mode { get; set; } = null!;

    [CLIFlag("quiet", Category = "General Options", Aliases = new[] { "q", "silent" }, Help = "Suppress majority of output messages")]
    public bool Quiet { get; set; }

    [CLIFlag("online", Category = "Storage Options", Help = "Allow downloading of corrupted files")]
    public bool Online { get; set; }

    [CLIFlag("language", Category = "Storage Options", Aliases = new[] { "L" }, Help = "Language to load")]
    public TACTLanguage? Language { get; set; }

    [CLIFlag("speech-language", Category = "Storage Options", Aliases = new[] { "T" }, Help = "Speech Language to load")]
    public TACTLanguage? SpeechLanguage { get; set; }

    [CLIFlag("graceful-exit", Category = "Storage Options", Help = "When enabled don't crash on invalid CMF Encryption")]
    public bool GracefulExit { get; set; }

    [CLIFlag("disable-index-cache", Category = "Storage Options", Help = "Don't cache decoded APM files")]
    public bool DisableAPMCache { get; set; }

    [CLIFlag("disable-cdn-cache", Category = "Storage Options", Help = "Don't cache data files from CDN")]
    public bool DisableCDNCache { get; set; }

    [CLIFlag("validate-cache", Category = "Storage Options", Help = "Validate files from CDN")]
    public bool ValidateCache { get; set; }

    [CLIFlag("disable-language-registry", Category = "Storage Options", Help = "Disable fetching language from registry", Hidden = true)]
    public bool NoLanguageRegistry { get; set; }

    [CLIFlag("allow-manifest-fallback", Category = "Storage Options", Help = "Allows falling back to older versions if manifest doesn't exist", Hidden = true)]
    public bool TryManifestFallback { get; set; }

    [CLIFlag("string-guid", Category = "Data Options", Help = "Returns all strings as their GUID instead of their value")]
    public bool StringsAsGuids { get; set; }

    [CLIFlag("skip-keys", Category = "Data Options", Help = "Skip key detection", Hidden = true)]
    public bool SkipKeys { get; set; }

    [CLIFlag("deduplicate-textures", Category = "Data Options", Aliases = new[] { "0" }, Help = "Re-use textures from other models")]
    public bool Deduplicate { get; set; }

    [CLIFlag("scratch-db", Category = "Data Options", Help = "Directory for persistent database storage for deduplication info")]
    public string? ScratchDBPath { get; set; }

    [CLIFlag("no-names", Category = "Data Options", Help = "Don't use names for textures")]
    public bool NoNames { get; set; }

    [CLIFlag("canonical-names", Category = "Data Options", Help = "Only use canonical names", Hidden = true)]
    public bool OnlyCanonical { get; set; }

    [CLIFlag("no-guid-names", Category = "Data Options", Help = "Completely disables using GUIDNames", Hidden = true)]
    public bool NoGuidNames { get; set; }

    [CLIFlag("extract-shaders", Category = "Data Options", Help = "Extract shader files", Hidden = true)]
    public bool ExtractShaders { get; set; }

    [CLIFlag("enable-async-save", Category = "Data Options", Help = "Enable asynchronous saving", Hidden = true)]
    public bool EnableAsyncSave { get; set; }
}
