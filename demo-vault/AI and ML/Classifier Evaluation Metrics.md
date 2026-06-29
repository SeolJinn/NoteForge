# Classifier Evaluation Metrics

Accuracy lies on imbalanced data. This is the note I send people who quote it anyway. See also [[Overfitting and Regularization]].

## Why Accuracy Fails
If 99% of your data is one class, a model that always predicts that class is 99% accurate and completely useless. Accuracy hides the failures that matter.

## Precision and Recall
- **Precision.** Of the things I flagged positive, how many were right? Punishes false alarms.
- **Recall.** Of all the real positives, how many did I catch? Punishes misses.
- They trade off. Tighten the threshold and precision rises while recall falls.

## F1 and the Threshold
F1 is the harmonic mean of precision and recall, so both have to be decent for the score to be. I use it when I need a single number but care about both sides.

## ROC and AUC
The ROC curve sweeps every threshold, plotting true positive rate against false positive rate. AUC summarizes it as one number: 0.5 is a coin flip, 1.0 is perfect. Good for comparing models, but on heavy imbalance I trust the precision-recall curve more.
