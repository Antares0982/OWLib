using DragonLib.CLI;
using PrometheusTool.Models.Enums;

namespace PrometheusTool.CLI;

public record ProFlags : ICLIFlags {
    [CLIFlag("directory", Positional = 0, IsRequired = true, Help = "Overwatch Directory")]
    public string OverwatchDirectory { get; set; } = null!;

    [CLIFlag("mode", Positional = 1, IsRequired = true, Help = "Extraction Mode")]
    public string Mode { get; set; } = null!;

    [CLIFlag("online", Help = "Allow downloading of corrupted files")]
    public bool Online { get; set; }

    [CLIFlag("language", Aliases = new[] { "L" }, Help = "Language to load")]
    public TACTLanguage? Language { get; set; }

    [CLIFlag("speech-language", Aliases = new[] { "T" }, Help = "Speech Language to load")]
    public TACTLanguage? SpeechLanguage { get; set; }

    [CLIFlag("graceful-exit", Help = "When enabled don't crash on invalid CMF Encryption")]
    public bool GracefulExit { get; set; }

    [CLIFlag("disable-index-cache", Help = "Cache Index files from CDN")]
    public bool DisableIndexCache { get; set; }

    [CLIFlag("disable-cdn-cache", Help = "Cache Data files from CDN")]
    public bool DisableCDNCache { get; set; }

    [CLIFlag("validate-cache", Help = "Validate files from CDN")]
    public bool ValidateCache { get; set; }

    [CLIFlag("quiet", Aliases = new[] { "q", "silent" }, Help = "Suppress majority of output messages")]
    public bool Quiet { get; set; }

    [CLIFlag("string-guid", Help = "Returns all strings as their GUID instead of their value")]
    public bool StringsAsGuids { get; set; }

    [CLIFlag("skip-keys", Help = "Skip key detection", Hidden = true)]
    public bool SkipKeys { get; set; }

    [CLIFlag("deduplicate-textures", Aliases = new[] { "0" }, Help = "Re-use textures from other models")]
    public bool Deduplicate { get; set; }

    [CLIFlag("scratch-db", Help = "Directory for persistent database storage for deduplication info")]
    public string? ScratchDBPath { get; set; }

    [CLIFlag("no-names", Help = "Don't use names for textures")]
    public bool NoNames { get; set; }

    [CLIFlag("canonical-names", Help = "Only use canonical names", Hidden = true)]
    public bool OnlyCanonical { get; set; }

    [CLIFlag("no-guid-names", Help = "Completely disables using GUIDNames", Hidden = true)]
    public bool NoGuidNames { get; set; }

    [CLIFlag("extract-shaders", Help = "Extract shader files", Hidden = true)]
    public bool ExtractShaders { get; set; }

    [CLIFlag("enable-async-save", Help = "Enable asynchronous saving", Hidden = true)]
    public bool EnableAsyncSave { get; set; }

    [CLIFlag("disable-language-registry", Help = "Disable fetching language from registry", Hidden = true)]
    public bool NoLanguageRegistry { get; set; }

    [CLIFlag("allow-manifest-fallback", Help = "Allows falling back to older versions if manifest doesn't exist", Hidden = true)]
    public bool TryManifestFallback { get; set; }
}
