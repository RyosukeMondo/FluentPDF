// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Collections.Generic;

namespace FluentPDF.App.Diagnostics.Models;

/// <summary>
/// Result model for --test-encrypt command.
/// </summary>
public sealed class EncryptCommandResult : CommandResult
{
    public bool QpdfAvailable { get; set; }
    public bool VerificationPassed { get; set; }
    public InputInfo Input { get; set; } = new();
    public OutputInfo Output { get; set; } = new();
    public EncryptionInfo Encryption { get; set; } = new();

    public sealed class InputInfo
    {
        public string File { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public sealed class OutputInfo
    {
        public string File { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public sealed class EncryptionInfo
    {
        public int StrengthBits { get; set; }
        public bool HasUserPassword { get; set; }
        public bool HasOwnerPassword { get; set; }
        public bool AllowPrint { get; set; }
        public bool AllowCopy { get; set; }
        public bool AllowModify { get; set; }
        public bool AllowAnnotate { get; set; }
    }

    public override int GetExitCode()
    {
        if (Status == "success")
        {
            return 0;
        }

        if (Errors.Exists(e => e.Contains("not found")))
        {
            return 1; // File not found
        }

        if (!QpdfAvailable)
        {
            return 3; // QPDF not found
        }

        return 2; // Encryption failed
    }
}
