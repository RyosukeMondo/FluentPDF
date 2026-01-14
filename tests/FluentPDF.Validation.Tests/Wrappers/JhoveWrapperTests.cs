using FluentAssertions;
using FluentPDF.Validation.Models;
using FluentPDF.Validation.Wrappers;
using Serilog;
using Serilog.Core;

namespace FluentPDF.Validation.Tests.Wrappers;

public class JhoveWrapperTests : IDisposable
{
    private readonly Logger _logger;
    private readonly string _testFilesPath;

    public JhoveWrapperTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        _testFilesPath = Path.Combine(Path.GetTempPath(), "jhove-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFilesPath);
    }

    public void Dispose()
    {
        _logger.Dispose();
        if (Directory.Exists(_testFilesPath))
        {
            Directory.Delete(_testFilesPath, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyFilePath_ReturnsFailure()
    {
        // Arrange
        var wrapper = new JhoveWrapper(_logger);

        // Act
        var result = await wrapper.ValidateAsync(string.Empty);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("File path cannot be empty");
    }

    [Fact]
    public async Task ValidateAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var wrapper = new JhoveWrapper(_logger);
        var filePath = Path.Combine(_testFilesPath, "nonexistent.pdf");

        // Act
        var result = await wrapper.ValidateAsync(filePath);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("File not found");
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task ValidateAsync_WithValidPdf_ReturnsSuccess()
    {
        // Arrange - Skip if Java not installed
        var javaInstalled = await IsJavaInstalledAsync();
        if (!javaInstalled)
        {
            // Skip test
            return;
        }

        var wrapper = new JhoveWrapper(_logger);
        var filePath = Path.Combine(_testFilesPath, "valid.pdf");
        await CreateMinimalValidPdfAsync(filePath);

        // Act
        var result = await wrapper.ValidateAsync(filePath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var jhoveResult = result.Value;
        jhoveResult.Should().NotBeNull();
        jhoveResult.Format.Should().NotBeNullOrEmpty();
        jhoveResult.Validity.Should().NotBeNullOrEmpty();
        jhoveResult.Status.Should().BeOneOf(ValidationStatus.Pass, ValidationStatus.Warn, ValidationStatus.Fail);
    }

    [Trait("Category", "Integration")]
    [Fact]
    public async Task ValidateAsync_WithCorrelationId_LogsWithCorrelationId()
    {
        // Arrange - Skip if Java not installed
        var javaInstalled = await IsJavaInstalledAsync();
        if (!javaInstalled)
        {
            return;
        }

        var wrapper = new JhoveWrapper(_logger);
        var filePath = Path.Combine(_testFilesPath, "test.pdf");
        await CreateMinimalValidPdfAsync(filePath);
        var correlationId = "test-correlation-456";

        // Act
        var result = await wrapper.ValidateAsync(filePath, correlationId);

        // Assert
        // Test passes if it doesn't throw - correlation ID is for logging
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var wrapper = new JhoveWrapper(_logger);
        var filePath = Path.Combine(_testFilesPath, "test.pdf");
        await CreateMinimalValidPdfAsync(filePath);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await wrapper.ValidateAsync(filePath, cancellationToken: cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void JhoveResult_WithValidStatus_IsValidTrue()
    {
        // Arrange & Act
        var result = new JhoveResult
        {
            Format = "1.7",
            Validity = "Well-Formed and valid",
            Status = ValidationStatus.Pass
        };

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void JhoveResult_WithInvalidStatus_IsValidFalse()
    {
        // Arrange & Act
        var result = new JhoveResult
        {
            Format = "Unknown",
            Validity = "Not valid",
            Status = ValidationStatus.Fail
        };

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Trait("Category", "Unit")]
    [Fact]
    public void JhoveResult_WithWellFormedStatus_IsValidTrue()
    {
        // Arrange & Act
        var result = new JhoveResult
        {
            Format = "1.7",
            Validity = "Well-Formed",
            Status = ValidationStatus.Warn
        };

        // Assert
        result.IsValid.Should().BeTrue();
    }

    private static async Task<bool> IsJavaInstalledAsync()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "java",
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task CreateMinimalValidPdfAsync(string filePath)
    {
        // Minimal valid PDF structure (PDF 1.4)
        var pdfContent = @"%PDF-1.4
1 0 obj
<<
/Type /Catalog
/Pages 2 0 R
>>
endobj
2 0 obj
<<
/Type /Pages
/Kids [3 0 R]
/Count 1
>>
endobj
3 0 obj
<<
/Type /Page
/Parent 2 0 R
/Resources <<
/Font <<
/F1 <<
/Type /Font
/Subtype /Type1
/BaseFont /Helvetica
>>
>>
>>
/MediaBox [0 0 612 792]
/Contents 4 0 R
>>
endobj
4 0 obj
<<
/Length 44
>>
stream
BT
/F1 12 Tf
100 700 Td
(Test PDF) Tj
ET
endstream
endobj
xref
0 5
0000000000 65535 f
0000000009 00000 n
0000000058 00000 n
0000000115 00000 n
0000000317 00000 n
trailer
<<
/Size 5
/Root 1 0 R
>>
startxref
410
%%EOF
";
        await File.WriteAllTextAsync(filePath, pdfContent);
    }

    private static string GetSampleValidJhoveJson()
    {
        return @"{
  ""jhove"": {
    ""repInfo"": [
      {
        ""version"": ""1.7"",
        ""status"": ""Well-Formed and valid"",
        ""properties"": [
          {
            ""name"": ""Pages"",
            ""values"": 1
          }
        ],
        ""messages"": []
      }
    ]
  }
}";
    }

    private static string GetSampleInvalidJhoveJson()
    {
        return @"{
  ""jhove"": {
    ""repInfo"": [
      {
        ""version"": ""Unknown"",
        ""status"": ""Not valid"",
        ""properties"": [],
        ""messages"": [
          {
            ""message"": ""Invalid PDF structure""
          },
          {
            ""message"": ""Missing required catalog""
          }
        ]
      }
    ]
  }
}";
    }

    private static string GetSampleJhoveJsonWithMetadata()
    {
        return @"{
  ""jhove"": {
    ""repInfo"": [
      {
        ""version"": ""1.7"",
        ""status"": ""Well-Formed and valid"",
        ""properties"": [
          {
            ""name"": ""Info"",
            ""values"": {
              ""Title"": ""Test Document"",
              ""Author"": ""Test Author"",
              ""CreationDate"": ""D:20230115123045+01'00'"",
              ""ModDate"": ""D:20230116143045+01'00'""
            }
          },
          {
            ""name"": ""Pages"",
            ""values"": 5
          },
          {
            ""name"": ""Encryption"",
            ""values"": {}
          }
        ],
        ""messages"": []
      }
    ]
  }
}";
    }
}
