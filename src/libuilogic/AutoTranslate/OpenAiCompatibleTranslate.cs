using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.Settings;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.UiLogic.Translate;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nikse.SubtitleEdit.UiLogic.Http;

namespace Nikse.SubtitleEdit.UiLogic.AutoTranslate
{
    /// <summary>
    /// Generic engine for any service exposing an OpenAI-compatible "chat/completions"
    /// endpoint (vLLM, Xiaomi MIMO, Together, etc.) - URL, API key, and model are all
    /// user-configured, so providers SE has no dedicated engine for still work (#12324).
    /// </summary>
    public class OpenAiCompatibleTranslate : IAutoTranslator, IBatchContextTranslator, IDisposable
    {
        private HttpClient _httpClient = null!;

        public static string StaticName { get; set; } = "OpenAI Compatible API";
        public override string ToString() => StaticName;
        public string Name => StaticName;
        public string Url => "https://platform.openai.com/docs/api-reference/chat";
        public string Error { get; set; } = string.Empty;
        public int MaxCharacters => 1500;

        /// <summary>
        /// Endpoint used when the url in settings is only the service base - see <see cref="AutoTranslateUrl"/>.
        /// </summary>
        public const string DefaultUrl = "http://localhost:8000/v1/chat/completions";

        public void Initialize()
        {
            _httpClient?.Dispose();
            _httpClient = HttpClientFactoryWithProxy.CreateHttpClientWithProxy();
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");
            _httpClient.BaseAddress = new Uri(AutoTranslateUrl.Complete(Configuration.Settings.Tools.OpenAiCompatibleTranslateUrl, DefaultUrl));
            _httpClient.Timeout = TimeSpan.FromMinutes(15);

            if (!string.IsNullOrEmpty(Configuration.Settings.Tools.OpenAiCompatibleTranslateApiKey))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + Configuration.Settings.Tools.OpenAiCompatibleTranslateApiKey);
            }
        }

        public List<TranslationPair> GetSupportedSourceLanguages()
        {
            return ChatGptTranslate.ListLanguages();
        }

        public List<TranslationPair> GetSupportedTargetLanguages()
        {
            return ChatGptTranslate.ListLanguages();
        }

        public async Task<string> Translate(string text, string sourceLanguageCode, string targetLanguageCode, CancellationToken cancellationToken)
        {
            // Model stays optional: single-model servers (e.g. llama.cpp, vLLM with one
            // model loaded) ignore it, while hosted providers require it.
            var model = Configuration.Settings.Tools.OpenAiCompatibleTranslateModel;
            var modelJson = string.Empty;
            if (!string.IsNullOrEmpty(model))
            {
                modelJson = "\"model\": \"" + Json.EncodeJsonText(model) + "\",";
            }

            if (string.IsNullOrWhiteSpace(Configuration.Settings.Tools.OpenAiCompatibleTranslatePrompt))
            {
                Configuration.Settings.Tools.OpenAiCompatibleTranslatePrompt = new ToolsSettings().OpenAiCompatibleTranslatePrompt;
            }
            var encodedUserMessage = LlmTranslatePrompt.BuildEncodedUserMessage(
                Configuration.Settings.Tools.OpenAiCompatibleTranslatePrompt, sourceLanguageCode, targetLanguageCode, text);
            var input = "{" + modelJson + "\"messages\": [{ \"role\": \"user\", \"content\": \"" + encodedUserMessage + "\" }]}";
            var content = new StringContent(input, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            var result = await _httpClient.PostAsync(string.Empty, content, cancellationToken);
            var bytes = await result.Content.ReadAsByteArrayAsync(cancellationToken);
            var json = Encoding.UTF8.GetString(bytes).Trim();
            if (!result.IsSuccessStatusCode)
            {
                Error = json;
                SeLogger.Error("Error calling " + StaticName + ": Status code=" + result.StatusCode + Environment.NewLine + json);
            }

            result.EnsureSuccessStatusCode();

            var parser = new SeJsonParser();
            var resultText = parser.GetFirstObject(json, "content");
            if (resultText == null)
            {
                return string.Empty;
            }

            var outputText = Json.DecodeJsonText(resultText).Trim();
            if (outputText.StartsWith('"') && outputText.EndsWith('"') && !text.StartsWith('"'))
            {
                outputText = outputText.Trim('"').Trim();
            }

            outputText = ChatGptTranslate.FixNewLines(outputText);
            outputText = ChatGptTranslate.RemovePreamble(text, outputText);
            outputText = ChatGptTranslate.DecodeUnicodeEscapes(outputText);
            return outputText.Trim();
        }

        public Task<int> TranslateBatchAsync(
            System.Collections.ObjectModel.ObservableCollection<TranslateRow> rows,
            int index,
            string sourceLanguageCode,
            string targetLanguageCode,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Settings.Tools.OpenAiCompatibleTranslatePrompt))
            {
                Configuration.Settings.Tools.OpenAiCompatibleTranslatePrompt = new ToolsSettings().OpenAiCompatibleTranslatePrompt;
            }

            var systemPrompt = ContextBatchTranslationProtocol.BuildSystemPrompt(
                Configuration.Settings.Tools.OpenAiCompatibleTranslatePrompt,
                sourceLanguageCode,
                targetLanguageCode);
            var model = Configuration.Settings.Tools.OpenAiCompatibleTranslateModel;
            return ContextBatchTranslationRunner.TranslateBatchAsync(
                rows,
                index,
                (userContent, token) => SendBatchRequestAsync(model, systemPrompt, userContent, token),
                cancellationToken);
        }

        internal static string BuildBatchRequestJson(string model, string systemPrompt, string userContent)
        {
            using var stream = new System.IO.MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                if (!string.IsNullOrWhiteSpace(model))
                {
                    writer.WriteString("model", model);
                }

                writer.WriteStartArray("messages");
                WriteMessage(writer, "system", systemPrompt);
                WriteMessage(writer, "user", userContent);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private async Task<string> SendBatchRequestAsync(
            string model,
            string systemPrompt,
            string userContent,
            CancellationToken cancellationToken)
        {
            using var content = new StringContent(
                BuildBatchRequestJson(model, systemPrompt, userContent), Encoding.UTF8, "application/json");
            using var result = await _httpClient.PostAsync(string.Empty, content, cancellationToken);
            var json = await result.Content.ReadAsStringAsync(cancellationToken);
            if (!result.IsSuccessStatusCode)
            {
                Error = json;
                SeLogger.Error("Error calling " + StaticName + ": Status code=" + result.StatusCode + Environment.NewLine + json);
            }

            result.EnsureSuccessStatusCode();
            var resultText = new SeJsonParser().GetFirstObject(json, "content");
            if (resultText == null)
            {
                Error = json;
                throw new HttpRequestException(StaticName + " returned no translated text.");
            }

            return Json.DecodeJsonText(resultText).Trim();
        }

        private static void WriteMessage(Utf8JsonWriter writer, string role, string content)
        {
            writer.WriteStartObject();
            writer.WriteString("role", role);
            writer.WriteString("content", content);
            writer.WriteEndObject();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}