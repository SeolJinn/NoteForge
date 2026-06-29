# Convolutional Networks

The architecture that taught machines to see. Companion to [[Neural Network Architectures]].

## The One Idea
Slide a small learnable filter across the image and reuse the same weights everywhere. A cat in the corner and a cat in the center share the same edge detectors. That weight sharing is why CNNs need far less data than a fully connected net would.

## The Layers That Matter
- **Convolution.** Detects local patterns. Early layers find edges, deeper layers find eyes and wheels.
- **Pooling.** Downsamples so the network gets a wider view and shrugs off small shifts.
- **Stacking.** Depth builds the hierarchy. Each layer composes features from the one below.

## Why ResNet Changed Things
Past a certain depth, plain networks got worse, not better. Residual connections let gradients skip layers, and suddenly 50 and 100 layer networks trained fine. Nearly everything since borrows the trick.

## Where They Stand Now
Transformers came for vision and won a lot of benchmarks. But for small data and tight compute budgets the convolutional prior still earns its keep.
