using System;
using System.Collections.Generic;
using System.Linq;
using NoteForge.Models;
using StopWord;

namespace NoteForge.Services.Search;

public class TfidfCalculator
{
    private readonly Dictionary<string, int> _documentFrequency = [];
    private readonly Dictionary<string, Dictionary<string, double>> _termFrequency = [];
    private readonly Dictionary<string, int> _documentLengths = [];
    private int _totalDocuments;
    private double _avgDocumentLength;

    private static readonly HashSet<string> Stopwords = LoadStopwords();

    private static HashSet<string> LoadStopwords()
    {
        var stopwords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var enWords = StopWords.GetStopWords("en");
        foreach (var word in enWords)
        {
            stopwords.Add(word);
        }

        return stopwords;
    }

    public void BuildIndex(IEnumerable<Note> notes)
    {
        _documentFrequency.Clear();
        _termFrequency.Clear();
        _documentLengths.Clear();

        List<Note> notesList = [.. notes];
        _totalDocuments = notesList.Count;

        foreach (var note in notesList)
        {
            var terms = Tokenize(note.Text);
            var uniqueTerms = new HashSet<string>();

            _documentLengths[note.FilePath] = terms.Count;

            var termCounts = new Dictionary<string, int>();
            foreach (var term in terms)
            {
                uniqueTerms.Add(term);
                termCounts[term] = termCounts.GetValueOrDefault(term, 0) + 1;
            }

            var maxFreq = termCounts.Values.DefaultIfEmpty(0).Max();
            var tf = new Dictionary<string, double>();

            foreach (var (term, count) in termCounts)
            {
                tf[term] = 0.5 + (0.5 * count / maxFreq);
            }

            _termFrequency[note.FilePath] = tf;

            foreach (var term in uniqueTerms)
            {
                _documentFrequency[term] = _documentFrequency.GetValueOrDefault(term, 0) + 1;
            }
        }

        _avgDocumentLength = _documentLengths.Values.DefaultIfEmpty(0).Average();
    }

    public Dictionary<string, Dictionary<string, double>> GetTfidfVectors()
    {
        Dictionary<string, Dictionary<string, double>> vectors = [];

        foreach (var (filePath, tf) in _termFrequency)
        {
            Dictionary<string, double> vector = [];

            foreach (var (term, termFreq) in tf)
            {
                if (_totalDocuments >= 3
                    && _documentFrequency.TryGetValue(term, out var df)
                    && df * 2 > _totalDocuments)
                    continue;

                var idf = CalculateIdf(term);
                var weight = termFreq * idf;
                if (weight > 0)
                    vector[term] = weight;
            }

            if (_documentLengths.TryGetValue(filePath, out var docLength) && _avgDocumentLength > 0)
            {
                var lengthNorm = 1.0 / (1.0 + Math.Log(1.0 + (docLength / _avgDocumentLength)));
                foreach (var term in vector.Keys)
                {
                    vector[term] *= lengthNorm;
                }
            }

            vectors[filePath] = vector;
        }

        return vectors;
    }

    public static double SparseCosineSimilarity(Dictionary<string, double> a, Dictionary<string, double> b)
    {
        if (a.Count is 0 || b.Count is 0)
            return 0;

        var (smaller, larger) = a.Count <= b.Count ? (a, b) : (b, a);

        double dotProduct = 0;
        foreach (var (term, weight) in smaller)
        {
            if (larger.TryGetValue(term, out var otherWeight))
                dotProduct += weight * otherWeight;
        }

        if (dotProduct is 0)
            return 0;

        double magnitudeA = 0;
        foreach (var weight in a.Values)
            magnitudeA += weight * weight;

        double magnitudeB = 0;
        foreach (var weight in b.Values)
            magnitudeB += weight * weight;

        return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }

    public List<(Note note, double score)> Search(IEnumerable<Note> notes, string query)
    {
        var queryTerms = Tokenize(query);
        if (queryTerms.Count == 0)
            return [];

        List<(Note note, double score)> scores = [];

        foreach (var note in notes)
        {
            if (!_termFrequency.ContainsKey(note.FilePath))
                continue;

            var score = CalculateTfidfScore(note.FilePath, queryTerms);
            if (score > 0)
            {
                scores.Add((note, score));
            }
        }

        var maxScore = scores.Count > 0 ? scores.Max(s => s.score) : 1.0;
        return [.. scores.Select(s => (s.note, s.score / maxScore))];
    }

    private double CalculateTfidfScore(string filePath, List<string> queryTerms)
    {
        if (!_termFrequency.TryGetValue(filePath, out var tf))
            return 0;

        double score = 0;

        foreach (var term in queryTerms)
        {
            if (tf.TryGetValue(term, out var termFreq))
            {
                var idf = CalculateIdf(term);
                score += termFreq * idf;
            }
            else if (term.Length >= 3)
            {
                var bestScore = 0.0;

                foreach (var (token, tokenFreq) in tf)
                {
                    if (!token.StartsWith(term, StringComparison.Ordinal))
                        continue;

                    var discount = (double)term.Length / token.Length;
                    var candidate = tokenFreq * discount;

                    if (candidate > bestScore)
                        bestScore = candidate;
                }

                if (bestScore > 0)
                {
                    var idf = CalculateIdf(term);
                    score += bestScore * idf;
                }
            }
        }

        if (_documentLengths.TryGetValue(filePath, out var docLength) && _avgDocumentLength > 0)
        {
            var lengthNorm = 1.0 / (1.0 + Math.Log(1.0 + (docLength / _avgDocumentLength)));
            score *= lengthNorm;
        }

        return score;
    }

    private double CalculateIdf(string term)
    {
        if (_documentFrequency.TryGetValue(term, out var docFreq))
            return Math.Log((double)(_totalDocuments + 1) / (docFreq + 1)) + 1;

        if (term.Length < 3)
            return 0;

        var summedFreq = 0;
        foreach (var (token, freq) in _documentFrequency)
        {
            if (token.StartsWith(term, StringComparison.Ordinal))
                summedFreq += freq;
        }

        if (summedFreq is 0)
            return 0;

        var cappedFreq = Math.Min(summedFreq, _totalDocuments);
        return Math.Log((double)(_totalDocuments + 1) / (cappedFreq + 1)) + 1;
    }

    private List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return [.. text
            .ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '_', '/', '\\', '#', '*', '`'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(t => IsValidToken(t))];
    }

    private bool IsValidToken(string token)
    {
        if (Stopwords.Contains(token))
            return false;

        if (token.Length >= 3)
            return true;

        if (token.Length == 2)
        {
            return token.Any(char.IsDigit) || token.All(char.IsLetter);
        }

        return false;
    }
}
