# Gradient Descent and Optimizers

How models actually learn, once you strip away the mystique. Pairs well with [[Overfitting and Regularization]].

## The Plain Version
You have a loss surface and you want to go downhill. The gradient points uphill, so you step the other way. The learning rate decides how big the step is, and almost every training disaster traces back to getting it wrong.

## Why Plain SGD Isn't Enough
- Pure stochastic gradient descent is noisy and slow to converge on anything ravine-shaped.
- Momentum fixes most of it. Accumulate a running velocity so you blow through small bumps and dampen oscillation across steep walls.

## What I Actually Reach For
- **AdamW** for almost everything. Adaptive per-parameter rates plus decoupled weight decay. The default that's hard to beat.
- Plain SGD with momentum still wins on large vision models if you can afford to tune the schedule.

## Learning Rate Is the Whole Game
Warmup for the first few hundred steps, then cosine decay. I've fixed more "broken" models by lowering the LR than by any architecture change. If loss spikes to NaN, the rate was too high. Full stop.
