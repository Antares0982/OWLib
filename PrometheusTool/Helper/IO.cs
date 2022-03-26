using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DragonLib;
using DragonLib.IO;
using TACTLib.Exceptions;
using TankLib;
using static PrometheusTool.Program;

namespace PrometheusTool.Helper;

// ReSharper disable once InconsistentNaming
public static class IO {
    private static readonly string[] ReservedWords = {
        "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
        "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
        "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public static readonly Dictionary<(ulong, ushort), string> GUIDTable = new();
    public static readonly Dictionary<ushort, Dictionary<string, ulong>> LocalizedNames = new();
    private static readonly Dictionary<ushort, HashSet<string>> IgnoredLocalizedNames = new();
    public static HashSet<ulong> MissingKeyLog = new();

    public static string? GetValidFilename(string? filename, bool force = true) {
        if (Flags is { NoNames: true } && !force) {
            return null;
        }

        if (filename == null) {
            return null;
        }

        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        var invalidReStr = $@"[{invalidChars}]+";

        var newFileName = filename.TrimEnd('.');
        var sanitisedNamePart = Regex.Replace(newFileName, invalidReStr, "_");

        return ReservedWords.Select(reservedWord => $"^{reservedWord}\\.")
            .Aggregate(sanitisedNamePart,
                (current, reservedWordPattern) => Regex.Replace(current,
                    reservedWordPattern,
                    "_reservedWord_.",
                    RegexOptions.IgnoreCase));
    }

    public static void LoadGUIDTable(bool onlyCanonical) {
        var guidNamesPath = Path.Combine("Static", "GUIDNames.csv");
        if (!File.Exists(guidNamesPath)) {
            return;
        }

        foreach (var dirtyLine in File.ReadAllLines(guidNamesPath)) {
            var line = dirtyLine.Split(';').FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(line)) {
                continue;
            }

            var parts = line.Split(',').Select(x => x.Trim()).ToArray();
            var indexString = parts[0];
            var typeString = parts[1];
            var name = parts[2];
            var canonicalString = parts[3];

            var index = ulong.Parse(indexString, NumberStyles.HexNumber);
            var type = ushort.Parse(typeString, NumberStyles.HexNumber);
            var canonical = byte.Parse(canonicalString) == 1;
            if (onlyCanonical && !canonical) {
                continue;
            }

            if (!canonical) {
                name += $"-{index:X}";
            }

            if (GUIDTable.ContainsKey((index, type))) {
                Logger.Warn("GUIDNames", $"Duplicate key detected: {indexString}.{typeString}");
            }

            GUIDTable[(index, type)] = name;
        }
    }

    public static void LoadLocalizedNamesMapping() {
        var locPath = Path.Combine("Static", "LocalizedNamesMapping.csv");
        if (!File.Exists(locPath)) {
            return;
        }

        foreach (var dirtyLine in File.ReadAllLines(locPath)) {
            var line = dirtyLine.Split(';').FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(line)) {
                continue;
            }

            var parts = line.Split(',').Select(x => x.Trim()).ToArray();
            var indexString = parts[0];
            var typeString = parts[1];
            var name = parts[2];

            var index = ulong.Parse(indexString, NumberStyles.HexNumber);
            var type = ushort.Parse(typeString, NumberStyles.HexNumber);

            if (!LocalizedNames.ContainsKey(type)) {
                LocalizedNames[type] = new Dictionary<string, ulong>();
                IgnoredLocalizedNames[type] = new HashSet<string>();
            }

            if (LocalizedNames[type].ContainsKey(name) && LocalizedNames[type][name] != index) {
                Logger.Warn("LocalizedNames", $"Duplicate localized name with different values??: {indexString}.{typeString} {name}");
                LocalizedNames[type].Remove(name);
                IgnoredLocalizedNames[type].Add(name);
                continue;
            }

            if (IgnoredLocalizedNames[type].Contains(name) || LocalizedNames[type].ContainsKey(name)) {
                continue;
            }

            LocalizedNames[type][name] = index;
        }
    }

    public static ulong? TryGetLocalizedName(ushort type, string name) {
        if (!LocalizedNames.ContainsKey(type)) {
            return null;
        }

        if (!LocalizedNames[type].TryGetValue(name, out var match)) {
            return null;
        }

        var guid = new teResourceGUID(match);
        guid.SetType(type);
        return guid;
    }

    public static string GetGUIDName(ulong guid) => GetNullableGUIDName(guid) ?? GetFileName(guid);

    public static string? GetNullableGUIDName(ulong guid) {
        var index = teResourceGUID.LongKey(guid);
        var type = teResourceGUID.Type(guid);
        return GUIDTable.TryGetValue((index, type), out var name) ? name : null;
    }

    public static string GetFileName(ulong guid) => teResourceGUID.AsString(guid);

    public static void WriteFile(Stream? stream, string filename) {
        if (stream == null) {
            return;
        }

        var path = Path.GetDirectoryName(filename);
        if (!Directory.Exists(path) && path != null) {
            Directory.CreateDirectory(path);
        }

        try {
            using Stream file = File.OpenWrite(filename);
            file.SetLength(0); // ensure no leftover data
            stream.CopyTo(file);
        } catch (IOException) {
            if (File.Exists(filename)) {
                return;
            }

            throw;
        }
    }

    public static void WriteFile(string? text, string filename) {
        if (text == null) {
            return;
        }

        var path = Path.GetDirectoryName(filename);
        if (!Directory.Exists(path) && path != null) {
            Directory.CreateDirectory(path);
        }

        var bytes = Encoding.Unicode.GetBytes(text);

        try {
            using Stream file = File.OpenWrite(filename);
            file.SetLength(0); // ensure no leftover data
            file.Write(bytes, 0, bytes.Length);
        } catch (IOException) {
            if (File.Exists(filename)) {
                return;
            }

            throw;
        }
    }

    public static void WriteFile(byte[]? bytes, string filename) {
        if (bytes == null) {
            return;
        }

        var path = Path.GetDirectoryName(filename);
        if (!Directory.Exists(path) && path != null) {
            Directory.CreateDirectory(path);
        }

        try {
            using Stream file = File.OpenWrite(filename);
            file.SetLength(0); // ensure no leftover data
            file.Write(bytes, 0, bytes.Length);
        } catch (IOException) {
            if (File.Exists(filename)) {
                return;
            }

            throw;
        }
    }

    public static void WriteFile(Memory<byte> bytes, string filename) {
        var path = Path.GetDirectoryName(filename);
        if (!Directory.Exists(path) && path != null) {
            Directory.CreateDirectory(path);
        }

        try {
            using Stream file = File.OpenWrite(filename);
            file.SetLength(0); // ensure no leftover data
            file.Write(bytes.Span);
        } catch (IOException) {
            if (File.Exists(filename)) {
                return;
            }

            throw;
        }
    }

    public static void WriteFile(ulong guid, string path) {
        if (!TankHandler.m_assets.ContainsKey(guid)) {
            return;
        }

        WriteFile(OpenFile(guid), guid, path);
    }

    public static void WriteFile(ulong guid, string path, string filename) {
        if (!TankHandler.m_assets.ContainsKey(guid)) {
            return;
        }

        WriteFile(OpenFile(guid), Path.Combine(path, filename));
    }

    public static void WriteFile(Stream? stream, ulong guid, string path) {
        if (stream == null || guid == 0) {
            return;
        }

        var filename = GetFileName(guid);
        WriteFile(stream, Path.Combine(path, filename));
    }

    public static Stream? OpenFile(ulong guid) {
        try {
            return TankHandler.OpenFile(guid);
        } catch (Exception e) {
            if (e is BLTEKeyException keyException) {
                if (MissingKeyLog.Add(keyException.MissingKey) && Debugger.IsAttached) {
                    Logger.Warn("BLTE", $"Missing key: {keyException.MissingKey:X16}");
                }
            }

            Logger.Debug("Core", $"Unable to load file: {guid:X8}");
            return null;
        }
    }

    public static void CreateDirectoryFromFile(string? path) {
        if (path == null) {
            return;
        }

        var dir = Path.GetDirectoryName(path);
        CreateDirectorySafe(dir);
    }

    public static void CreateDirectorySafe(string? path) {
        if (path == null) {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        if (string.IsNullOrWhiteSpace(fullPath)) {
            return;
        }

        if (!Directory.Exists(fullPath)) {
            Directory.CreateDirectory(fullPath);
        }
    }

    public static string? GetString(ulong guid) {
        if (guid == 0) {
            return null;
        }

        try {
            return Flags is { StringsAsGuids: true } ? teResourceGUID.AsString(guid) : GetStringInternal(guid);
        } catch {
            return null;
        }
    }

    public static string? GetStringInternal(ulong guid) {
        if (guid == 0) {
            return null;
        }

        try {
            using var stream = OpenFile(guid);
            return stream == null ? null : new teString(stream);
        } catch {
            return null;
        }
    }

    public static string? GetSubtitleString(ulong key) => key == 0 ? null : GetSubtitle(key)?.m_strings?.FirstOrDefault();

    public static teSubtitleThing? GetSubtitle(ulong guid) {
        if (guid == 0) {
            return null;
        }

        using var stream = OpenFile(guid);
        if (stream == null) {
            return null;
        }

        using var reader = new BinaryReader(stream);
        return new teSubtitleThing(reader);
    }

    public static string FormatByteSize(long size) {
        var text = size.GetHumanReadableBytes();
        if (size < 0) {
            text = "-" + text;
        }

        return text;
    }
}
