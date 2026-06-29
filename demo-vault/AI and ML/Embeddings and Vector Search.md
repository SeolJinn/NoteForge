# Embeddings and Vector Search

Dense vector representations and how to search them at scale. Companion to [[Transformers and Attention]] and [[API Design Notes]].

## What an Embedding Is

A fixed-dimensional vector produced by a model such that semantically similar inputs land near each other in vector space. "Near" almost always means cosine similarity in practice.

## Cosine vs Dot Product vs Euclidean

For normalized vectors all three rank identically. Most production embedding APIs return normalized vectors so you can pick whichever the index supports.

## Index Types

- **Flat / brute-force.** Linear scan. Fine up to a few hundred thousand vectors.
- **HNSW.** Graph-based, log-ish query time, the current default for in-memory ANN.
- **IVF.** Cluster-then-search. Better for billion-scale on disk.
- **PQ.** Quantize for memory savings, accept recall loss.

## Hybrid with Lexical

Pure vector search misses exact identifier matches and recent jargon. The standard fix is fusion with BM25 or TF-IDF. NoteForge does this with a harmonic mean — both signals must agree before a result is kept.

## Common Mistakes

- Mixing embeddings from different models in the same index. They don't share a coordinate system.
- Forgetting to normalize when the index assumes unit-length vectors.
- Treating cosine similarity as a probability. It isn't one.
