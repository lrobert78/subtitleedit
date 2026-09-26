using System.Collections.ObjectModel;
using System.Text.Json;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.UiLogic.Translate;

namespace LibUiLogicTests.Translate;

public class ContextBatchTranslationProtocolTests
{
    [Fact]
    public void UserContentCarriesSpeakerMetadataWithoutGuessingGender()
    {
        var content = ContextBatchTranslationProtocol.BuildUserContent(
            [new ContextBatchTranslationProtocol.HistoryItem("Hello", "Cześć", "speaker-1", "Anna", SpeakerGender.Female)],
            [new ContextBatchTranslationProtocol.Line(1, "How are you?", "speaker-2", "Alex", SpeakerGender.Unknown)]);

        using var document = JsonDocument.Parse(content);
        var history = document.RootElement.GetProperty("history")[0];
        Assert.Equal("speaker-1", history.GetProperty("speakerId").GetString());
        Assert.Equal("Anna", history.GetProperty("speaker").GetString());
        Assert.Equal("Female", history.GetProperty("gender").GetString());

        var line = document.RootElement.GetProperty("lines")[0];
        Assert.Equal(1, line.GetProperty("n").GetInt32());
        Assert.Equal("speaker-2", line.GetProperty("speakerId").GetString());
        Assert.Equal("Alex", line.GetProperty("speaker").GetString());
        Assert.Equal("Unknown", line.GetProperty("gender").GetString());
    }

    [Fact]
    public void CompletenessRequiresEveryRequestedNumberAndNoExtras()
    {
        var lines = new[]
        {
            new ContextBatchTranslationProtocol.Line(1, "One", "", "", SpeakerGender.Unknown),
            new ContextBatchTranslationProtocol.Line(2, "Two", "", "", SpeakerGender.Unknown),
        };

        Assert.True(ContextBatchTranslationProtocol.IsComplete(
            ContextBatchTranslationProtocol.ParseTranslations("```json\n{\"1\":\"Jeden\",\"2\":\"Dwa\"}\n```"), lines));
        Assert.False(ContextBatchTranslationProtocol.IsComplete(
            ContextBatchTranslationProtocol.ParseTranslations("{\"1\":\"Jeden\"}"), lines));
        Assert.False(ContextBatchTranslationProtocol.IsComplete(
            ContextBatchTranslationProtocol.ParseTranslations("{\"1\":\"Jeden\",\"2\":\"Dwa\",\"3\":\"Trzy\"}"), lines));
        Assert.False(ContextBatchTranslationProtocol.IsComplete(
            ContextBatchTranslationProtocol.ParseTranslations("{\"1\":\"Jeden\",\"2\":\"Dwa\",\"note\":\"extra\"}"), lines));
        Assert.False(ContextBatchTranslationProtocol.IsComplete(
            ContextBatchTranslationProtocol.ParseTranslations("{\"1\":\"Jeden\",\"1\":\"Raz\",\"2\":\"Dwa\"}"), lines));
        Assert.False(ContextBatchTranslationProtocol.IsComplete(
            ContextBatchTranslationProtocol.ParseTranslations("{\"1\":\"Jeden\",\"2\":\"\"}"), lines));
    }

    [Fact]
    public async Task RunnerSendsHistoryAndMetadataAndOnlyChangesTranslatedText()
    {
        var rows = new ObservableCollection<TranslateRow>
        {
            new()
            {
                Number = 1, Text = "Earlier", TranslatedText = "Wcześniej",
                SpeakerId = "speaker-1", Actor = "Anna", Gender = SpeakerGender.Female,
            },
            new()
            {
                Number = 2, Text = "{\\an8}<i>Hello</i>", SpeakerId = "speaker-2",
                Actor = "Alex", Gender = SpeakerGender.Unknown,
            },
            new()
            {
                Number = 3, Text = "Goodbye", SpeakerId = "speaker-1",
                Actor = "Anna", Gender = SpeakerGender.Female,
            },
        };
        string? request = null;

        var translated = await ContextBatchTranslationRunner.TranslateBatchAsync(
            rows,
            1,
            (content, _) =>
            {
                request = content;
                return Task.FromResult("{\"1\":\"Cześć\",\"2\":\"Do widzenia\"}");
            },
            CancellationToken.None);

        Assert.Equal(2, translated);
        Assert.Equal("{\\an8}Cześć", rows[1].TranslatedText);
        Assert.Equal("Do widzenia", rows[2].TranslatedText);
        Assert.Equal("speaker-2", rows[1].SpeakerId);
        Assert.Equal("Alex", rows[1].Actor);
        Assert.Equal(SpeakerGender.Unknown, rows[1].Gender);

        using var document = JsonDocument.Parse(request!);
        Assert.Equal("Earlier", document.RootElement.GetProperty("history")[0].GetProperty("source").GetString());
        Assert.Equal("speaker-2", document.RootElement.GetProperty("lines")[0].GetProperty("speakerId").GetString());
        Assert.Equal("Unknown", document.RootElement.GetProperty("lines")[0].GetProperty("gender").GetString());
    }

    [Fact]
    public async Task IncompleteBatchIsNotPartiallyAppliedBeforeSmallerRetry()
    {
        var rows = new ObservableCollection<TranslateRow>
        {
            new() { Text = "One" },
            new() { Text = "Two" },
        };
        var requests = 0;

        var translated = await ContextBatchTranslationRunner.TranslateBatchAsync(
            rows,
            0,
            (content, _) =>
            {
                requests++;
                if (requests <= 2)
                {
                    Assert.Equal(string.Empty, rows[0].TranslatedText);
                    Assert.Equal(string.Empty, rows[1].TranslatedText);
                    return Task.FromResult("{\"1\":\"Jeden\"}");
                }

                using var document = JsonDocument.Parse(content);
                Assert.Single(document.RootElement.GetProperty("lines").EnumerateArray());
                return Task.FromResult("{\"1\":\"Jeden\"}");
            },
            CancellationToken.None);

        Assert.Equal(1, translated);
        Assert.Equal(3, requests);
        Assert.Equal("Jeden", rows[0].TranslatedText);
        Assert.Equal(string.Empty, rows[1].TranslatedText);
    }
}
