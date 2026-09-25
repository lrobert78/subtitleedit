using System.Text.Json;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Features.Translate.LlamaCppAdvanced;
using Nikse.SubtitleEdit.Logic.Config;

namespace UITests.Features.Translate;

public class LlamaCppAdvancedProtocolSpeakerTests
{
    [Fact]
    public void BuildUserContent_WritesSpeakerMetadataForHistoryAndLines()
    {
        var history = new List<LlamaCppAdvancedProtocol.HistoryPair>
        {
            new("How are you?", "Jak się masz?", "speaker-1", "Anna", SpeakerGender.Female),
        };
        var lines = new List<LlamaCppAdvancedProtocol.BatchLine>
        {
            new(7, "I am ready.", "speaker-2", "Mark", SpeakerGender.Male),
            new(8, "Who is there?", Speaker: "Speaker 3"),
            new(9, "Let's go."),
        };

        using var document = JsonDocument.Parse(LlamaCppAdvancedProtocol.BuildUserContent(history, lines));

        var historyItem = document.RootElement.GetProperty("history")[0];
        Assert.Equal("speaker-1", historyItem.GetProperty("speakerId").GetString());
        Assert.Equal("Anna", historyItem.GetProperty("speaker").GetString());
        Assert.Equal("Female", historyItem.GetProperty("gender").GetString());

        var line = document.RootElement.GetProperty("lines")[0];
        Assert.Equal(7, line.GetProperty("n").GetInt32());
        Assert.Equal("speaker-2", line.GetProperty("speakerId").GetString());
        Assert.Equal("Mark", line.GetProperty("speaker").GetString());
        Assert.Equal("Male", line.GetProperty("gender").GetString());

        var unknownGenderLine = document.RootElement.GetProperty("lines")[1];
        Assert.Equal("Speaker 3", unknownGenderLine.GetProperty("speaker").GetString());
        Assert.Equal("Unknown", unknownGenderLine.GetProperty("gender").GetString());

        var lineWithoutProfile = document.RootElement.GetProperty("lines")[2];
        Assert.False(lineWithoutProfile.TryGetProperty("speakerId", out _));
        Assert.False(lineWithoutProfile.TryGetProperty("speaker", out _));
        Assert.False(lineWithoutProfile.TryGetProperty("gender", out _));
    }

    [Fact]
    public void BuildSystemPrompt_ExplainsThatGenderMustNotBeGuessedOrReturned()
    {
        var prompt = LlamaCppAdvancedProtocol.BuildSystemPrompt(
            "English",
            "Polish",
            new SeLlamaCppAdvanced());

        Assert.Contains("Gender may be Unknown and must not be guessed", prompt);
        Assert.Contains("never output, translate, or alter that metadata", prompt);
        Assert.Contains("Answer with ONLY a JSON object", prompt);
    }

    [Fact]
    public void IsComplete_RejectsMissingAndUnexpectedLineNumbers()
    {
        var lines = new List<LlamaCppAdvancedProtocol.BatchLine>
        {
            new(1, "One"),
            new(2, "Two"),
        };

        Assert.True(AdvancedTranslatorBase.IsComplete(
            new Dictionary<int, string> { [1] = "Jeden", [2] = "Dwa" }, lines));
        Assert.False(AdvancedTranslatorBase.IsComplete(
            new Dictionary<int, string> { [1] = "Jeden" }, lines));
        Assert.False(AdvancedTranslatorBase.IsComplete(
            new Dictionary<int, string> { [1] = "Jeden", [2] = "Dwa", [3] = "Trzy" }, lines));
    }
}
