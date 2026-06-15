namespace MoePortal.Core.Interfaces;

public record ChatMessage(string Role, string Content); // Role: "user" | "assistant" | "system"

public record AiChatRequest(
    List<ChatMessage> History,
    string UserMessage,
    string Mode,              // "support" | "fas-draft"
    string? CitizenContextJson  // Serialized CitizenRecord fields for fas-draft mode
);

public record AiChatResponse(
    string Reply,
    bool IsGrounded,
    string? CitationSource,
    object? UpdatedDraftFields  // Non-null only in fas-draft mode
);

public interface IAiAssistantService
{
    IAsyncEnumerable<string> StreamChatAsync(AiChatRequest request, CancellationToken ct = default);
    Task<AiChatResponse> GetChatCompletionAsync(AiChatRequest request, CancellationToken ct = default);
}
