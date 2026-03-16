using System;
using System.Collections.Generic;
using System.Linq;

namespace NoteForge.Services.Embeddings;

public static class VectorMath
{
    public static float[] Normalize(float[] vector)
    {
        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude == 0)
            return vector;

        var normalized = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            normalized[i] = vector[i] / magnitude;
        }
        return normalized;
    }

    public static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must be same length");

        float dotProduct = 0;
        float magnitudeA = 0;
        float magnitudeB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            magnitudeA += a[i] * a[i];
            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;

        return dotProduct / (MathF.Sqrt(magnitudeA) * MathF.Sqrt(magnitudeB));
    }

    public static double HarmonicMean(double a, double b)
    {
        if (a + b is 0)
            return 0;

        return 2.0 * (a * b) / (a + b);
    }

    public static List<(T item, float score)> RankBySimilarity<T>(
        float[] queryEmbedding,
        IEnumerable<(T item, float[] embedding)> candidates,
        float minThreshold = 0.3f,
        int maxResults = 50)
    {
        List<(T item, float score)> results = [];

        foreach (var (item, embedding) in candidates)
        {
            var similarity = CosineSimilarity(queryEmbedding, embedding);
            if (similarity >= minThreshold)
            {
                results.Add((item, similarity));
            }
        }

        return [.. results.OrderByDescending(x => x.score).Take(maxResults)];
    }
}
