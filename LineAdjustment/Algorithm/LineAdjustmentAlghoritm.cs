using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using Serilog;

namespace LineAdjustment.Algorithm;

internal sealed class LineAdjustmentAlgorithm : IDisposable
{
#if DEBUG
    private const int ProcessingThreadsCount = 1;
#else
    private const int ProcessingThreadsCount = 6;
#endif

    private static ImmutableSortedSet<Line> _stringSortedSet =
        ImmutableSortedSet.Create<Line>(new AdjustedLineComparer());

    private BlockingCollection<Line> _linesQueue;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _token;
#if DEBUG
    private const char SplitSymbol = '+';
#else
    private const char SplitSymbol = ' ';
#endif

    public string Transform(string text, int lineWidth, CancellationToken token = default)
    {
        try
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (lineWidth <= 0) throw new ArgumentOutOfRangeException(nameof(lineWidth));

            if (_cancellationTokenSource != default)
                CleanUp();

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _token = _cancellationTokenSource.Token;

            _linesQueue = new BlockingCollection<Line>();
            for (var i = 0; i < ProcessingThreadsCount; i++)
                Task.Factory.StartNew(LinesProcessing, TaskCreationOptions.LongRunning);

            var linesCount = SplitTextToLinesAndSendToProcessing(text, lineWidth, token);
            WaitProcessingCompletion(linesCount);
            return Join(_stringSortedSet);
        }
        finally
        {
            _stringSortedSet = _stringSortedSet.Clear();
        }
    }

    private void WaitProcessingCompletion(int linesCount)
    {
        while (_stringSortedSet.Count != linesCount)
            Thread.Sleep(1);
    }

    private void LinesProcessing()
    {
        try
        {
            foreach (var lineToProcess in _linesQueue.GetConsumingEnumerable(_token))
            {
                lineToProcess.Adjusted = AdjustString(lineToProcess.Transformation);
                ImmutableInterlocked.Update(ref _stringSortedSet,
                    (collection, item) => collection.Add(item),
                    lineToProcess);
            }
        }
        catch (OperationCanceledException)
        {
            //ignored
        }
        catch (ObjectDisposedException)
        {
            //ignored
        }
        catch (Exception ex)
        {
            Log.Error("Can't process line. Ex: " + ex);
        }
    }

    private static string AdjustString(LineTransform transformation)
    {
        string ConcatWordsWithSpaceBetween(IReadOnlyList<string> words, int wordsCount, int whiteSpaceCount,
            int resultOfDivision = 0)
        {
            var sb = new StringBuilder();
            for (var index = 0; index < wordsCount; index++)
            {
                var word = words[index];
                sb.Append(word);

                if (index == wordsCount - 1)
                    continue;

                switch (resultOfDivision)
                {
                    case > 0:
                        sb.Append(SplitSymbol, whiteSpaceCount + 1);
                        resultOfDivision--;
                        break;
                    default:
                        sb.Append(SplitSymbol, whiteSpaceCount);
                        break;
                }
            }

            return sb.ToString();
        }

        if (transformation == null) throw new ArgumentNullException(nameof(transformation));

        var words = transformation.Words;
        var lineBreakWidth = transformation.LineBreakWidth;
        var wordsTextLength = transformation.WordsTextLength;

        var wordsCount = words.Length;
        var intervalsBetweenWords = wordsCount - 1;
        var emptySpaceInLine = wordsTextLength + intervalsBetweenWords - lineBreakWidth;

        switch (emptySpaceInLine)
        {
            case > 0 when wordsCount > 1: // In this case, there should be only one word
                throw new InvalidDataException("words.Length > 1");
            case > 0: // long word
                return words.Single();
            case 0: // "...Расстояние между словами нужно заполнять равным количеством пробелов..."
                return ConcatWordsWithSpaceBetween(words, wordsCount, 1);
            default: //standard case
                var spaceInLineNeedFeel = Math.Abs(emptySpaceInLine) + intervalsBetweenWords;
                if (wordsCount ==
                    1) //"...Если в строке помещается только 1 слово, то дополнить строку пробелами справа..."
                {
                    var sb = new StringBuilder();
                    return sb.Append(words.Single())
                        .Append(SplitSymbol, spaceInLineNeedFeel)
                        .ToString();
                }
                else
                {
                    var resultOfDivision = spaceInLineNeedFeel % intervalsBetweenWords;
                    var whiteSpaceCount = spaceInLineNeedFeel / intervalsBetweenWords;
                    return ConcatWordsWithSpaceBetween(words, wordsCount, whiteSpaceCount, resultOfDivision);
                }
        }
    }

    private int SplitTextToLinesAndSendToProcessing(string text, int lineWidth,
        CancellationToken cancellationToken = default)
    {
        void SendToQueue(List<string> list, int currentLineLength1, int lineIndex1)
        {
            var lineToProcess = new Line
            {
                Transformation = new LineTransform
                {
                    Words = list.ToArray(),
                    LineBreakWidth = lineWidth,
                    WordsTextLength = currentLineLength1
                },
                Index = lineIndex1
            };

            if (!_linesQueue.TryAdd(lineToProcess))
                throw new Exception("Can't send line to adjust processing");
        }

        try
        {
            var words = text
                .Split(default)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            var lineWords = new List<string>();
            var currentLineLength = 0;
            var lineIndex = 0;

            foreach (var word in words)
            {
                if (cancellationToken.IsCancellationRequested)
                    return default;

                var wordLength = word.Length;

                if (currentLineLength > 0 &&
                    currentLineLength + wordLength + lineWords.Count >
                    lineWidth) //does not fit taking into account spaces inside words
                {
                    SendToQueue(lineWords, currentLineLength, lineIndex);

                    //CleanUp
                    lineWords.Clear();
                    currentLineLength = 0;

                    lineIndex++;
                }

                lineWords.Add(word);
                currentLineLength += wordLength;
            }

            if (lineWords.Any())
                SendToQueue(lineWords, currentLineLength, lineIndex);

            return lineIndex + 1;
        }
        finally
        {
            _linesQueue.CompleteAdding();
        }
    }


    private static string Join(ImmutableSortedSet<Line> sortedLines)
    {
        return string.Join(Environment.NewLine, sortedLines.Select(l => l.Adjusted));
    }

    private void CleanUp()
    {
        _cancellationTokenSource?.Dispose();

        if (!_linesQueue.IsAddingCompleted)
            _linesQueue.CompleteAdding();

        _linesQueue.Dispose();
        _stringSortedSet = _stringSortedSet.Clear();
    }

    public void Dispose()
    {
        CleanUp();
    }
}