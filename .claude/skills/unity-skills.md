---
name: unity-skills
description: Control Unity Editor via the AI Game Developer MCP server — inspect scenes, create/modify GameObjects, write and compile C# scripts, run tests, manage assets, take screenshots, profile performance, and more. Use whenever the user wants to operate Unity from chat — create or modify GameObjects/scripts/scenes/assets, batch-edit, or run any Unity Editor automation, even if they just say "在 Unity 里…" or "操作 Unity". 通过 MCP 自动化 Unity 编辑器（场景检查、对象创建修改、脚本编写编译、测试运行、资源管理、截图、性能分析等）。
---

# Unity Skills (AI Game Developer / Unity-MCP)

Use this skill when the user wants to control the Unity Editor through the AI Game Developer MCP server.

## MCP Server

The MCP server (`ai-game-developer`) is configured in `.mcp.json` as an HTTP server at `http://localhost:8080`.

**Prerequisite**: The Unity Editor must be open with the `com.ivanmurzak.unity.mcp` plugin installed and the MCP server running. The server auto-starts when Unity opens the project.

If the server is not reachable, tell the user: "请先打开 Unity 编辑器，确保 AI Game Developer 插件已安装并运行 MCP 服务器。"

## Available Tool Categories (70+ tools)

### Project & Assets
- `project_get_info` — Get Unity project info (version, packages, paths)
- `project_get_packages` — List installed packages
- `asset_*` — Find, create, delete, move, import assets
- `resource_*` — Load/create scriptable objects, materials, textures

### Scene & Hierarchy
- `scene_*` — Get scene info, load/save scenes
- `game_object_*` — Create, find, delete, duplicate GameObjects; manage hierarchy (parent/child)
- `component_*` — Add, get, set, remove components on GameObjects
- `transform_*` — Get/set position, rotation, scale
- `hierarchy_*` — Get full scene hierarchy tree

### Scripting & Editor
- `script_*` — Create, read, update, delete C# scripts
- `script_compile` — Trigger compilation and get errors
- `script_get_compile_errors` — Get compilation errors
- `editor_*` — Editor state, selection, play mode control
- `test_*` — Create and run Unity tests
- `package_*` — Install/remove packages

### Profiling & Diagnostics
- `profiler_*` — Capture and analyze profiler data
- `debug_*` — Get logs, errors, warnings
- `screenshot_take` — Take editor or game screenshots
- `console_*` — Read console logs

### Extension Packs (10)
Animation, Cinemachine, InputSystem, Navigation, ParticleSystem, ProBuilder, Splines, Terrain, Tilemap, Timeline — additional tools available when the corresponding Unity packages are installed.

## Usage Patterns

### Read-before-write
Always inspect the current state before making changes:
1. Use `scene_get_info` / `hierarchy_get` to understand the scene
2. Use `game_object_find` to locate objects before modifying
3. Use `component_get` to read current values before setting new ones

### Script creation
1. Use `script_create` with the full C# source code
2. Use `script_compile` to verify it compiles
3. Use `script_get_compile_errors` to check for issues
4. Attach the script via `component_add` if it's a MonoBehaviour

### Scene manipulation
1. Create GameObjects with `game_object_create`
2. Set parent with `game_object_set_parent`
3. Add components with `component_add`
4. Set component properties with `component_set`

### Batch operations
When modifying 2+ objects, prefer doing operations in sequence rather than one-by-one with redundant reads. Read once, plan all changes, then execute.

## Important Notes

- **Unity must be running** for any MCP tool to work
- **Compilation triggers**: Script creation/modification can trigger Domain Reload — wait and retry on transient unavailability
- **Play Mode**: Some operations are restricted during Play Mode; exit Play Mode first if needed
- **Path requirement**: Project path must not contain spaces
- **Tuanjie compatibility**: This project uses Tuanjie 2022.3.62t11 — most tools work, but some editor-specific features may differ from standard Unity

## Auto-Generate Skills

For the most up-to-date skill list with exact parameters, run in Unity Editor:
`Window > AI Game Developer > Auto-generate` (Skills section)

Or via CLI:
```bash
unity-mcp-cli setup-skills claude-code /Users/liuzihao35/repos/gamejam-anchor
```
(This requires Unity to be running first)
