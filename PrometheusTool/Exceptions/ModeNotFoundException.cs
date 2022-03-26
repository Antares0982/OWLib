using System;

namespace PrometheusTool.Exceptions;

public class ModeNotFoundException : Exception {
    public ModeNotFoundException(string modeName) : base($"Cannot find mode {modeName}.") => ModeName = modeName;

    public ModeNotFoundException() : base("Cannot find mode.") { }
    public string? ModeName { get; set; }
}
