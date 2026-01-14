using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// P/Invoke declarations for PDFium form API.
/// Provides managed wrappers for PDFium form fill environment and form field functions.
/// </summary>
public static class PdfiumFormInterop
{
    private const string DllName = "pdfium.dll";

    #region Form Fill Environment

    /// <summary>
    /// Initializes the form fill environment.
    /// Must be called before any form field operations.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="formInfo">Pointer to FPDF_FORMFILLINFO structure.</param>
    /// <returns>A safe handle to the form fill environment, or an invalid handle if initialization failed.</returns>
    public static SafePdfFormHandle InitFormFillEnvironment(SafePdfDocumentHandle document, IntPtr formInfo)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        var handle = FPDFDOC_InitFormFillEnvironment(document, formInfo);
        return handle;
    }

    /// <summary>
    /// Exits the form fill environment.
    /// Called automatically by SafePdfFormHandle.ReleaseHandle().
    /// </summary>
    /// <param name="formHandle">Handle to the form environment.</param>
    internal static void ExitFormFillEnvironment(IntPtr formHandle)
    {
        if (formHandle != IntPtr.Zero)
        {
            FPDFDOC_ExitFormFillEnvironment(formHandle);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfFormHandle FPDFDOC_InitFormFillEnvironment(
        SafePdfDocumentHandle document,
        IntPtr forminfo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDFDOC_ExitFormFillEnvironment(IntPtr hHandle);

    #endregion

    #region Form Field Count

    /// <summary>
    /// Gets the number of form fields on a page.
    /// </summary>
    /// <param name="formHandle">Handle to the form environment.</param>
    /// <param name="page">Handle to the page.</param>
    /// <returns>The number of form fields, or -1 on error.</returns>
    public static int GetFormFieldCount(SafePdfFormHandle formHandle, SafePdfPageHandle page)
    {
        if (formHandle == null || formHandle.IsInvalid)
        {
            return -1;
        }

        if (page == null || page.IsInvalid)
        {
            return -1;
        }

        return FPDFPage_HasFormFieldAtPoint(formHandle, page, 0, 0);
    }

    #endregion

    #region Form Field Type

    /// <summary>
    /// Form field type constants.
    /// </summary>
    public static class FieldType
    {
        public const int Unknown = 0;
        public const int PushButton = 1;
        public const int CheckBox = 2;
        public const int RadioButton = 3;
        public const int ComboBox = 4;
        public const int ListBox = 5;
        public const int TextField = 6;
        public const int Signature = 7;
    }

    /// <summary>
    /// Gets the type of a form field at a specific point.
    /// </summary>
    /// <param name="formHandle">Handle to the form environment.</param>
    /// <param name="page">Handle to the page.</param>
    /// <param name="pageX">X coordinate in page coordinate system.</param>
    /// <param name="pageY">Y coordinate in page coordinate system.</param>
    /// <returns>The form field type constant, or FieldType.Unknown if no field at the point.</returns>
    public static int GetFormFieldTypeAtPoint(SafePdfFormHandle formHandle, SafePdfPageHandle page, double pageX, double pageY)
    {
        if (formHandle == null || formHandle.IsInvalid)
        {
            return FieldType.Unknown;
        }

        if (page == null || page.IsInvalid)
        {
            return FieldType.Unknown;
        }

        return FPDFPage_HasFormFieldAtPoint(formHandle, page, pageX, pageY);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFPage_HasFormFieldAtPoint(
        SafePdfFormHandle hHandle,
        SafePdfPageHandle page,
        double page_x,
        double page_y);

    #endregion

    #region Form Field Annotations

    /// <summary>
    /// Gets the number of annotations on a page.
    /// </summary>
    /// <param name="page">Handle to the page.</param>
    /// <returns>The number of annotations, or -1 on error.</returns>
    public static int GetAnnotationCount(SafePdfPageHandle page)
    {
        if (page == null || page.IsInvalid)
        {
            return -1;
        }

        return FPDFPage_GetAnnotCount(page);
    }

    /// <summary>
    /// Gets an annotation handle by index.
    /// </summary>
    /// <param name="page">Handle to the page.</param>
    /// <param name="index">Zero-based annotation index.</param>
    /// <returns>Handle to the annotation, or IntPtr.Zero on error.</returns>
    public static IntPtr GetAnnotation(SafePdfPageHandle page, int index)
    {
        if (page == null || page.IsInvalid)
        {
            return IntPtr.Zero;
        }

        return FPDFPage_GetAnnot(page, index);
    }

    /// <summary>
    /// Closes an annotation handle.
    /// </summary>
    /// <param name="annot">Handle to the annotation.</param>
    public static void CloseAnnotation(IntPtr annot)
    {
        if (annot != IntPtr.Zero)
        {
            FPDFPage_CloseAnnot(annot);
        }
    }

    /// <summary>
    /// Gets the subtype of an annotation.
    /// </summary>
    /// <param name="annot">Handle to the annotation.</param>
    /// <returns>The annotation subtype, or AnnotationSubtype.Unknown on error.</returns>
    public static int GetAnnotationSubtype(IntPtr annot)
    {
        if (annot == IntPtr.Zero)
        {
            return AnnotationSubtype.Unknown;
        }

        return FPDFAnnot_GetSubtype(annot);
    }

    /// <summary>
    /// Annotation subtype constants.
    /// </summary>
    public static class AnnotationSubtype
    {
        public const int Unknown = 0;
        public const int Text = 1;
        public const int Link = 2;
        public const int FreeText = 3;
        public const int Line = 4;
        public const int Square = 5;
        public const int Circle = 6;
        public const int Polygon = 7;
        public const int PolyLine = 8;
        public const int Highlight = 9;
        public const int Underline = 10;
        public const int Squiggly = 11;
        public const int StrikeOut = 12;
        public const int Stamp = 13;
        public const int Caret = 14;
        public const int Ink = 15;
        public const int Popup = 16;
        public const int FileAttachment = 17;
        public const int Sound = 18;
        public const int Movie = 19;
        public const int Widget = 20;
        public const int Screen = 21;
        public const int PrinterMark = 22;
        public const int TrapNet = 23;
        public const int Watermark = 24;
        public const int ThreeD = 25;
        public const int Redact = 26;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFPage_GetAnnotCount(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFPage_GetAnnot(SafePdfPageHandle page, int index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFPage_CloseAnnot(IntPtr annot);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFAnnot_GetSubtype(IntPtr annot);

    #endregion

    #region Form Field Properties

    /// <summary>
    /// Gets the flags of a form field annotation.
    /// </summary>
    /// <param name="formHandle">Handle to the form environment.</param>
    /// <param name="annot">Handle to the annotation.</param>
    /// <returns>The form field flags, or 0 on error.</returns>
    public static int GetFormFieldFlags(SafePdfFormHandle formHandle, IntPtr annot)
    {
        if (formHandle == null || formHandle.IsInvalid || annot == IntPtr.Zero)
        {
            return 0;
        }

        return FPDFAnnot_GetFormFieldFlags(formHandle, annot);
    }

    /// <summary>
    /// Form field flag constants.
    /// </summary>
    public static class FieldFlags
    {
        public const int ReadOnly = 1 << 0;
        public const int Required = 1 << 1;
        public const int NoExport = 1 << 2;
        public const int Multiline = 1 << 12;
        public const int Password = 1 << 13;
    }

    /// <summary>
    /// Gets the value of a text form field.
    /// </summary>
    /// <param name="formHandle">Handle to the form environment.</param>
    /// <param name="annot">Handle to the annotation.</param>
    /// <returns>The form field value, or empty string on error.</returns>
    public static string GetFormFieldValue(SafePdfFormHandle formHandle, IntPtr annot)
    {
        if (formHandle == null || formHandle.IsInvalid || annot == IntPtr.Zero)
        {
            return string.Empty;
        }

        // Get value length (in bytes, UTF-16LE)
        var length = FPDFAnnot_GetFormFieldValue(formHandle, annot, null, 0);
        if (length == 0)
        {
            return string.Empty;
        }

        // Get value bytes
        var buffer = new byte[length];
        FPDFAnnot_GetFormFieldValue(formHandle, annot, buffer, length);

        // Decode UTF-16LE to string and trim null terminators
        return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    /// <summary>
    /// Gets the name of a form field.
    /// </summary>
    /// <param name="annot">Handle to the annotation.</param>
    /// <returns>The form field name, or empty string on error.</returns>
    public static string GetFormFieldName(IntPtr annot)
    {
        if (annot == IntPtr.Zero)
        {
            return string.Empty;
        }

        // Get name length (in bytes, UTF-16LE)
        var length = FPDFAnnot_GetFormFieldName(annot, null, 0);
        if (length == 0)
        {
            return string.Empty;
        }

        // Get name bytes
        var buffer = new byte[length];
        FPDFAnnot_GetFormFieldName(annot, buffer, length);

        // Decode UTF-16LE to string and trim null terminators
        return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    /// <summary>
    /// Checks if a checkbox or radio button is checked.
    /// </summary>
    /// <param name="formHandle">Handle to the form environment.</param>
    /// <param name="annot">Handle to the annotation.</param>
    /// <returns>True if checked; otherwise, false.</returns>
    public static bool IsFormFieldChecked(SafePdfFormHandle formHandle, IntPtr annot)
    {
        if (formHandle == null || formHandle.IsInvalid || annot == IntPtr.Zero)
        {
            return false;
        }

        return FPDFAnnot_IsChecked(formHandle, annot);
    }

    /// <summary>
    /// Gets the rectangle bounds of an annotation.
    /// </summary>
    /// <param name="annot">Handle to the annotation.</param>
    /// <param name="left">Outputs the left coordinate.</param>
    /// <param name="bottom">Outputs the bottom coordinate.</param>
    /// <param name="right">Outputs the right coordinate.</param>
    /// <param name="top">Outputs the top coordinate.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    public static bool GetAnnotationRect(IntPtr annot, out float left, out float bottom, out float right, out float top)
    {
        left = bottom = right = top = 0;

        if (annot == IntPtr.Zero)
        {
            return false;
        }

        return FPDFAnnot_GetRect(annot, out left, out bottom, out right, out top);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFAnnot_GetFormFieldFlags(SafePdfFormHandle hHandle, IntPtr annot);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDFAnnot_GetFormFieldValue(
        SafePdfFormHandle hHandle,
        IntPtr annot,
        byte[]? buffer,
        uint buflen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDFAnnot_GetFormFieldName(
        IntPtr annot,
        byte[]? buffer,
        uint buflen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_IsChecked(SafePdfFormHandle hHandle, IntPtr annot);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_GetRect(
        IntPtr annot,
        out float left,
        out float bottom,
        out float right,
        out float top);

    #endregion

    #region Form Field Manipulation

    /// <summary>
    /// Sets the value of a text form field.
    /// </summary>
    /// <param name="formHandle">Handle to the form environment.</param>
    /// <param name="annot">Handle to the annotation.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    public static bool SetFormFieldValue(SafePdfFormHandle formHandle, IntPtr annot, string value)
    {
        if (formHandle == null || formHandle.IsInvalid || annot == IntPtr.Zero)
        {
            return false;
        }

        // PDFium expects UTF-16LE encoded wide string (FPDF_WIDESTRING)
        var wideStr = new FPDF_WIDESTRING(value);
        return FPDFAnnot_SetStringValue(annot, "V", wideStr);
    }

    /// <summary>
    /// Sets the checked state of a checkbox or radio button.
    /// </summary>
    /// <param name="document">Handle to the document.</param>
    /// <param name="annot">Handle to the annotation.</param>
    /// <param name="isChecked">True to check; false to uncheck.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    public static bool SetFormFieldChecked(SafePdfDocumentHandle document, IntPtr annot, bool isChecked)
    {
        if (document == null || document.IsInvalid || annot == IntPtr.Zero)
        {
            return false;
        }

        return FPDFAnnot_SetAP(annot, 0, isChecked ? new FPDF_WIDESTRING("Yes") : new FPDF_WIDESTRING("Off"));
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_SetStringValue(
        IntPtr annot,
        [MarshalAs(UnmanagedType.LPStr)] string key,
        FPDF_WIDESTRING value);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_SetAP(
        IntPtr annot,
        int appearanceMode,
        FPDF_WIDESTRING value);

    #endregion

    #region Document Save

    /// <summary>
    /// Saves a PDF document with modifications.
    /// </summary>
    /// <param name="document">Handle to the document.</param>
    /// <param name="writer">Pointer to FPDF_FILEWRITE structure.</param>
    /// <param name="flags">Save flags (0 for default).</param>
    /// <returns>True if successful; otherwise, false.</returns>
    public static bool SaveDocument(SafePdfDocumentHandle document, IntPtr writer, int flags)
    {
        if (document == null || document.IsInvalid || writer == IntPtr.Zero)
        {
            return false;
        }

        return FPDF_SaveAsCopy(document, writer, flags);
    }

    /// <summary>
    /// Save flags for FPDF_SaveAsCopy.
    /// </summary>
    public static class SaveFlags
    {
        public const int Incremental = 1;
        public const int NoIncremental = 2;
        public const int RemoveSecurity = 3;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDF_SaveAsCopy(
        SafePdfDocumentHandle document,
        IntPtr pFileWrite,
        int flags);

    #endregion

    #region FPDF_WIDESTRING Helper

    /// <summary>
    /// Represents a PDFium wide string (UTF-16LE).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class FPDF_WIDESTRING
    {
        private readonly IntPtr _ptr;

        public FPDF_WIDESTRING(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                _ptr = IntPtr.Zero;
                return;
            }

            // Encode as UTF-16LE with null terminator
            var bytes = System.Text.Encoding.Unicode.GetBytes(str + '\0');
            _ptr = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, _ptr, bytes.Length);
        }

        ~FPDF_WIDESTRING()
        {
            if (_ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_ptr);
            }
        }

        public static implicit operator IntPtr(FPDF_WIDESTRING wideStr)
        {
            return wideStr._ptr;
        }
    }

    #endregion
}
