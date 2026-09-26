using System.Collections.ObjectModel;
using System.Net.Http;
using Nikse.SubtitleEdit.UiLogic.AutoTranslate;

namespace Nikse.SubtitleEdit.UiLogic.Translate;

public static class ContextBatchTranslationRunner
{
    public static Task<int> TranslateBatchAsync(
        ObservableCollection<TranslateRow> rows,
        int index,
        Func<string, CancellationToken, Task<string>> sendAsync,
        CancellationToken cancellationToken,
        int batchSize = 10,
        int historySize = 8)
    {
        if (MergeAndSplitHelper.IsKeptUntranslated(rows[index].Text))
        {
            rows[index].TranslatedText = rows[index].Text;
            return Task.FromResult(1);
        }

        var count = 1;
        var maxCount = Math.Min(Math.Clamp(batchSize, 1, 50), rows.Count - index);
        while (count < maxCount && !MergeAndSplitHelper.IsKeptUntranslated(rows[index + count].Text))
        {
            count++;
        }

        return TranslateChunkAsync(rows, index, count, sendAsync, cancellationToken, historySize);
    }

    private static async Task<int> TranslateChunkAsync(
        ObservableCollection<TranslateRow> rows,
        int index,
        int count,
        Func<string, CancellationToken, Task<string>> sendAsync,
        CancellationToken cancellationToken,
        int historySize)
    {
        var lines = new List<ContextBatchTranslationProtocol.Line>(count);
        var stripped = new StrippedLine[count];
        for (var i = 0; i < count; i++)
        {
            var row = rows[index + i];
            stripped[i] = StrippedLine.Strip(row.Text);
            lines.Add(new ContextBatchTranslationProtocol.Line(
                i + 1, stripped[i].Text, row.SpeakerId, row.Actor, row.Gender));
        }

        var history = CollectHistory(rows, index, historySize);
        var userContent = ContextBatchTranslationProtocol.BuildUserContent(history, lines);
        Dictionary<int, string> map = new();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reply = await sendAsync(userContent, cancellationToken);
            map = ContextBatchTranslationProtocol.ParseTranslations(reply);
            if (ContextBatchTranslationProtocol.IsComplete(map, lines))
            {
                for (var i = 0; i < count; i++)
                {
                    var translation = map[i + 1];
                    rows[index + i].TranslatedText = translation.Length > 0
                        ? stripped[i].Restore(translation)
                        : rows[index + i].Text;
                }

                return count;
            }
        }

        if (count > 1)
        {
            return await TranslateChunkAsync(rows, index, count / 2, sendAsync, cancellationToken, historySize);
        }

        throw new HttpRequestException("The translation service returned no complete numbered batch");
    }

    private static List<ContextBatchTranslationProtocol.HistoryItem> CollectHistory(
        ObservableCollection<TranslateRow> rows,
        int index,
        int historySize)
    {
        var history = new List<ContextBatchTranslationProtocol.HistoryItem>();
        for (var i = index - 1; i >= 0 && history.Count < Math.Clamp(historySize, 0, 50); i--)
        {
            if (string.IsNullOrEmpty(rows[i].TranslatedText))
            {
                continue;
            }

            var source = StrippedLine.Strip(rows[i].Text).Text;
            var translation = StrippedLine.Strip(rows[i].TranslatedText).Text;
            if (source.Length > 0 && translation.Length > 0)
            {
                history.Add(new ContextBatchTranslationProtocol.HistoryItem(
                    source, translation, rows[i].SpeakerId, rows[i].Actor, rows[i].Gender));
            }
        }

        history.Reverse();
        return history;
    }
}
