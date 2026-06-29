# Retrieval Augmented Generation

Giving a model an open-book exam instead of making it memorize. Built on [[Embeddings and Vector Search]], and the better answer when [[Fine-Tuning vs Prompting]] tips toward facts.

## The Problem It Solves
Models hallucinate when asked about things outside their training data, and they have no idea they're doing it. RAG fixes this by fetching real documents at query time and stuffing them into the context, so the answer is grounded in something you can cite.

## How It Actually Runs
1. Chunk your documents and turn each chunk into an embedding.
2. Store the vectors in an index for fast similarity search.
3. At query time, embed the question, pull the nearest chunks, and prepend them to the prompt.

## Where It Goes Wrong
- **Chunking.** Too big and you bury the signal, too small and you lose context. 200 to 500 tokens with overlap is my usual start.
- **Retrieval quality is the ceiling.** Garbage chunks in, confident garbage out. The model can't fix a bad fetch.

## My Take
Most RAG failures are retrieval failures wearing a generation costume. Fix the search before you blame the LLM.
