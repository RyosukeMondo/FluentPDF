using FluentPDF.QualityAgent.Models;
using FluentPDF.QualityAgent.Parsers;
using Xunit;

namespace FluentPDF.QualityAgent.Tests.Parsers;

public class SsimParserTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly SsimParser _parser;

    public SsimParserTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        _parser = new SsimParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Parse_WithNonExistentFile_ReturnsFailure()
    {
        // Act
        var result = _parser.Parse("non-existent-file.json");

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("not found", result.Errors[0].Message);
    }

    [Fact]
    public void Parse_WithPassingScores_ReturnsAllPassed()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test1_RenderPage",
                "ssimScore": 1.0,
                "baselineImage": "baseline/test1.png",
                "currentImage": "current/test1.png"
            },
            {
                "testName": "Test2_RenderDocument",
                "ssimScore": 0.995,
                "baselineImage": "baseline/test2.png",
                "currentImage": "current/test2.png"
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "passing.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        var ssimResults = result.Value;
        Assert.Equal(2, ssimResults.Total);
        Assert.Equal(2, ssimResults.Passed);
        Assert.Equal(0, ssimResults.MinorRegressions);
        Assert.Equal(0, ssimResults.MajorRegressions);
        Assert.Equal(0, ssimResults.CriticalRegressions);

        var test1 = ssimResults.Tests.First(t => t.TestName == "Test1_RenderPage");
        Assert.Equal(1.0, test1.SsimScore);
        Assert.Equal(RegressionSeverity.None, test1.Regression);
        Assert.Equal("baseline/test1.png", test1.BaselineImagePath);
        Assert.Equal("current/test1.png", test1.CurrentImagePath);
    }

    [Fact]
    public void Parse_WithMinorRegression_IdentifiesCorrectly()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test_MinorRegression",
                "ssimScore": 0.98,
                "baselineImage": "baseline/test.png",
                "currentImage": "current/test.png"
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "minor.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        var ssimResults = result.Value;
        Assert.Equal(1, ssimResults.Total);
        Assert.Equal(0, ssimResults.Passed);
        Assert.Equal(1, ssimResults.MinorRegressions);
        Assert.Equal(0, ssimResults.MajorRegressions);
        Assert.Equal(0, ssimResults.CriticalRegressions);

        var test = ssimResults.Tests[0];
        Assert.Equal(0.98, test.SsimScore);
        Assert.Equal(RegressionSeverity.Minor, test.Regression);
    }

    [Fact]
    public void Parse_WithMajorRegression_IdentifiesCorrectly()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test_MajorRegression",
                "ssimScore": 0.96
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "major.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        var ssimResults = result.Value;
        Assert.Equal(1, ssimResults.Total);
        Assert.Equal(0, ssimResults.Passed);
        Assert.Equal(0, ssimResults.MinorRegressions);
        Assert.Equal(1, ssimResults.MajorRegressions);
        Assert.Equal(0, ssimResults.CriticalRegressions);

        var test = ssimResults.Tests[0];
        Assert.Equal(0.96, test.SsimScore);
        Assert.Equal(RegressionSeverity.Major, test.Regression);
    }

    [Fact]
    public void Parse_WithCriticalRegression_IdentifiesCorrectly()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test_CriticalRegression",
                "ssimScore": 0.92
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "critical.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        var ssimResults = result.Value;
        Assert.Equal(1, ssimResults.Total);
        Assert.Equal(0, ssimResults.Passed);
        Assert.Equal(0, ssimResults.MinorRegressions);
        Assert.Equal(0, ssimResults.MajorRegressions);
        Assert.Equal(1, ssimResults.CriticalRegressions);

        var test = ssimResults.Tests[0];
        Assert.Equal(0.92, test.SsimScore);
        Assert.Equal(RegressionSeverity.Critical, test.Regression);
    }

    [Fact]
    public void Parse_WithMixedResults_ClassifiesAllCorrectly()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test_Passing",
                "ssimScore": 0.995
            },
            {
                "testName": "Test_Minor",
                "ssimScore": 0.985
            },
            {
                "testName": "Test_Major",
                "ssimScore": 0.965
            },
            {
                "testName": "Test_Critical",
                "ssimScore": 0.93
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "mixed.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        var ssimResults = result.Value;
        Assert.Equal(4, ssimResults.Total);
        Assert.Equal(1, ssimResults.Passed);
        Assert.Equal(1, ssimResults.MinorRegressions);
        Assert.Equal(1, ssimResults.MajorRegressions);
        Assert.Equal(1, ssimResults.CriticalRegressions);
    }

    [Fact]
    public void Parse_WithObjectWrapper_ParsesCorrectly()
    {
        // Arrange
        var ssimJson = """
        {
            "tests": [
                {
                    "testName": "Test1",
                    "ssimScore": 0.99
                }
            ]
        }
        """;
        var filePath = Path.Combine(_tempDirectory, "wrapped.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
    }

    [Fact]
    public void Parse_WithResultsWrapper_ParsesCorrectly()
    {
        // Arrange
        var ssimJson = """
        {
            "results": [
                {
                    "name": "Test1",
                    "score": 0.99
                }
            ]
        }
        """;
        var filePath = Path.Combine(_tempDirectory, "results-wrapped.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
    }

    [Fact]
    public void Parse_WithAlternativePropertyNames_ParsesCorrectly()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "name": "Test1",
                "score": 0.98,
                "baseline": "base.png",
                "actual": "current.png"
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "alt-names.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        var test = result.Value.Tests[0];
        Assert.Equal("Test1", test.TestName);
        Assert.Equal(0.98, test.SsimScore);
        Assert.Equal("base.png", test.BaselineImagePath);
        Assert.Equal("current.png", test.CurrentImagePath);
    }

    [Fact]
    public void Parse_WithScoreOutOfRange_ClampsToValidRange()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test_TooHigh",
                "ssimScore": 1.5
            },
            {
                "testName": "Test_TooLow",
                "ssimScore": -0.5
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "out-of-range.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        var tests = result.Value.Tests;
        Assert.Equal(1.0, tests[0].SsimScore);
        Assert.Equal(0.0, tests[1].SsimScore);
    }

    [Fact]
    public void Parse_WithStringScore_ParsesCorrectly()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test1",
                "ssimScore": "0.98"
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "string-score.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0.98, result.Value.Tests[0].SsimScore);
    }

    [Fact]
    public void Parse_WithMissingScore_SkipsTest()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test_NoScore"
            },
            {
                "testName": "Test_Valid",
                "ssimScore": 0.99
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "missing-score.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Total);
        Assert.Equal("Test_Valid", result.Value.Tests[0].TestName);
    }

    [Fact]
    public void Parse_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "invalid.json");
        File.WriteAllText(filePath, "{ invalid json }");

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("JSON parsing error", result.Errors[0].Message);
    }

    [Fact]
    public void Parse_WithBoundaryScores_ClassifiesCorrectly()
    {
        // Arrange
        var ssimJson = """
        [
            {
                "testName": "Test_BoundaryNone",
                "ssimScore": 0.99
            },
            {
                "testName": "Test_BoundaryMinor",
                "ssimScore": 0.989
            },
            {
                "testName": "Test_BoundaryMajor",
                "ssimScore": 0.97
            },
            {
                "testName": "Test_BoundaryMajor2",
                "ssimScore": 0.969
            },
            {
                "testName": "Test_BoundaryCritical",
                "ssimScore": 0.95
            },
            {
                "testName": "Test_BoundaryCritical2",
                "ssimScore": 0.949
            }
        ]
        """;
        var filePath = Path.Combine(_tempDirectory, "boundary.json");
        File.WriteAllText(filePath, ssimJson);

        // Act
        var result = _parser.Parse(filePath);

        // Assert
        Assert.True(result.IsSuccess);
        var tests = result.Value.Tests;

        // >= 0.99: None
        Assert.Equal(RegressionSeverity.None, tests[0].Regression);
        // < 0.99: Minor
        Assert.Equal(RegressionSeverity.Minor, tests[1].Regression);
        // >= 0.97: Minor
        Assert.Equal(RegressionSeverity.Minor, tests[2].Regression);
        // < 0.97: Major
        Assert.Equal(RegressionSeverity.Major, tests[3].Regression);
        // >= 0.95: Major
        Assert.Equal(RegressionSeverity.Major, tests[4].Regression);
        // < 0.95: Critical
        Assert.Equal(RegressionSeverity.Critical, tests[5].Regression);
    }
}
