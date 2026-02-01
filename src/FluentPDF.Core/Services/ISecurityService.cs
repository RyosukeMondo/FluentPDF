using FluentPDF.Core.Models;
using FluentResults;

namespace FluentPDF.Core.Services;

/// <summary>
/// Defines the encryption strength for PDF documents.
/// </summary>
public enum EncryptionStrength
{
    /// <summary>
    /// 128-bit AES encryption (PDF 1.6+).
    /// </summary>
    Aes128 = 128,

    /// <summary>
    /// 256-bit AES encryption (PDF 2.0+).
    /// </summary>
    Aes256 = 256
}

/// <summary>
/// Defines permission flags for encrypted PDF documents.
/// </summary>
[Flags]
public enum PdfPermissions
{
    /// <summary>
    /// No permissions granted.
    /// </summary>
    None = 0,

    /// <summary>
    /// Allow printing the document.
    /// </summary>
    Print = 1 << 0,

    /// <summary>
    /// Allow copying text and graphics from the document.
    /// </summary>
    Copy = 1 << 1,

    /// <summary>
    /// Allow modifying the document (except annotations and form fields).
    /// </summary>
    Modify = 1 << 2,

    /// <summary>
    /// Allow adding or modifying annotations and form fields.
    /// </summary>
    Annotate = 1 << 3,

    /// <summary>
    /// All permissions granted.
    /// </summary>
    All = Print | Copy | Modify | Annotate
}

/// <summary>
/// Represents encryption settings for a PDF document.
/// </summary>
public sealed class EncryptionSettings
{
    /// <summary>
    /// Gets or sets the user password required to open the document.
    /// If null or empty, the document can be opened without a password.
    /// </summary>
    public string? UserPassword { get; set; }

    /// <summary>
    /// Gets or sets the owner password required to change permissions.
    /// Must be set to enable encryption.
    /// </summary>
    public string OwnerPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the permission flags for the encrypted document.
    /// </summary>
    public PdfPermissions Permissions { get; set; } = PdfPermissions.All;

    /// <summary>
    /// Gets or sets the encryption strength.
    /// </summary>
    public EncryptionStrength Strength { get; set; } = EncryptionStrength.Aes256;
}

/// <summary>
/// Service contract for PDF security and encryption operations.
/// Provides methods to encrypt PDFs with password protection and permission controls.
/// </summary>
public interface ISecurityService
{
    /// <summary>
    /// Encrypts a PDF document with the specified settings.
    /// </summary>
    /// <param name="inputPath">The path to the input PDF file.</param>
    /// <param name="outputPath">The path to save the encrypted PDF file.</param>
    /// <param name="settings">The encryption settings (passwords, permissions, strength).</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// Fails if the input file does not exist, output path is invalid,
    /// or encryption operation fails.
    /// </returns>
    Task<Result> EncryptDocumentAsync(
        string inputPath,
        string outputPath,
        EncryptionSettings settings);

    /// <summary>
    /// Validates encryption settings before applying them.
    /// </summary>
    /// <param name="settings">The encryption settings to validate.</param>
    /// <returns>
    /// A Result indicating whether the settings are valid.
    /// Fails if owner password is missing, passwords are too weak,
    /// or settings are invalid.
    /// </returns>
    Result ValidateSettings(EncryptionSettings settings);

    /// <summary>
    /// Checks if a PDF document is encrypted.
    /// </summary>
    /// <param name="filePath">The path to the PDF file.</param>
    /// <returns>
    /// A Result containing true if the document is encrypted, false otherwise.
    /// </returns>
    Task<Result<bool>> IsEncryptedAsync(string filePath);

    /// <summary>
    /// Removes encryption from a PDF document (requires owner password).
    /// </summary>
    /// <param name="inputPath">The path to the encrypted PDF file.</param>
    /// <param name="outputPath">The path to save the decrypted PDF file.</param>
    /// <param name="ownerPassword">The owner password for the encrypted document.</param>
    /// <returns>
    /// A Result indicating success or failure.
    /// Fails if the password is incorrect or decryption fails.
    /// </returns>
    Task<Result> RemoveEncryptionAsync(
        string inputPath,
        string outputPath,
        string ownerPassword);
}
