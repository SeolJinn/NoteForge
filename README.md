# NoteForge

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2B-0078D6.svg)](#)

NoteForge is a Windows desktop application for managing markdown notes in vault-based workspaces. It combines a live inline markdown editor with tabbed navigation, a force-directed knowledge graph, and AI-powered summarization and semantic search — all running locally against an Ollama instance, so your notes never leave your machine.

<!-- TODO: replace with an actual screenshot -->
<!-- ![NoteForge screenshot](docs/screenshots/main.png) -->

## Features

- **Vault-based workspaces.** Organize notes as plain `.md` files in a folder of your choice. Switch between multiple vaults without losing state.
- **Inline markdown editor.** CodeMirror 6 renders markdown live — headings, bold, italic, lists, links, tables, math, footnotes, code blocks, task checkboxes, and wiki-links (`[[note name]]`) all preview in place with syntax markers revealed only on the active line.
- **Tabbed editing.** Work on several notes at once with draggable, reorderable tabs.
- **Folder tree + favorites.** Nested folders, drag-and-drop between folders, and a favorites section for quick access.
- **Knowledge graph.** Physics-based force-directed view of links between your notes, with both explicit (markdown / wiki) and semantic (embedding-derived) edges.
- **Hybrid search.** TF-IDF keyword search combined with semantic embedding search via harmonic-mean scoring — finds both exact matches and conceptually related notes.
- **AI summaries.** Generate a summary of the current note with a single click, streamed token-by-token from a local Ollama model.
- **Fully local.** No telemetry, no cloud sync, no account. Everything runs on your machine.

## Install

Download the latest installer from the [Releases page](https://github.com/SeolJinn/NoteForge/releases/latest) and run it. Windows SmartScreen may warn about an unrecognized publisher — click **More info** → **Run anyway**. The app installs per-user (no UAC prompt needed) and adds a Start-menu entry.

### Prerequisites

- **Windows 10 version 1809 (October 2018 Update) or newer**, x64.
- **Ollama** (optional, required only for AI summaries and semantic search). See [AI setup](#ai-setup) below.

## AI setup

NoteForge's AI features talk to a local [Ollama](https://ollama.com) instance. Install Ollama, then pull the default models:

```bash
ollama pull ibm/granite4:1b-h       # text generation (summaries)
ollama pull nomic-embed-text        # embeddings (semantic search & graph)
```

Start Ollama (it runs as a background service by default on Windows), then launch NoteForge. Open the **Settings** panel from the sidebar to toggle AI features, point NoteForge at a different Ollama URL, or swap in your own text-generation and embedding models.

## Build from source

**Requirements:** .NET 8 SDK, Node.js 18+ (for the CodeMirror editor bundle), Visual Studio 2022 with the "Windows application development" workload (or the Windows App SDK 1.8 component packs).

```powershell
# Clone
git clone https://github.com/SeolJinn/NoteForge.git
cd NoteForge

# Bundle the editor (one time, or whenever editor/src/ changes)
cd editor
npm install
npm run build
cd ..

# Run in dev mode
dotnet run --project NoteForge\NoteForge.csproj

# Produce a self-contained publish folder
dotnet publish NoteForge\NoteForge.csproj -c Release -r win-x64 -p:Publishing=true

# Build the installer (requires Inno Setup 6 installed)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

The installer lands in `dist\NoteForge-Setup-<version>.exe`.

## Architecture

NoteForge is a WinUI 3 app targeting .NET 8. Key pieces:

- **CQRS with Mediator.** Commands and queries in `Handlers/` are dispatched through `IMediator`.
- **Inline editor.** A Node.js project in `editor/` builds to a single embedded HTML resource (CodeMirror 6 + KaTeX) hosted in a `WebView2` control. C#/JS messaging runs through `EditorInteropService`.
- **Hybrid search.** `TfidfCalculator` + `SemanticSearchStrategy` combine keyword and embedding scores via harmonic mean.
- **SQLite-backed embeddings.** Per-vault embedding cache at `%LOCALAPPDATA%\NoteForge\Embeddings\<vault-hash>.db` so embeddings survive restarts and only regenerate when note content changes.

## License

NoteForge is licensed under the [GNU General Public License v3.0](LICENSE).

Copyright © 2026 Manolache Alexandru
