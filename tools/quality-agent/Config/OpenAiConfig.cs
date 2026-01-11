namespace FluentPDF.QualityAgent.Config;

/// <summary>
/// Configuration for OpenAI/Azure OpenAI integration.
/// </summary>
public class OpenAiConfig
{
    /// <summary>
    /// The OpenAI API key. Can be set via OPENAI_API_KEY environment variable.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// The Azure OpenAI endpoint URL. Can be set via AZURE_OPENAI_ENDPOINT environment variable.
    /// </summary>
    public string? AzureEndpoint { get; init; }

    /// <summary>
    /// The Azure OpenAI deployment name. Can be set via AZURE_OPENAI_DEPLOYMENT environment variable.
    /// </summary>
    public string? AzureDeploymentName { get; init; }

    /// <summary>
    /// The model to use (e.g., "gpt-4", "gpt-3.5-turbo"). Defaults to "gpt-4".
    /// </summary>
    public string Model { get; init; } = "gpt-4";

    /// <summary>
    /// Maximum number of tokens in the prompt. Defaults to 4000.
    /// </summary>
    public int MaxPromptTokens { get; init; } = 4000;

    /// <summary>
    /// Maximum number of retry attempts. Defaults to 3.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Initial delay between retries in milliseconds. Defaults to 1000ms.
    /// </summary>
    public int InitialRetryDelayMs { get; init; } = 1000;

    /// <summary>
    /// Whether to use Azure OpenAI instead of OpenAI.
    /// </summary>
    public bool UseAzure => !string.IsNullOrEmpty(AzureEndpoint);

    /// <summary>
    /// Create an OpenAiConfig from environment variables.
    /// </summary>
    public static OpenAiConfig FromEnvironment()
    {
        return new OpenAiConfig
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            AzureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
            AzureDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT"),
            Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4"
        };
    }

    /// <summary>
    /// Validate the configuration.
    /// </summary>
    public bool IsValid()
    {
        if (UseAzure)
        {
            return !string.IsNullOrEmpty(ApiKey)
                && !string.IsNullOrEmpty(AzureEndpoint)
                && !string.IsNullOrEmpty(AzureDeploymentName);
        }

        return !string.IsNullOrEmpty(ApiKey);
    }
}
