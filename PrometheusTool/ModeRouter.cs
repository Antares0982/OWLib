using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DragonLib.CLI;
using DragonLib.IO;
using PrometheusTool.Exceptions;
using PrometheusTool.Modes;

namespace PrometheusTool;

public class ModeRouter {
    public ModeRouter() {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && x.GetCustomAttribute<ModeAttribute>() != null)) {
            var attribute = type.GetCustomAttribute<ModeAttribute>()!;

            Modes[attribute.Tag.ToLower()] = (attribute, type, attribute.Flags);
        }
    }

    private Dictionary<string, (ModeAttribute Info, Type Type, Type FlagsType)> Modes { get; } = new();

    public void PrintModeHelp(bool helpInvoked) {
        var done = new HashSet<string>();
        foreach (var modePair in Modes.Where(x => !x.Value.Info.IsSensitive).GroupBy(x => x.Value.Info.Group)) {
            var groupName = "Uncategorized";
            if (modePair.Key != null) {
                groupName = modePair.Key;
            }

            Logger.Info("FLAG", $"{groupName} modes: ");
            foreach (var (key, (info, _, _)) in modePair) {
                Logger.Info("FLAG", $"{key}; {info.Description}");
            }
        }

        foreach (var (_, (_, _, flagType)) in Modes) {
            if (done.Add(flagType.FullName ?? flagType.Name)) {
                CommandLineFlags.PrintHelp(flagType, CommandLineFlags.PrintHelp, helpInvoked);
            }
        }
    }

    public IMode ConstructMode(string tag) {
        if (!Modes.TryGetValue(tag, out var modeInfo)) {
            Logger.Info("Mode", $"Available modes: {string.Join(", ", Modes.Where(x => !x.Value.Info.IsSensitive).Select(x => x.Key))}");
            Logger.Fatal("Mode", $"Cannot find tool mode {tag}.");
            throw new ModeNotFoundException(tag);
        }

        var modeFlags = CommandLineFlags.ParseFlags(modeInfo.FlagsType);
        return (IMode) (Activator.CreateInstance(modeInfo.Type, modeFlags) ?? throw new InvalidOperationException());
    }

    public IMode ConstructMode(string tag, ICLIFlags? modeFlags) {
        if (!Modes.TryGetValue(tag, out var modeInfo)) {
            Logger.Info("Mode", $"Available modes: {string.Join(", ", Modes.Where(x => !x.Value.Info.IsSensitive).Select(x => x.Key))}");
            Logger.Fatal("Mode", $"Cannot find tool mode {tag}.");
            throw new ModeNotFoundException(tag);
        }

        return (IMode) (Activator.CreateInstance(modeInfo.Type, modeFlags) ?? throw new InvalidOperationException());
    }

    public (ICLIFlags Flags, ModeAttribute Info) ConstructModeFlags(string tag) {
        if (!Modes.TryGetValue(tag, out var modeInfo)) {
            Logger.Info("Mode", $"Available modes: {string.Join(", ", Modes.Where(x => !x.Value.Info.IsSensitive).Select(x => x.Key))}");
            Logger.Fatal("Mode", $"Cannot find tool mode {tag}.");
            throw new ModeNotFoundException(tag);
        }

        return (CommandLineFlags.ParseFlags(modeInfo.FlagsType), modeInfo.Info)!;
    }
}
