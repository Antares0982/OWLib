using DragonLib.IO;
using PrometheusTool.CLI;

namespace PrometheusTool.Modes.Debug; 

[Mode("test", typeof(TestFlags), Description = "This is to test if ModeRouter works", Group = "Debug", UtilNoArchiveNeeded = true)]
public class TestMode : IMode {
    public TestMode(TestFlags flags) {
        Logger.Info("PRO", "It works!");
    }
}
