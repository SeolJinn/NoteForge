using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NoteForge.Services.Embeddings;

public class EmbeddingDebugHelper
{
    private readonly OllamaService _ollamaService;
    private readonly ILogger<EmbeddingDebugHelper> _logger;

    public EmbeddingDebugHelper(OllamaService ollamaService, ILogger<EmbeddingDebugHelper> logger)
    {
        _ollamaService = ollamaService;
        _logger = logger;
    }

    public async Task RunDiagnosticsAsync()
    {
        _logger.LogInformation("=== Embedding System Diagnostics ===");

        await TestOllamaConnection();
        await TestEmbeddingGeneration();
        await TestEmbeddingStorage();
        await TestSimilarityCalculation();
    }

    private async Task TestOllamaConnection()
    {
        _logger.LogInformation("1. Testing Ollama connection...");
        try
        {
            var testEmbedding = await _ollamaService.GenerateEmbeddingAsync("test");
            if (testEmbedding == null)
            {
                _logger.LogError("✗ Ollama returned null embedding - model may not be loaded");
            }
            else
            {
                _logger.LogInformation($"✓ Ollama connected - embedding dimension: {testEmbedding.Length}");
                _logger.LogInformation($"  First 5 values: [{string.Join(", ", testEmbedding.Take(5).Select(v => v.ToString("F4")))}]");
                _logger.LogInformation($"  Magnitude: {Math.Sqrt(testEmbedding.Sum(v => v * v)):F4}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Ollama connection failed");
        }
    }

    private async Task TestEmbeddingGeneration()
    {
        _logger.LogInformation("2. Testing embedding generation for similar/different texts...");

        var text1 = "This is about markdown formatting";
        var text2 = "Markdown is a lightweight markup language";
        var text3 = "The weather is sunny today";

        var emb1 = await _ollamaService.GenerateEmbeddingAsync(text1);
        var emb2 = await _ollamaService.GenerateEmbeddingAsync(text2);
        var emb3 = await _ollamaService.GenerateEmbeddingAsync(text3);

        if (emb1 != null && emb2 != null && emb3 != null)
        {
            var sim12 = VectorMath.CosineSimilarity(emb1, emb2);
            var sim13 = VectorMath.CosineSimilarity(emb1, emb3);
            var sim23 = VectorMath.CosineSimilarity(emb2, emb3);

            _logger.LogInformation($"  Similarity (markdown texts): {sim12:F4}");
            _logger.LogInformation($"  Similarity (markdown vs weather): {sim13:F4}");
            _logger.LogInformation($"  Similarity (markdown vs weather #2): {sim23:F4}");

            if (sim12 > sim13)
            {
                _logger.LogInformation("✓ Related texts have higher similarity than unrelated");
            }
            else
            {
                _logger.LogWarning("✗ Similarity scores seem inverted or random");
            }
        }
    }

    private async Task TestEmbeddingStorage()
    {
        _logger.LogInformation("3. Testing embedding storage...");

        if (App.EmbeddingRepository == null)
        {
            _logger.LogWarning("✗ EmbeddingRepository not initialized (no vault loaded?)");
            return;
        }

        try
        {
            var allEmbeddings = await App.EmbeddingRepository.GetAllEmbeddingsAsync();
            _logger.LogInformation($"✓ Found {allEmbeddings.Count} embeddings in database");

            if (allEmbeddings.Count > 0)
            {
                var sample = allEmbeddings.First();
                _logger.LogInformation($"  Sample: {System.IO.Path.GetFileName(sample.FilePath)}");
                _logger.LogInformation($"  Dimension: {sample.Embedding.Length}");
                _logger.LogInformation($"  First 5 values: [{string.Join(", ", sample.Embedding.Take(5).Select(v => v.ToString("F4")))}]");
                _logger.LogInformation($"  Magnitude: {Math.Sqrt(sample.Embedding.Sum(v => v * v)):F4}");

                var allSame = allEmbeddings.All(e =>
                    e.Embedding.Take(5).SequenceEqual(sample.Embedding.Take(5)));
                if (allSame)
                {
                    _logger.LogError("✗ WARNING: All embeddings appear identical!");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Error accessing embedding storage");
        }
    }

    private async Task TestSimilarityCalculation()
    {
        _logger.LogInformation("4. Testing similarity calculation...");

        float[] vec1 = { 1.0f, 0.0f, 0.0f };
        float[] vec2 = { 1.0f, 0.0f, 0.0f };
        float[] vec3 = { 0.0f, 1.0f, 0.0f };
        float[] vec4 = { -1.0f, 0.0f, 0.0f };

        var simIdentical = VectorMath.CosineSimilarity(vec1, vec2);
        var simOrthogonal = VectorMath.CosineSimilarity(vec1, vec3);
        var simOpposite = VectorMath.CosineSimilarity(vec1, vec4);

        _logger.LogInformation($"  Identical vectors: {simIdentical:F4} (should be 1.0)");
        _logger.LogInformation($"  Orthogonal vectors: {simOrthogonal:F4} (should be 0.0)");
        _logger.LogInformation($"  Opposite vectors: {simOpposite:F4} (should be -1.0)");

        if (Math.Abs(simIdentical - 1.0f) < 0.01f &&
            Math.Abs(simOrthogonal) < 0.01f &&
            Math.Abs(simOpposite - (-1.0f)) < 0.01f)
        {
            _logger.LogInformation("✓ Similarity calculation working correctly");
        }
        else
        {
            _logger.LogError("✗ Similarity calculation appears broken");
        }

        await Task.CompletedTask;
    }
}
