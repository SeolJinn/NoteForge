# Machine Learning

Quick reference: **machine learning** notes from building a digit classifier end-to-end without a high-level framework. See also [[Neural Network Architectures]] and [[Embeddings and Vector Search]].

## Architecture

Three fully-connected layers, ReLU activations between them, softmax at the output. Input is flattened 28x28 grayscale, output is a 10-way distribution over digits.

## Forward Pass

For each layer: `z = Wx + b`, then activation. Cache the pre-activation values — you'll need them for the backward pass.

## Backpropagation

Chain rule, layer by layer, right to left. The gradient of the loss with respect to each weight matrix is what we want. Cross-entropy loss paired with softmax simplifies the output-layer gradient: it collapses to `predicted - target`.

## Optimization

Plain stochastic gradient descent first, then Adam once the basics work. Tune the step size by eyeballing the loss curve — if it diverges, halve it; if it crawls, double it.

## Regularization

Dropout on the hidden layers (p=0.2) and weight decay on the linear layers. Without these the validation accuracy plateaus early while training loss keeps falling — textbook overfitting.

## What Actually Worked

- Normalize inputs to zero-mean unit-variance. Forgetting this cost me an afternoon.
- Initialize weights with He initialization for ReLU layers, not plain Gaussian.
- Shuffle the training set every epoch.
- Keep a held-out validation split. The training loss lies.

## Open Questions

- Why does batch size 32 generalize better than 256 on this dataset?
- Convolutional layers should help — try replacing the first linear layer with a small CNN.
