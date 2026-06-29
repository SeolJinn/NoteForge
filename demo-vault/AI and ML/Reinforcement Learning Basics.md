# Reinforcement Learning Basics

Notes from working through the Sutton & Barto first chapters again.

## The Setting

Agent, environment, state, action, reward. The agent picks actions to maximize cumulative discounted reward. Everything else is detail.

## Value Functions

State-value `V(s)` is the expected return starting from state `s`. Action-value `Q(s,a)` is the expected return starting from `s` and taking action `a`. The Bellman equations relate `V` and `Q` recursively.

## Exploration vs Exploitation

The fundamental tension. Greedy policies exploit known good actions; exploration is required to find better ones. Epsilon-greedy is the dumb baseline that often works. UCB and Thompson sampling are smarter when sample budget matters.

## Policy Gradient

Directly parameterize the policy and ascend its gradient. REINFORCE is the simplest form, has high variance, gets fixed by adding a baseline. Actor-critic methods combine policy gradient with a learned value estimate.

## Deep RL Sharp Edges

Off-policy methods diverge silently when the value estimate goes off-distribution. Reward shaping helps and also lies — the agent will exploit it. Always check the actual policy behavior, not just the reward curve.

## What I Want to Try Next

A short experiment with PPO on a continuous-control task. Maybe inverted pendulum from scratch — small enough to debug, hard enough to teach me something.
