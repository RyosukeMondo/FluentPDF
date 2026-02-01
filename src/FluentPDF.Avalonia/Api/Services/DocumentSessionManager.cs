// Copyright (c) 2025 FluentPDF. All rights reserved.

using System.Collections.Concurrent;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Api.Services;

/// <summary>
/// Interface for managing document sessions in the API.
/// </summary>
public interface IDocumentSessionManager
{
    /// <summary>
    /// Creates a new session for a document.
    /// </summary>
    /// <param name="document">The PDF document.</param>
    /// <returns>Session ID (UUID).</returns>
    string CreateSession(PdfDocument document);

    /// <summary>
    /// Gets a document by session ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>The document, or null if not found.</returns>
    PdfDocument? GetDocument(string sessionId);

    /// <summary>
    /// Closes a document session and releases resources.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>True if session was closed, false if not found.</returns>
    bool CloseSession(string sessionId);

    /// <summary>
    /// Closes all document sessions.
    /// </summary>
    void CloseAllSessions();

    /// <summary>
    /// Gets the count of active sessions.
    /// </summary>
    int SessionCount { get; }
}

/// <summary>
/// Manages document sessions for the verification API.
/// Thread-safe using ConcurrentDictionary.
/// </summary>
public sealed class DocumentSessionManager : IDocumentSessionManager, IDisposable
{
    private readonly ConcurrentDictionary<string, PdfDocument> _sessions = new();
    private readonly ILogger<DocumentSessionManager> _logger;
    private bool _disposed;

    public DocumentSessionManager(ILogger<DocumentSessionManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public int SessionCount => _sessions.Count;

    /// <inheritdoc />
    public string CreateSession(PdfDocument document)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var sessionId = Guid.NewGuid().ToString("N");

        if (_sessions.TryAdd(sessionId, document))
        {
            _logger.LogInformation(
                "Created document session. SessionId={SessionId}, PageCount={PageCount}",
                sessionId, document.PageCount);
            return sessionId;
        }

        // Extremely unlikely collision, try again
        sessionId = Guid.NewGuid().ToString("N");
        _sessions.TryAdd(sessionId, document);
        return sessionId;
    }

    /// <inheritdoc />
    public PdfDocument? GetDocument(string sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_sessions.TryGetValue(sessionId, out var document))
        {
            return document;
        }

        _logger.LogDebug("Document session not found. SessionId={SessionId}", sessionId);
        return null;
    }

    /// <inheritdoc />
    public bool CloseSession(string sessionId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_sessions.TryRemove(sessionId, out var document))
        {
            try
            {
                document.Handle?.Dispose();
                _logger.LogInformation("Closed document session. SessionId={SessionId}", sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing document. SessionId={SessionId}", sessionId);
                return true; // Still return true since we removed from sessions
            }
        }

        _logger.LogDebug("Session not found for close. SessionId={SessionId}", sessionId);
        return false;
    }

    /// <inheritdoc />
    public void CloseAllSessions()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var sessionIds = _sessions.Keys.ToList();
        foreach (var sessionId in sessionIds)
        {
            CloseSession(sessionId);
        }

        _logger.LogInformation("Closed all sessions. Count={Count}", sessionIds.Count);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CloseAllSessions();
        _sessions.Clear();
    }
}
