using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using MoePortal.Core.Interfaces;
using MoePortal.Infrastructure.Services;
using Moq;
using Xunit;

namespace MoePortal.Tests;

public class AiClassificationTests
{
    private AgenticFasService CreateService(string mockResponseText)
    {
        var mockChat = new Mock<IChatCompletionService>();
        mockChat.Setup(x => x.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(), 
            It.IsAny<PromptExecutionSettings>(), 
            null, 
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<ChatMessageContent> { new ChatMessageContent(AuthorRole.Assistant, mockResponseText) });

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(mockChat.Object);
        var kernel = builder.Build();

        var mockLogger = new Mock<ILogger<AgenticFasService>>();

        return new AgenticFasService(kernel, mockLogger.Object);
    }

    [Fact]
    public async Task GetChatCompletionAsync_SupportMode_GroundedResponse_ReturnsIsGroundedTrue()
    {
        // Arrange
        var service = CreateService("You can apply for the Financial Assistance Scheme through the portal by clicking 'Apply'.");
        var request = new AiChatRequest(new List<ChatMessage>(), "How do I apply for FAS?", "support", null);

        // Act
        var response = await service.GetChatCompletionAsync(request);

        // Assert
        Assert.True(response.IsGrounded);
        Assert.Equal("You can apply for the Financial Assistance Scheme through the portal by clicking 'Apply'.", response.Reply);
    }

    [Fact]
    public async Task GetChatCompletionAsync_SupportMode_FallbackResponse_ReturnsIsGroundedFalse()
    {
        // Arrange
        var service = CreateService("I don't have information on that. Please contact MOE at 6872 2220.");
        var request = new AiChatRequest(new List<ChatMessage>(), "What is the meaning of life?", "support", null);

        // Act
        var response = await service.GetChatCompletionAsync(request);

        // Assert
        Assert.False(response.IsGrounded);
        Assert.Equal("I don't have information on that. Please contact MOE at 6872 2220.", response.Reply);
    }
}
