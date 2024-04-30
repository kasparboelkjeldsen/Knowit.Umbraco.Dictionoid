using Knowit.Umbraco.Dictionoid.AiClients.Configurations;
using Knowit.Umbraco.Dictionoid.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Chat;

namespace Knowit.Umbraco.Dictionoid.AiClients;

public class ChatGptClient : IAiClient
{
    private readonly ChatGptApiConfiguration _config;
    private readonly OpenAIAPI _gptApi;

    public ChatGptClient(IOptions<ChatGptApiConfiguration> config)
    {
        _config = config.Value;
        _gptApi = new OpenAIAPI(_config.ApiKey);
    }

    public async Task<ChatResult> TranslateAsync(List<ChatMessage> messages)
    {
        return await _gptApi.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = _config.ModelId,
            Messages = messages,
            ResponseFormat = ChatRequest.ResponseFormats.JsonObject
        });
    }

    public List<ChatMessage> BuildPrompt(TranslationRequest request)
    {
        var messages = new List<ChatMessage>()
        {
            new ChatMessage()
            {
                Role = ChatMessageRole.System, TextContent =
                    @"
		You are a sophisticated translation service interacting with a JSON object that contains multiple 'Items,' each identified by a 'key' (language) and 'value' (text in that language). While many of these 'values' may initially be empty with at least one filled for each item, your task is to translate and complete these empty fields, ensuring a comprehensive multilingual dataset.

		It is possible for all values to be empty, but then the value 'DetectLanguage' should have been set. If DetectLanguage is set, try to determine the language of the string and use this as a basis for filling out -all- translation values.

		In addition to basic translation, you are equipped with a 'color' feature, which allows for the modification of translations based on a specific stylistic or tonal request. 

		Importantly, this feature can also instruct you to alter existing translations for consistency or to adhere to a requested style—even if it means overriding an existing value in a specific language. For example, if both Australian English and standard English texts are 'please select number,' but the instruction is to infuse 'Australian' color with humor or local vernacular, you should modify the Australian English accordingly, even though the original text may be standard.

		Your final output should be the enriched JSON, with all values filled and any specified 'color' adjustments applied, maintaining the integrity and structure of the original format."
            }
        };

        if (!string.IsNullOrWhiteSpace(request.Color))
        {
            messages.Add(
                new ChatMessage()
                {
                    Role = ChatMessageRole.System,
                    TextContent =
                        $"You are tasked with infusing the translation with a specific tone or style, as dictated by the 'color' argument provided by the user. This argument influences the translation to reflect a particular character or mood. For instance, if the input for 'color' is 'funny', your translation should incorporate humor—this could mean adding cultural references, idioms, or playful language relevant to the context. Conversely, if the 'color' is 'formal', your translation should adopt a more respectful and serious tone. Your objective is to accurately adapt the tone based on the provided descriptor, which in this case is: \"{request.Color}\". Apply this guidance thoughtfully to ensure that the translation aligns with the requested 'color', enhancing its relevance and impact."
                }
            );
        }

        messages.Add(new ChatMessage()
        {
            Role = ChatMessageRole.User,
            TextContent = $"please translate missing values in the following json: " +
                          JsonConvert.SerializeObject(request)
        });

        return messages;
    }
}