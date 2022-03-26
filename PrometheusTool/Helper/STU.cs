using System;
using System.Linq;
using TankLib.STU;
using TankLib.STU.Types;
using static PrometheusTool.Helper.IO;

namespace PrometheusTool.Helper;

public static class STU {
    public static string? GetDescriptionString(ulong key) {
        if (key == 0) {
            return null;
        }

        var description = GetInstance<STU_96ABC153>(key);
        return GetString(description?.m_94672A2A);
    }

    public static T? GetInstance<T>(ulong key) where T : STUInstance {
        if (key == 0) {
            return null;
        }

        using var structuredData = OpenSTUSafe(key);
        return structuredData?.GetInstance<T>();
    }

    public static T[]? GetInstances<T>(ulong key) where T : STUInstance {
        if (key == 0) {
            return null;
        }

        using var structuredData = OpenSTUSafe(key);
        return structuredData?.GetInstances<T>().ToArray();
    }

    public static teStructuredData? OpenSTUSafe(ulong key) {
        if (key == 0) {
            return null;
        }

#if RELEASE
        try {
#endif
            using var stream = OpenFile(key);
            return stream == null ? null : new teStructuredData(stream);
#if RELEASE
        } catch (Exception) {
            return null;
        }
#endif
    }
}
