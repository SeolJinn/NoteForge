# Overfitting and Regularization

The moment your model memorizes instead of learns. Companion to [[Gradient Descent and Optimizers]] and [[Classifier Evaluation Metrics]].

## How You Spot It
Training loss keeps dropping while validation loss bottoms out and starts climbing. That divergence is the whole signal. If you only watch training loss you will fool yourself every single time.

## The Tools That Work
- **Early stopping.** Cheap, brutal, effective. Watch validation, stop when it turns.
- **Dropout** around 0.1 to 0.3. Randomly zero activations so no single neuron gets too important.
- **Weight decay.** Penalize big weights, prefer simpler functions.

## More Data Beats Cleverness
Every fancy regularizer is a substitute for data you don't have. When I can get more labeled examples, that helps more than any tweak. Augmentation is the next best thing.

## My Rule of Thumb
If the gap between train and validation accuracy is more than a few points, I stop adding capacity and start adding constraints.
