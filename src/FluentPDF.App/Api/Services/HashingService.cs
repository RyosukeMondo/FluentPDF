// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Security.Cryptography;

namespace FluentPDF.App.Api.Services;

/// <summary>
/// Interface for hashing and comparison services.
/// </summary>
public interface IHashingService
{
    /// <summary>
    /// Computes SHA256 hash of a stream.
    /// </summary>
    /// <param name="stream">The stream to hash.</param>
    /// <returns>Hex-encoded SHA256 hash.</returns>
    string ComputeHash(Stream stream);

    /// <summary>
    /// Computes SHA256 hash of a byte array.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>Hex-encoded SHA256 hash.</returns>
    string ComputeHash(byte[] data);
}

/// <summary>
/// Provides SHA256 hashing for render verification.
/// </summary>
public sealed class HashingService : IHashingService
{
    /// <inheritdoc />
    public string ComputeHash(Stream stream)
    {
        stream.Position = 0;
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(stream);
        stream.Position = 0; // Reset for subsequent reads
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <inheritdoc />
    public string ComputeHash(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
