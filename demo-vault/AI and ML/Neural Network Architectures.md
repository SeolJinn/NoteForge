# Neural Network Architectures

A loose taxonomy of the architectures I keep running into. Companion to [[Machine Learning]] and [[Transformers and Attention]].

## Feedforward (MLP)

Stacked linear layers with nonlinearities. Universal function approximator in theory, painfully sample-hungry in practice. Still the right baseline for tabular data.

## Convolutional

Weight-sharing over spatial neighborhoods. Inductive bias: nearby pixels are correlated, features are translation-invariant. Used to be the dominant choice for vision; transformers are eating that lunch but ConvNets still win on small datasets.

## Recurrent (RNN, LSTM, GRU)

Sequential state. Beautiful in theory, hard to train (vanishing gradients), and largely superseded by attention for language modeling. LSTMs still useful for streaming inference where you can't afford a full attention pass.

## Attention-Based

The current winner for almost everything that isn't strictly local. Quadratic in sequence length, which is the active research frontier. See [[Transformers and Attention]].

## Graph Neural Networks

Message passing over node neighborhoods. Niche but indispensable for molecule property prediction, social graphs, and knowledge bases.

## Picking One

For a new problem I default to: tabular → gradient boosting (not even a NN); images → start with a small ConvNet, escalate to a vision transformer if data is plentiful; text → pretrained transformer, fine-tune.
