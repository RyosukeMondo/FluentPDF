using System.ComponentModel;
using System.Reflection;
using FluentAssertions;
using FluentPDF.Mcp.Tools;
using ModelContextProtocol.Server;
using Xunit;

namespace FluentPDF.Core.Tests;

/// <summary>
/// Validates MCP tool class structure, attributes, and method signatures
/// via reflection to catch schema regressions early.
/// </summary>
public sealed class McpToolSchemaTests
{
    private static readonly Type[] ToolClasses =
    [
        typeof(SearchTools),
        typeof(DocumentTools),
        typeof(InspectionTools),
        typeof(NavigationTools),
        typeof(PageTools),
        typeof(RenderTools),
        typeof(MutationTools),
    ];

    /// <summary>
    /// Maps each tool class to its expected (Name, MethodName) pairs so we
    /// detect accidental renames or deletions.
    /// </summary>
    private static readonly Dictionary<Type, (string Name, string Method)[]> ExpectedTools = new()
    {
        [typeof(SearchTools)] =
        [
            ("pdf_search_keyword", nameof(SearchTools.SearchKeyword)),
            ("pdf_find_pages", nameof(SearchTools.FindPages)),
            ("pdf_summarize_page", nameof(SearchTools.SummarizePage)),
            ("pdf_highlight_relevant", nameof(SearchTools.HighlightRelevant)),
            ("pdf_get_metadata", nameof(SearchTools.GetMetadata)),
            ("pdf_list_annotations", nameof(SearchTools.ListAnnotations)),
        ],
        [typeof(DocumentTools)] =
        [
            ("pdf_open", nameof(DocumentTools.Open)),
            ("pdf_save", nameof(DocumentTools.Save)),
            ("pdf_close", nameof(DocumentTools.Close)),
            ("pdf_gui_state", nameof(DocumentTools.GuiState)),
        ],
        [typeof(InspectionTools)] =
        [
            ("pdf_get_text", nameof(InspectionTools.GetText)),
            ("pdf_get_objects", nameof(InspectionTools.GetObjects)),
            ("pdf_get_object_detail", nameof(InspectionTools.GetObjectDetail)),
        ],
        [typeof(NavigationTools)] =
        [
            ("pdf_navigate", nameof(NavigationTools.Navigate)),
            ("pdf_zoom", nameof(NavigationTools.Zoom)),
            ("pdf_toggle_panel", nameof(NavigationTools.TogglePanel)),
        ],
        [typeof(PageTools)] =
        [
            ("pdf_rotate_page", nameof(PageTools.RotatePage)),
            ("pdf_delete_page", nameof(PageTools.DeletePage)),
            ("pdf_insert_blank", nameof(PageTools.InsertBlank)),
        ],
        [typeof(RenderTools)] =
        [
            ("pdf_render_page", nameof(RenderTools.RenderPage)),
            ("pdf_screenshot", nameof(RenderTools.Screenshot)),
        ],
        [typeof(MutationTools)] =
        [
            ("pdf_click", nameof(MutationTools.Click)),
            ("pdf_move_selection", nameof(MutationTools.MoveSelection)),
            ("pdf_delete_selection", nameof(MutationTools.DeleteSelection)),
            ("pdf_add_text", nameof(MutationTools.AddText)),
            ("pdf_draw_shape", nameof(MutationTools.DrawShape)),
            ("pdf_list_shapes", nameof(MutationTools.ListShapes)),
            ("pdf_delete_shape", nameof(MutationTools.DeleteShape)),
        ],
    };

    #region Tool Class Structure

    [Theory]
    [MemberData(nameof(AllToolClasses))]
    public void ToolClass_IsSealed(Type toolClass)
    {
        toolClass.IsSealed.Should().BeTrue(
            $"{toolClass.Name} must be sealed to prevent unintended inheritance");
    }

    [Theory]
    [MemberData(nameof(AllToolClasses))]
    public void ToolClass_HasMcpServerToolTypeAttribute(Type toolClass)
    {
        toolClass.GetCustomAttribute<McpServerToolTypeAttribute>()
            .Should().NotBeNull(
                $"{toolClass.Name} must be decorated with [McpServerToolType]");
    }

    [Theory]
    [MemberData(nameof(AllToolClasses))]
    public void ToolClass_HasExpectedToolCount(Type toolClass)
    {
        var methods = GetToolMethods(toolClass);
        var expected = ExpectedTools[toolClass];

        methods.Should().HaveCount(expected.Length,
            $"{toolClass.Name} should expose exactly {expected.Length} MCP tools");
    }

    #endregion

    #region Tool Method Existence

    [Theory]
    [MemberData(nameof(AllExpectedToolMethods))]
    public void ToolMethod_Exists(Type toolClass, string toolName, string methodName)
    {
        var method = toolClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

        method.Should().NotBeNull(
            $"Method {methodName} (tool '{toolName}') must exist on {toolClass.Name}");
    }

    [Theory]
    [MemberData(nameof(AllExpectedToolMethods))]
    public void ToolMethod_HasMcpServerToolAttribute(Type toolClass, string toolName, string methodName)
    {
        var method = toolClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        var attr = method.GetCustomAttribute<McpServerToolAttribute>();

        attr.Should().NotBeNull(
            $"{toolClass.Name}.{methodName} must have [McpServerTool]");
        attr!.Name.Should().Be(toolName,
            $"{toolClass.Name}.{methodName} tool name must match expected value");
    }

    #endregion

    #region Tool Names

    [Fact]
    public void AllToolNames_AreUnique()
    {
        var allNames = new List<string>();

        foreach (var toolClass in ToolClasses)
        {
            foreach (var method in GetToolMethods(toolClass))
            {
                var attr = method.GetCustomAttribute<McpServerToolAttribute>()!;
                allNames.Add(attr.Name!);
            }
        }

        allNames.Should().OnlyHaveUniqueItems("MCP tool names must be globally unique");
    }

    [Fact]
    public void AllToolNames_FollowNamingConvention()
    {
        foreach (var toolClass in ToolClasses)
        {
            foreach (var method in GetToolMethods(toolClass))
            {
                var attr = method.GetCustomAttribute<McpServerToolAttribute>()!;
                attr.Name.Should().StartWith("pdf_",
                    $"Tool {attr.Name} on {toolClass.Name}.{method.Name} must use pdf_ prefix");
                attr.Name.Should().MatchRegex("^[a-z][a-z0-9_]+$",
                    $"Tool {attr.Name} must use lowercase snake_case");
            }
        }
    }

    #endregion

    #region Tool Descriptions

    [Theory]
    [MemberData(nameof(AllExpectedToolMethods))]
    public void ToolMethod_HasNonEmptyDescription(Type toolClass, string toolName, string methodName)
    {
        var method = toolClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        var desc = method.GetCustomAttribute<DescriptionAttribute>();

        desc.Should().NotBeNull(
            $"{toolClass.Name}.{methodName} (tool '{toolName}') must have a [Description]");
        desc!.Description.Should().NotBeNullOrWhiteSpace(
            $"{toolClass.Name}.{methodName} description must not be empty");
    }

    [Theory]
    [MemberData(nameof(AllExpectedToolMethods))]
    public void ToolMethod_DescriptionHasMinimumLength(Type toolClass, string toolName, string methodName)
    {
        var method = toolClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        var desc = method.GetCustomAttribute<DescriptionAttribute>()!;

        desc.Description.Length.Should().BeGreaterThanOrEqualTo(20,
            $"Tool '{toolName}' description should be descriptive (>= 20 chars)");
    }

    #endregion

    #region Method Signatures

    [Theory]
    [MemberData(nameof(AllExpectedToolMethods))]
    public void ToolMethod_IsStatic(Type toolClass, string toolName, string methodName)
    {
        var method = toolClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;

        method.IsStatic.Should().BeTrue(
            $"{toolClass.Name}.{methodName} (tool '{toolName}') must be static for MCP server registration");
    }

    [Theory]
    [MemberData(nameof(AllExpectedToolMethods))]
    public void ToolMethod_ReturnsTaskOfString(Type toolClass, string toolName, string methodName)
    {
        var method = toolClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        var returnType = method.ReturnType;

        returnType.Should().Be(typeof(Task<string>),
            $"{toolClass.Name}.{methodName} (tool '{toolName}') must return Task<string>");
    }

    [Theory]
    [MemberData(nameof(AllExpectedToolMethods))]
    public void ToolMethod_FirstParameterIsFluentPdfClient(Type toolClass, string toolName, string methodName)
    {
        var method = toolClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        var parameters = method.GetParameters();

        parameters.Should().NotBeEmpty(
            $"{toolClass.Name}.{methodName} (tool '{toolName}') must have at least the FluentPdfClient parameter");
        parameters[0].ParameterType.Name.Should().Be("FluentPdfClient",
            $"{toolClass.Name}.{methodName} (tool '{toolName}') first parameter must be FluentPdfClient");
    }

    #endregion

    #region Parameter Descriptions

    [Theory]
    [MemberData(nameof(AllExpectedToolMethods))]
    public void ToolMethod_AllNonClientParametersHaveDescriptions(
        Type toolClass, string toolName, string methodName)
    {
        var method = toolClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        var parameters = method.GetParameters().Skip(1); // skip FluentPdfClient

        foreach (var param in parameters)
        {
            var desc = param.GetCustomAttribute<DescriptionAttribute>();
            desc.Should().NotBeNull(
                $"Parameter '{param.Name}' on {toolClass.Name}.{methodName} " +
                $"(tool '{toolName}') must have a [Description]");
            desc!.Description.Should().NotBeNullOrWhiteSpace(
                $"Parameter '{param.Name}' on {toolClass.Name}.{methodName} " +
                $"description must not be empty");
        }
    }

    #endregion

    #region Specific Tool Verification

    [Fact]
    public void SearchKeyword_HasQueryParameter()
    {
        var method = typeof(SearchTools).GetMethod(nameof(SearchTools.SearchKeyword))!;
        var queryParam = method.GetParameters().FirstOrDefault(p => p.Name == "query");

        queryParam.Should().NotBeNull("SearchKeyword must have a 'query' parameter");
        queryParam!.ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void SearchKeyword_HasOptionalCaseSensitiveParameter()
    {
        var method = typeof(SearchTools).GetMethod(nameof(SearchTools.SearchKeyword))!;
        var param = method.GetParameters().FirstOrDefault(p => p.Name == "caseSensitive");

        param.Should().NotBeNull();
        param!.HasDefaultValue.Should().BeTrue("caseSensitive should be optional");
        param.DefaultValue.Should().Be(false);
    }

    [Fact]
    public void Open_HasPathParameter()
    {
        var method = typeof(DocumentTools).GetMethod(nameof(DocumentTools.Open))!;
        var pathParam = method.GetParameters().FirstOrDefault(p => p.Name == "path");

        pathParam.Should().NotBeNull("Open must have a 'path' parameter");
        pathParam!.ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void RenderPage_HasDpiDefaultOf150()
    {
        var method = typeof(RenderTools).GetMethod(nameof(RenderTools.RenderPage))!;
        var dpiParam = method.GetParameters().FirstOrDefault(p => p.Name == "dpi");

        dpiParam.Should().NotBeNull("RenderPage must have a 'dpi' parameter");
        dpiParam!.HasDefaultValue.Should().BeTrue("dpi should be optional");
        dpiParam.DefaultValue.Should().Be(150);
    }

    [Fact]
    public void AddText_HasFontSizeDefault()
    {
        var method = typeof(MutationTools).GetMethod(nameof(MutationTools.AddText))!;
        var param = method.GetParameters().FirstOrDefault(p => p.Name == "fontSize");

        param.Should().NotBeNull();
        param!.HasDefaultValue.Should().BeTrue("fontSize should be optional");
        param.DefaultValue.Should().Be(12f);
    }

    [Fact]
    public void DrawShape_HasExpectedParameterCount()
    {
        var method = typeof(MutationTools).GetMethod(nameof(MutationTools.DrawShape))!;
        // client + shape + x + y + width + height + fillColor + strokeColor + strokeWidth = 9
        method.GetParameters().Should().HaveCount(9);
    }

    [Fact]
    public void Navigate_HasTargetParameter()
    {
        var method = typeof(NavigationTools).GetMethod(nameof(NavigationTools.Navigate))!;
        var param = method.GetParameters().FirstOrDefault(p => p.Name == "target");

        param.Should().NotBeNull("Navigate must have a 'target' parameter");
        param!.ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void ListAnnotations_PageNumberIsOptional()
    {
        var method = typeof(SearchTools).GetMethod(nameof(SearchTools.ListAnnotations))!;
        var param = method.GetParameters().FirstOrDefault(p => p.Name == "pageNumber");

        param.Should().NotBeNull();
        param!.ParameterType.Should().Be(typeof(int?),
            "pageNumber should be nullable to support filtering all pages");
        param.HasDefaultValue.Should().BeTrue();
    }

    [Fact]
    public void ListShapes_HasOptionalFilters()
    {
        var method = typeof(MutationTools).GetMethod(nameof(MutationTools.ListShapes))!;
        var pageParam = method.GetParameters().FirstOrDefault(p => p.Name == "page");
        var sourceParam = method.GetParameters().FirstOrDefault(p => p.Name == "source");

        pageParam.Should().NotBeNull();
        pageParam!.ParameterType.Should().Be(typeof(int?));
        pageParam.HasDefaultValue.Should().BeTrue();

        sourceParam.Should().NotBeNull();
        sourceParam!.ParameterType.Should().Be(typeof(string));
        sourceParam.HasDefaultValue.Should().BeTrue();
    }

    #endregion

    #region Comprehensive Coverage

    [Fact]
    public void TotalToolCount_MatchesExpected()
    {
        int totalExpected = ExpectedTools.Values.Sum(arr => arr.Length);
        int totalActual = ToolClasses.Sum(t => GetToolMethods(t).Length);

        totalActual.Should().Be(totalExpected,
            $"Total MCP tool count should be {totalExpected} across all tool classes");
    }

    [Fact]
    public void AllToolClasses_AreInToolsNamespace()
    {
        foreach (var toolClass in ToolClasses)
        {
            toolClass.Namespace.Should().Be("FluentPDF.Mcp.Tools",
                $"{toolClass.Name} must be in FluentPDF.Mcp.Tools namespace");
        }
    }

    [Fact]
    public void AllToolClasses_HaveNoPublicConstructors()
    {
        foreach (var toolClass in ToolClasses)
        {
            // Sealed classes with only static methods should use the default
            // parameterless constructor; no custom public constructors expected.
            var ctors = toolClass.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(c => c.GetParameters().Length > 0)
                .ToArray();

            ctors.Should().BeEmpty(
                $"{toolClass.Name} should not have parameterized public constructors");
        }
    }

    [Fact]
    public void AllToolMethods_AreAsync()
    {
        foreach (var toolClass in ToolClasses)
        {
            foreach (var method in GetToolMethods(toolClass))
            {
                var returnType = method.ReturnType;
                returnType.IsGenericType.Should().BeTrue(
                    $"{toolClass.Name}.{method.Name} should return a generic Task");
                returnType.GetGenericTypeDefinition().Should().Be(typeof(Task<>),
                    $"{toolClass.Name}.{method.Name} should return Task<T>");
            }
        }
    }

    #endregion

    #region Helpers and Data Sources

    public static IEnumerable<object[]> AllToolClasses()
    {
        return ToolClasses.Select(t => new object[] { t });
    }

    public static IEnumerable<object[]> AllExpectedToolMethods()
    {
        foreach (var (toolClass, tools) in ExpectedTools)
        {
            foreach (var (name, method) in tools)
            {
                yield return [toolClass, name, method];
            }
        }
    }

    private static MethodInfo[] GetToolMethods(Type toolClass)
    {
        return toolClass
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null)
            .ToArray();
    }

    #endregion
}
