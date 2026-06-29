# Tokenization and Subwords

The invisible layer that decides what a model can even see. Feeds into [[Transformers and Attention]].

## Why Not Just Words
A whole-word vocabulary explodes and still can't handle a word it never saw. Character-level handles anything but throws away all the useful structure. Subwords are the compromise that won.

## How BPE Works
Byte pair encoding starts from characters and greedily merges the most frequent adjacent pair, over and over, until you hit your vocab size. Common words end up as single tokens, rare words get chopped into pieces. "tokenization" might be one token while "antidisestablishment" is five.

## The Gotchas
- Token count is not word count. A rule of thumb is roughly 4 characters per token in English, much worse for other languages.
- Numbers and code tokenize badly, which is half of why models fumble arithmetic.
- A leading space is part of the token. " the" and "the" are different.

## Why I Care
Your context limit is measured in tokens, your bill is measured in tokens, and weird model failures often live right here at the boundary nobody looks at.
