using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DragonLib.CLI;
using DragonLib.IO;
using Microsoft.Win32;
using PrometheusTool.CLI;
using PrometheusTool.Helper;
using PrometheusTool.Models.Enums;
using PrometheusTool.Modes;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Core.Product.Tank;
using TACTLib.Exceptions;
using TankLib;
using TankLib.STU;
using TankLib.STU.Types;
using TankLib.TACT;

namespace PrometheusTool;

public static class Program {
    public static uint BuildVersion;
    public static ClientHandler Client { get; private set; } = null!;
    public static ProductHandler_Tank TankHandler { get; private set; } = null!;

    public static ProFlags Flags { get; private set; } = null!;
    public static ICLIFlags ModeFlags { get; private set; } = null!;
    public static ModeAttribute? ModeInfo { get; private set; }
    public static ModeRouter ModeRouter { get; private set; } = null!;
    public static Dictionary<ushort, HashSet<ulong>> TrackedFiles { get; } = new();
    public static bool IsPTR => Client.AgentProduct.Uid == "prometheus_test";

    public static string[] AppArgs { get; set; } = Environment.GetCommandLineArgs()
        .SkipWhile(x => x.EndsWith(".exe") || x.EndsWith(".dll"))
        .ToArray();

    public static void Main() {
        InitTankSettings();
        HookConsole();

        Logger.Info("Core", $"{Assembly.GetExecutingAssembly().GetName().Name} v{Util.GetVersion(typeof(Program).Assembly)}");
        Logger.Info("Core", $"dotnet {Environment.Version} {Environment.OSVersion}");
        Logger.Info("Core", $"CommandLine: [{string.Join(", ", AppArgs.Select(x => $"\"{x}\""))}]");

        ModeRouter = new ModeRouter();
        
        var flags = CommandLineFlags.ParseFlags<ProFlags>(PrintHelp);
        if (flags == null) {
            return;
        }

        Flags = flags;
        Logger.Info("Core", Flags.ToString());

        if (string.IsNullOrWhiteSpace(Flags.OverwatchDirectory) || string.IsNullOrWhiteSpace(Flags.Mode) || Flags.Help) {
            CommandLineFlags.PrintHelp<ProFlags>(PrintHelp, true);
            return;
        }

        (ModeFlags, ModeInfo) = ModeRouter.ConstructModeFlags(Flags.Mode);
        Logger.Info("Core", ModeFlags.ToString());
        if (!ModeInfo.UtilNoArchiveNeeded) {
            try {
                InitStorage(Flags.Online);
            } catch (Exception ex) when (ex.InnerException is UnsupportedBuildVersionException) {
                Logger.Log(ConsoleSwatch.XTermColor.OrangeRed, true, Console.Error, "CASC", "FATAL", "This version of DataTool does not support this version of Overwatch. Download a newer version of the tools.");
                throw;
            } catch (FileNotFoundException) {
                // file not found exceptions thrown by TACTLib should already include good exception info, we don't need to log anything here
                throw;
            } catch {
                Logger.Log(ConsoleSwatch.XTermColor.OrangeRed,
                    true,
                    Console.Error,
                    "CASC",
                    "FATAL",
                    "\n========================\n" +
                    "Error ini tializing CASC!\n" +
                    "Please Scan & Repair your game, launch it for a minute, and try the tools again before reporting a bug!\n" +
                    "========================");
                throw;
            }

            InitKeys();
            InitMisc();
        }

        var mode = ModeRouter.ConstructMode(flags.Mode, ModeFlags);
        if (mode is IDisposable disposable) {
            disposable.Dispose();
        }
    }

    private static void PrintHelp(List<(CLIFlagAttribute? Flag, Type FlagType)> flags, bool helpInvoked) {
        CommandLineFlags.PrintHelp(flags, helpInvoked);
        ModeRouter.PrintModeHelp(helpInvoked);
    }

    private static void HookConsole() {
        AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
        Process.GetCurrentProcess().EnableRaisingEvents = true;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Console.ForegroundColor = ConsoleColor.Gray;
        Console.CancelKeyPress += (_, _) => Console.ForegroundColor = ConsoleColor.Gray;
        Console.OutputEncoding = Encoding.UTF8;
    }

    private static void HandleSingleException(Exception ex) {
        while (true) {
            if (ex is TargetInvocationException fex) {
                ex = fex.InnerException ?? ex;
            }

            Logger.Log(ConsoleSwatch.XTermColor.HotPink3, true, Console.Error, null, "FATAL", ex.Message);
            if (ex.StackTrace != null) {
                Logger.Log(ConsoleSwatch.XTermColor.MediumPurple, true, Console.Error, null, "FATAL", ex.StackTrace);
            }

            if (ex is BLTEDecoderException decoder) {
                File.WriteAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"BLTEDump-{AppDomain.CurrentDomain.FriendlyName}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.blte"), decoder.GetBLTEData());
            }

            if (ex.InnerException != null) {
                ex = ex.InnerException;
                continue;
            }

            break;
        }
    }

    [DebuggerStepThrough]
    private static void ExceptionHandler(object sender, UnhandledExceptionEventArgs e) {
        if (e.ExceptionObject is Exception ex) {
            HandleSingleException(ex);

            if (Debugger.IsAttached) {
                throw ex;
            }
        }

        unchecked {
            Environment.Exit(-1);
        }
    }

    private static void TryFetchLocaleFromRegistry() {
        if (!OperatingSystem.IsWindows()) {
            return;
        }

        try {
            if (Flags.Language == null) {
                var textLanguage = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Blizzard Entertainment\Battle.net\Launch Options\Pro", "LOCALE", null) as string;
                if (!string.IsNullOrWhiteSpace(textLanguage)) {
                    if (Enum.TryParse<TACTLanguage>(textLanguage, out var textLang)) {
                        Flags.Language = textLang;
                    } else {
                        Logger.Error("Core", $"Invalid text language found via registry: {textLanguage}. Ignoring.");
                    }
                }
            }

            if (Flags.SpeechLanguage == null) {
                var speechLanguage = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Blizzard Entertainment\Battle.net\Launch Options\Pro", "LOCALE_AUDIO", null) as string;
                if (!string.IsNullOrWhiteSpace(speechLanguage)) {
                    if (Enum.TryParse<TACTLanguage>(speechLanguage, out var speechLang)) {
                        Flags.SpeechLanguage = speechLang;
                    } else {
                        Logger.Error("Core", $"Invalid speech language found via registry: {speechLanguage}. Ignoring.");
                    }
                }
            }
        } catch (Exception) {
            // Ignored
        }
    }

    private static void InitTankSettings() {
        Logger.ShowDebug = Debugger.IsAttached;
    }

    public static void InitTrackedFiles() {
        foreach (var asset in TankHandler.m_assets) {
            var type = teResourceGUID.Type(asset.Key);
            if (!TrackedFiles.TryGetValue(type, out var typeMap)) {
                typeMap = new HashSet<ulong>();
                TrackedFiles[type] = typeMap;
            }

            typeMap.Add(asset.Key);
        }
    }

    public static void InitStorage(bool online = false) { // turnin offline off again, can cause perf issues with bundle hack
        // Attempt to load language via registry, if they were already provided via flags then this won't do anything
        if (!Flags.NoLanguageRegistry) {
            TryFetchLocaleFromRegistry();
        }

        Logger.Info("CASC", $"Text Language: {Flags.Language} | Speech Language: {Flags.SpeechLanguage}");

        ManifestCryptoHandler.AttemptFallbackManifests = Flags.TryManifestFallback;
        var args = new ClientCreateArgs {
            SpeechLanguage = Flags.SpeechLanguage.ToString(),
            TextLanguage = Flags.Language.ToString(),
            HandlerArgs = new ClientCreateArgs_Tank { CacheAPM = !Flags.DisableAPMCache, ManifestRegion = ProductHandler_Tank.REGION_DEV },
            Online = online,
        };

        LoadHelper.PreLoad();
        Client = new ClientHandler(Flags.OverwatchDirectory, args);
        LoadHelper.PostLoad(Client);

        if (args.TextLanguage != "enUS") {
            Logger.Warn("Core", "Reminder! When extracting data in other languages, the names of the heroes/skins/etc must be in the language you have chosen.");
        }

        if (Client.AgentProduct.ProductCode != "pro") {
            Logger.Warn("Core", $"The branch \"{Client.AgentProduct.ProductCode}\" is not supported!. This might result in failure to load. Proceed with caution.");
        }

        var clientLanguages = Client.AgentProduct.Settings.Languages.Select(x => x.Language).ToArray();
        if (!clientLanguages.Contains(args.TextLanguage)) {
            Logger.Warn("Core", $"Battle.Net Agent reports that text language {args.TextLanguage} is not installed.");
        } else if (!clientLanguages.Contains(args.SpeechLanguage)) {
            Logger.Warn("Core", $"Battle.Net Agent reports that speech language {args.SpeechLanguage} is not installed.");
        }

        if (Client.ProductHandler is not ProductHandler_Tank tankHandler) {
            Logger.Error("Core", $"Not a valid Overwatch installation (detected product: {Client.Product})");
            return;
        }

        TankHandler = tankHandler;

        BuildVersion = uint.Parse(Client.InstallationInfo.Values["Version"].Split('.').Last());
        switch (BuildVersion) {
            case < 39028:
                Logger.Error("Core", "DataTool doesn't support Overwatch versions below 1.14. Please use OverTool.");
                break;
            case < ProductHandler_Tank.VERSION_152_PTR:
                Logger.Error("Core", "This version of DataTool doesn't support versions of Overwatch below 1.52. Please downgrade DataTool.");
                break;
        }

        InitTrackedFiles();
    }

    public static void InitMisc() {
        if (Flags.Deduplicate) {
            // todo(naomi): port scratchdb.
        }

        if (!Flags.NoGuidNames) {
            IO.LoadGUIDTable(Flags.OnlyCanonical);
        }

        IO.LoadLocalizedNamesMapping();

        // todo: Sound.WwiseBank.GetReady();
    }

    public static void InitKeys() {
        if (!Flags.SkipKeys) {
            Logger.Info("Core", "Checking ResourceKeys");

            foreach (var key in TrackedFiles[0x90]) {
                if (!TankHandler.m_assets.ContainsKey(key)) {
                    continue;
                }

                var resourceKey = STU.GetInstance<STUResourceKey>(key);
                if (resourceKey == null || resourceKey.GetKeyID() == 0 || Client.ConfigHandler.Keyring.Keys.ContainsKey(resourceKey.GetReverseKeyID())) {
                    continue;
                }

                Client.ConfigHandler.Keyring.AddKey(resourceKey.GetReverseKeyID(), resourceKey.m_key);
                Logger.Info("Core", $"Added ResourceKey {resourceKey.GetKeyIDString()}, Value: {resourceKey.GetKeyValueString()}");
            }
        }
    }
}
