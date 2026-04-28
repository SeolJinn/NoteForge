using System;

namespace NoteForge.Services.Ai;

public sealed class AiDisabledException() : InvalidOperationException("AI is disabled. Enable a provider in Settings.");
