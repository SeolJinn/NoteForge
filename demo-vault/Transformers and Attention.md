# Transformers and Attention

Self-attention is the operation that took over deep learning for sequences. Pairs with [[Neural Network Architectures]] and feeds [[Embeddings and Vector Search]].

## Scaled Dot-Product Attention

Three projections of the input — queries, keys, values. Compute `softmax(QK^T / sqrt(d_k)) V`. The scaling by `sqrt(d_k)` keeps the softmax from saturating when dimensions get large.

## Multi-Head

Run attention in parallel with smaller per-head dimensions, concatenate, project. Each head can specialize: some attend to syntactic neighbors, others to long-range coreference, others to nothing useful at all.

## Positional Encoding

Self-attention is permutation-invariant by default. Add positional information either through fixed sinusoidal encodings or learned position embeddings. Rotary positional embeddings (RoPE) have largely won at scale.

## Causal Masking

For autoregressive models, mask out future positions before the softmax so each token only attends to itself and earlier tokens. This is the difference between an encoder block and a decoder block.

## What Surprised Me

Attention weights look interpretable but mostly aren't — heads encode redundant or composite information. Don't read too much into a heatmap.

The bigger the model, the more attention sinks emerge: tokens that everything attends to as a kind of null option. Quantization breaks badly if you don't preserve these.
