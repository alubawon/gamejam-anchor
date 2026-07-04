# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Game jam Unity project ("gamejam-anchor"). Currently a minimal template — only `Assets/Scenes/SampleScene.scene` exists, no custom scripts or prefabs yet.

## Engine & Tooling

- **Unity version:** 2022.3.62t11 (Tuanjie — a Chinese Unity derivative, editor v1.9.3)
- **Solution file:** `gamejam-anchor.sln` (VS Code configured via `.vscode/settings.json`)
- **Scripting backend:** Mono (default), API compatibility .NET 4.x equivalent
- **YAML tag prefix:** `tag:yousandi.cn,2023` (Tuanjie-specific, not standard `unity3d.com`)

## Key Packages

- TextMeshPro 3.0.9, Timeline 1.7.7, UGUI 1.0.0, Visual Scripting 1.9.4
- `cn.tuanjie.codely.bridge` 1.0.66 — Tuanjie bridge package
- Code coverage and profile analyzer tools installed but unused

## Unity Project Conventions

- Target platforms: Standalone (Win/Mac/Linux), Android, iOS, WebGL
- Default orientation: Landscape, resolution 1920×1080
- `.vscode/settings.json` hides most Unity binary file types from the explorer
- No `.gitignore` exists yet — one should be added before committing (exclude `Library/`, `Temp/`, `obj/`, `Build/`, `UserSettings/`)

## Working with This Project

- **Opening:** Open the project root folder in the Tuanjie/Unity Hub (not the .sln directly)
- **Building:** Via Unity Editor → File → Build Settings; no CLI build scripts exist
- **Testing:** Unity Test Framework is available but no test assemblies are configured
- **C# editing:** Use VS Code with the C# extension; `dotnet defaultSolution` is set to `gamejam-anchor.sln`

## Adding New Systems

When adding game systems, follow standard Unity conventions:
- Scripts → `Assets/Scripts/<SystemName>/`
- Prefabs → `Assets/Prefabs/`
- Scenes → `Assets/Scenes/`
- Use Assembly Definition files (`.asmdef`) to organize scripts once the project grows beyond a handful of scripts
