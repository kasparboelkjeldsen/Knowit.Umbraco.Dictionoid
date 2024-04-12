using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API.Chat;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Knowit.Umbraco.Dictionoid.API
{
	internal static class Dictionoid
	{

		internal async static Task<string> GetTranslationResult(string text, IEnumerable<ILanguage> languages, string apiKey)
		{
			var request = CreateTranslationRequest(text, languages);
			var result = await Dictionoid.GPTTranslate(request, apiKey);
			try
			{
				return result.Choices.First().Message.TextContent;
			}
			catch
			{
				return string.Empty;
			}
		}

		internal static async Task<ChatResult> GPTTranslate(TranslationRequest request, string apiKey)
		{
			var messages = new List<ChatMessage>() { new ChatMessage() { Role = ChatMessageRole.System, Content =
				@"
You are a sophisticated translation service interacting with a JSON object that contains multiple 'Items,' each identified by a 'key' (language) and 'value' (text in that language). While many of these 'values' may initially be empty with at least one filled for each item, your task is to translate and complete these empty fields, ensuring a comprehensive multilingual dataset.

It is possible for all values to be empty, but then the value 'DetectLanguage' should have been set. If DetectLanguage is set, try to determine the language of the string and use this as a basis for filling out -all- translation values.

In addition to basic translation, you are equipped with a 'color' feature, which allows for the modification of translations based on a specific stylistic or tonal request. 

Importantly, this feature can also instruct you to alter existing translations for consistency or to adhere to a requested style—even if it means overriding an existing value in a specific language. For example, if both Australian English and standard English texts are 'please select number,' but the instruction is to infuse 'Australian' color with humor or local vernacular, you should modify the Australian English accordingly, even though the original text may be standard.

Your final output should be the enriched JSON, with all values filled and any specified 'color' adjustments applied, maintaining the integrity and structure of the original format." } };

			if (!string.IsNullOrWhiteSpace(request.Color))
			{
				messages.Add(
				new ChatMessage()
				{
					Role = ChatMessageRole.System,
					Content =
					$"You are tasked with infusing the translation with a specific tone or style, as dictated by the 'color' argument provided by the user. This argument influences the translation to reflect a particular character or mood. For instance, if the input for 'color' is 'funny', your translation should incorporate humor—this could mean adding cultural references, idioms, or playful language relevant to the context. Conversely, if the 'color' is 'formal', your translation should adopt a more respectful and serious tone. Your objective is to accurately adapt the tone based on the provided descriptor, which in this case is: \"{request.Color}\". Apply this guidance thoughtfully to ensure that the translation aligns with the requested 'color', enhancing its relevance and impact."
				}
				);
			}

			messages.Add(new ChatMessage() { Role = ChatMessageRole.User, Content = $"please translate missing values in the following json: " + JsonConvert.SerializeObject(request) });

			var api = new OpenAI_API.OpenAIAPI(apiKey);
			var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
			{
				Model = "gpt-4-turbo-preview",
				Messages = messages,
				ResponseFormat = "json_object",
			});
			return result;
		}

		internal static bool UpdateDictionaryItems(string key, IEnumerable<ILanguage> languages, ILocalizationService localizationService, IDictionaryRepository dictionaryRepository, string content)
		{
			var jobject = JObject.Parse(content);
			bool success;
			if (key.Contains("."))
			{
				success = UpdateNestedDictionaryItems(key, languages, localizationService, dictionaryRepository, jobject);
			}
			else
			{
				success = UpdateSingleDictionaryItem(key, languages, localizationService, dictionaryRepository, jobject);
			}

			return success;
		}
        internal static dynamic? GetGroupedResults(string key, List<dynamic>? results)
        {
            return results
                .Where(res => res.key == key)
                .GroupBy(res => new { res.key, res.pk })
                .Select(g => new
                {
                    key = g.Key.key,
                    id = g.Key.pk,
                    translations = g.Select(res => new { lang = res.languageISOCode, text = res.value }).ToList()
                }).FirstOrDefault();
        }
        private static string CombineKeys(string buildKey, string keyPart)
		{
			return string.IsNullOrEmpty(buildKey) ? keyPart : $"{buildKey}.{keyPart}";
		}

		private static IDictionaryItem CreateAndSaveDictionaryItem(string key, Guid? parent, IEnumerable<ILanguage> languages, JObject content, ILocalizationService localizationService, IDictionaryRepository dictionaryRepository)
		{
			var newItem = localizationService.CreateDictionaryItemWithIdentity(key, parent, null);
			UpdateDictionaryItemValues(newItem, languages, content, localizationService);
			dictionaryRepository.Save(newItem);
			return newItem;
		}

		private static TranslationRequest CreateTranslationRequest(string text, IEnumerable<ILanguage> languages) =>
			new TranslationRequest
			{
				Color = "",
				DetectLanguage = text,
				Items = languages.Select(s => new TranslationItem { Key = s.CultureName, Value = "" }).ToList()
			};

		private static void UpdateDictionaryItemValues(IDictionaryItem item, IEnumerable<ILanguage> languages, JObject content, ILocalizationService localizationService)
		{
			foreach (var lang in languages)
			{
				var value = content["Items"]?.FirstOrDefault(i => i["Key"].ToString() == lang.CultureName)?["Value"]?.ToString();
				if (value != null)
				{
					localizationService.AddOrUpdateDictionaryValue(item, lang, value);
				}
			}
		}


		private static bool UpdateNestedDictionaryItems(string key, IEnumerable<ILanguage> languages, ILocalizationService localizationService, IDictionaryRepository dictionaryRepository, JObject content)
		{
			var keys = key.Split('.');
			string buildKey = string.Empty;
			Guid? parent = null;
			bool success = false;

			foreach (var k in keys)
			{
				buildKey = CombineKeys(buildKey, k);
				var dictionaryItemExists = localizationService.GetDictionaryItemByKey(buildKey);

				if (dictionaryItemExists == null)
				{
					var newItem = CreateAndSaveDictionaryItem(buildKey, parent, languages, content, localizationService, dictionaryRepository);
					parent = newItem?.Key;
					success = newItem != null;
				}
				else
				{
					parent = dictionaryItemExists.Key;
				}
			}

			return success;
		}

		private static bool UpdateSingleDictionaryItem(string key, IEnumerable<ILanguage> languages, ILocalizationService localizationService, IDictionaryRepository dictionaryRepository, JObject content)
		{
			var newItem = localizationService.CreateDictionaryItemWithIdentity(key, null, null);
			if (newItem == null) return false;

			UpdateDictionaryItemValues(newItem, languages, content, localizationService);
			dictionaryRepository.Save(newItem);
			return true;
		}
	}
}