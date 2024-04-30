using Knowit.Umbraco.Dictionoid.Models;
using OpenAI_API.Chat;

namespace Knowit.Umbraco.Dictionoid.AiClients;

public interface IAiClient
{
    Task<ChatResult> TranslateAsync(List<ChatMessage> messages);
    List<ChatMessage> BuildPrompt(TranslationRequest request);
}