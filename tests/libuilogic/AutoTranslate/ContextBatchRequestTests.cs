using System.Text.Json;
using Nikse.SubtitleEdit.UiLogic.AutoTranslate;

namespace LibUiLogicTests.AutoTranslate;

public class ContextBatchRequestTests
{
    [Fact]
    public void OpenAiCompatibleRequestUsesSeparateSystemAndUserMessages()
    {
        var json = OpenAiCompatibleTranslate.BuildBatchRequestJson("model-x", "System \"prompt\"", "{\"lines\":[]}");

        using var document = JsonDocument.Parse(json);
        Assert.Equal("model-x", document.RootElement.GetProperty("model").GetString());
        var messages = document.RootElement.GetProperty("messages");
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("System \"prompt\"", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("{\"lines\":[]}", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public void OpenAiCompatibleRequestAllowsServerWithoutModelSetting()
    {
        using var document = JsonDocument.Parse(OpenAiCompatibleTranslate.BuildBatchRequestJson("", "System", "User"));

        Assert.False(document.RootElement.TryGetProperty("model", out _));
    }

    [Fact]
    public void GeminiRequestAsksForJsonAndKeepsPromptSeparate()
    {
        var json = GeminiTranslate.BuildBatchRequestJson("System", "{\"lines\":[]}");

        using var document = JsonDocument.Parse(json);
        Assert.Equal("System", document.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal("{\"lines\":[]}", document.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString());
        Assert.Equal("application/json", document.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString());
    }
}
