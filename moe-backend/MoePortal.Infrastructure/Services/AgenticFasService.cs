using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MoePortal.Core.Interfaces;

namespace MoePortal.Infrastructure.Services;

public class AgenticFasService : IAiAssistantService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<AgenticFasService> _logger;
    private readonly string _knowledgeBaseContent;

    private const string SUPPORT_SYSTEM_PROMPT = """
        You are the MOE Education Support Assistant.
        Answer ONLY from the knowledge base provided below.
        If the answer is not in the knowledge base, respond EXACTLY with this phrase:
        "I don't have information on that. Please contact MOE at 6872 2220."
        Never invent or extrapolate information beyond what the knowledge base states.
        Always cite the relevant section when answering.

        KNOWLEDGE BASE:
        {knowledge_base}
        """;

    private const string FAS_DRAFT_SYSTEM_PROMPT = """
        You are an assistant helping a citizen complete a Financial Assistance Scheme (FAS) application.

        TRUSTED PREFILL — these fields are already verified from government records. Do NOT ask for them again:
        {citizen_context}

        USER INPUT REQUIRED — ask for these ONE AT A TIME in a friendly, clear manner:
        - household_income: Monthly gross household income in SGD
        - household_size: Total number of persons in household
        - num_dependants: Number of dependants supported
        - school_name: Name of educational institution
        - year_of_study: Current year or level of study
        - reason_for_application: Brief description of financial circumstances

        NON-AUTOFILL — NEVER set or suggest values for these fields. The user must tick them manually:
        - consent_given
        - declaration_true

        After each user response, output a JSON block at the very end of your reply (inside ```json and ```) 
        containing ALL fields collected so far. Do not include consent_given or declaration_true in the JSON block.
        Example format:
        ```json
        {"household_income": 2500, "household_size": 4, "num_dependants": 2, "school_name": "ABC School", "year_of_study": "Secondary 4", "reason_for_application": "Medical bills"}
        ```

        Ask only ONE question at a time. Be empathetic and professional.
        If the user provides all 6 pieces of information in a single message, do NOT ask for confirmation. Just thank them, summarize the details, and output the full JSON block immediately.
        """;

    public AgenticFasService(Kernel kernel, ILogger<AgenticFasService> logger)
    {
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;

        // Load knowledge base from KnowledgeBase/ directory at startup
        var kbPath = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase");
        if (Directory.Exists(kbPath))
        {
            var files = Directory.GetFiles(kbPath, "*.md");
            _knowledgeBaseContent = string.Join("\n\n---\n\n",
                files.Select(f => $"# Source: {Path.GetFileName(f)}\n{File.ReadAllText(f)}"));
            _logger.LogInformation("Knowledge base loaded: {FileCount} documents", files.Length);
        }
        else
        {
            _knowledgeBaseContent = "No knowledge base documents found.";
            _logger.LogWarning("KnowledgeBase directory not found at {Path}", kbPath);
        }
    }

    private ChatHistory BuildHistory(AiChatRequest request)
    {
        var systemPrompt = request.Mode switch
        {
            "support"   => SUPPORT_SYSTEM_PROMPT.Replace("{knowledge_base}", _knowledgeBaseContent),
            "fas-draft" => FAS_DRAFT_SYSTEM_PROMPT.Replace("{citizen_context}", request.CitizenContextJson ?? "{}"),
            _           => throw new ArgumentException($"Unknown AI mode: {request.Mode}")
        };

        var history = new ChatHistory(systemPrompt);

        foreach (var msg in request.History)
        {
            if (msg.Role == "user")      history.AddUserMessage(msg.Content);
            else if (msg.Role == "assistant") history.AddAssistantMessage(msg.Content);
        }

        history.AddUserMessage(request.UserMessage);
        return history;
    }

    public async IAsyncEnumerable<string> StreamChatAsync(
        AiChatRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var history  = BuildHistory(request);
        var settings = new OpenAIPromptExecutionSettings { Temperature = 0.2, MaxTokens = 2048 };

        await foreach (var chunk in _chatCompletion.GetStreamingChatMessageContentsAsync(history, settings, null, ct))
        {
            if (chunk.Content is not null)
                yield return chunk.Content;
        }
    }

    public async Task<AiChatResponse> GetChatCompletionAsync(AiChatRequest request, CancellationToken ct = default)
    {
        var history  = BuildHistory(request);
        var settings = new OpenAIPromptExecutionSettings { Temperature = 0.2, MaxTokens = 2048 };

        string replyText;
        object? updatedFields = null;

        try
        {
            var result = await _chatCompletion.GetChatMessageContentAsync(history, settings, null, ct);
            replyText = result.Content ?? "I couldn't generate a response.";

            // ── FAS draft: extract JSON block with collected fields ───────────────
            if (request.Mode == "fas-draft" && result.Content != null)
            {
                var match = Regex.Match(result.Content, @"```json\s*(\{.*?\})\s*```", RegexOptions.Singleline);
                if (match.Success)
                {
                    try
                    {
                        updatedFields = JsonSerializer.Deserialize<object>(match.Groups[1].Value);
                        replyText     = result.Content.Replace(match.Value, string.Empty).Trim();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse FAS draft JSON block from AI response");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Provider returned an error (likely invalid API Key).");
            replyText = "SYSTEM ALERT: The AI Provider API key is invalid or expired. Please contact the administrator to rotate the OPENAI_API_KEY in the backend configuration.";
            return new AiChatResponse(replyText, false, null, null);
        }

        // ── Support mode: detect fallback (unanswered question) ──────────────
        var isGrounded = true;
        if (request.Mode == "support")
        {
            var isFallback = replyText.Contains("I don't have information", StringComparison.OrdinalIgnoreCase)
                          || replyText.Contains("contact MOE", StringComparison.OrdinalIgnoreCase);

            if (isFallback)
            {
                isGrounded = false;
                _logger.LogWarning(
                    "Unanswered support question logged. Question: {Question}",
                    request.UserMessage);
                // TODO Sprint 2: persist to UnansweredQuestionLog table for support team review
            }
        }

        return new AiChatResponse(
            Reply:              replyText,
            IsGrounded:         isGrounded,
            CitationSource:     request.Mode == "support" ? "MOE Knowledge Base" : null,
            UpdatedDraftFields: updatedFields);
    }
}
