# Diffusion Models

How you generate an image by learning to undo noise. Still the cleverest idea I've met in generative ML.

## The Core Trick
Take a real image and add Gaussian noise step by step until it's pure static. Then train a network to reverse one step at a time. Generation is just starting from noise and running the reverse process. Sculpting an image out of fog.

## Why It Beat GANs
- No adversarial training, so no mode collapse and no two-network instability.
- The loss is a simple denoising objective. Stable to train, which matters more than people admit.
- The tradeoff is speed. Sampling needs many steps where a GAN needs one.

## Making It Practical
Latent diffusion runs the whole process in a compressed latent space instead of raw pixels. That's the trick that made it cheap enough to run on consumer hardware. Stable Diffusion is this.

## Steering the Output
Classifier-free guidance is how text prompts actually bite. You push the sample toward the conditioned prediction and away from the unconditioned one. Crank the guidance scale too high and you get oversaturated, fried-looking images.
