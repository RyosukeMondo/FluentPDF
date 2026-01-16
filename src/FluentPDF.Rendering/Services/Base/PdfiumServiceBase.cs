namespace FluentPDF.Rendering.Services.Base;

/// <summary>
/// Base class for services that interact with the PDFium native library.
/// Provides threading-safe helper methods and comprehensive documentation of PDFium threading constraints.
/// </summary>
/// <remarks>
/// <para><strong>CRITICAL THREADING REQUIREMENT:</strong></para>
/// <para>
/// PDFium native library calls CANNOT be executed from Task.Run threads in .NET 9.0 WinUI 3
/// self-contained deployments. Doing so causes immediate AccessViolation crashes that terminate
/// the application. All PDFium interop calls MUST execute on the calling thread.
/// </para>
/// <para><strong>CORRECT PATTERN - Use Task.Yield():</strong></para>
/// <code>
/// public async Task&lt;Result&lt;Data&gt;&gt; OperationAsync(...)
/// {
///     await Task.Yield();  // Proper async behavior without thread switch
///
///     var handle = PdfiumInterop.Method(...);  // Safe: executes on calling thread
///     // ... process data
///     return Result.Ok(data);
/// }
/// </code>
/// <para><strong>INCORRECT PATTERN - DO NOT USE Task.Run:</strong></para>
/// <code>
/// // ❌ WRONG - DO NOT DO THIS - WILL CRASH:
/// public async Task&lt;Result&lt;Data&gt;&gt; OperationAsync(...)
/// {
///     return await Task.Run(() =>  // ❌ Thread switch causes crash
///     {
///         var handle = PdfiumInterop.Method(...);  // ❌ Crashes in .NET 9.0 WinUI 3
///         return Result.Ok(data);
///     });
/// }
/// </code>
/// <para><strong>WHY Task.Yield() INSTEAD OF Task.Run:</strong></para>
/// <list type="bullet">
/// <item><description>Task.Yield() yields control back to the scheduler without switching threads</description></item>
/// <item><description>Provides async behavior for UI responsiveness without thread pool execution</description></item>
/// <item><description>Keeps PDFium calls on the original calling thread, avoiding AccessViolation</description></item>
/// <item><description>Maintains compatibility with WinUI 3 threading model and .NET 9.0</description></item>
/// </list>
/// <para><strong>ARCHITECTURAL SAFEGUARDS:</strong></para>
/// <list type="number">
/// <item><description>All services calling PDFium MUST inherit from PdfiumServiceBase</description></item>
/// <item><description>Use ExecutePdfiumOperationAsync&lt;T&gt; helper for standard async patterns</description></item>
/// <item><description>Add code comments in service methods explaining threading requirements</description></item>
/// <item><description>Never wrap PdfiumInterop calls in Task.Run, Parallel.For, or similar constructs</description></item>
/// </list>
/// </remarks>
public abstract class PdfiumServiceBase
{
    /// <summary>
    /// Executes a PDFium operation asynchronously without switching threads.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="operation">
    /// The synchronous operation to execute. This function will be called on the current thread,
    /// not on a background thread pool thread.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the value
    /// returned by the operation function.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method uses Task.Yield() to provide async behavior without switching threads,
    /// ensuring PDFium calls remain on the calling thread to prevent crashes.
    /// </para>
    /// <para><strong>USAGE EXAMPLE:</strong></para>
    /// <code>
    /// public async Task&lt;Result&lt;List&lt;BookmarkNode&gt;&gt;&gt; ExtractBookmarksAsync(PdfDocument document)
    /// {
    ///     return await ExecutePdfiumOperationAsync(() =>
    ///     {
    ///         var handle = (SafePdfDocumentHandle)document.Handle;
    ///         // ... PDFium interop calls
    ///         return Result.Ok(bookmarks);
    ///     });
    /// }
    /// </code>
    /// <para><strong>THREAD SAFETY:</strong></para>
    /// <para>
    /// The operation executes on the calling thread after yielding to the scheduler.
    /// This is safe for PDFium calls and maintains UI responsiveness without the
    /// AccessViolation crashes caused by Task.Run.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    protected static async Task<T> ExecutePdfiumOperationAsync<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Yield to provide async behavior without thread switching.
        // This allows the UI thread to remain responsive while keeping
        // PDFium calls on the original calling thread to prevent crashes.
        await Task.Yield();

        return operation();
    }

    /// <summary>
    /// Executes a PDFium operation asynchronously without switching threads, with no return value.
    /// </summary>
    /// <param name="operation">
    /// The synchronous operation to execute. This action will be called on the current thread,
    /// not on a background thread pool thread.
    /// </param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method uses Task.Yield() to provide async behavior without switching threads,
    /// ensuring PDFium calls remain on the calling thread to prevent crashes.
    /// </para>
    /// <para><strong>USAGE EXAMPLE:</strong></para>
    /// <code>
    /// public async Task UpdateFormFieldAsync(PdfDocument document, FormFieldUpdate update)
    /// {
    ///     await ExecutePdfiumOperationAsync(() =>
    ///     {
    ///         var handle = (SafePdfDocumentHandle)document.Handle;
    ///         PdfiumInterop.UpdateFormField(handle, update);
    ///     });
    /// }
    /// </code>
    /// <para><strong>THREAD SAFETY:</strong></para>
    /// <para>
    /// The operation executes on the calling thread after yielding to the scheduler.
    /// This is safe for PDFium calls and maintains UI responsiveness.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when operation is null.</exception>
    protected static async Task ExecutePdfiumOperationAsync(Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Yield to provide async behavior without thread switching
        await Task.Yield();

        operation();
    }
}
