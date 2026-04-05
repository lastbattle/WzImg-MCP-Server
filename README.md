# WzImg MCP Server Documentation

**Version**: 1.1.0

## Overview

WzImg MCP Server is a Model Context Protocol (MCP) server that enables AI assistants (Claude, GPT, Gemini, etc.) to interact with MapleStory IMG files programmatically. 
It provides 74 tools across 10 categories for reading, analyzing, modifying, and exporting game data.

First export the .img files with [HaCreator](https://github.com/lastbattle/Harepacker-resurrected), you will need the manifest.json!

---

## Quick Start

### Installation

```bash
# Build the server
cd WzImg-MCP-Server
dotnet build WzImgMCP.csproj
```

---

## Connection Methods

WzImg MCP Server supports two transport modes: **stdio** (standard input/output) and **HTTP** (Streamable HTTP with SSE).

### MCP server name: wzimg

### Method 1: Stdio (Default)

The stdio method runs the server as a subprocess. The client communicates via stdin/stdout.

```bash
# Run in stdio mode (default)
dotnet run --project WzImgMCP
```

**Claude Desktop Configuration** (`%APPDATA%\Claude\claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "wzimg": {
      "command": "dotnet",
      "args": ["run", "--project", "E:/path/to/WzImg-MCP-Server/WzImgMCP.csproj"],
      "env": {
        "WZIMGMCP_DATA_PATH": "D:\\Extract\\v83"
      }
    }
  }
}
```

**Claude Code Configuration** (`.mcp.json` in project root):

```json
{
  "mcpServers": {
    "wzimg": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "E:/path/to/WzImg-MCP-Server/WzImgMCP.csproj"],
      "env": {
        "WZIMGMCP_DATA_PATH": "D:\\Extract\\v83"
      }
    }
  }
}
```

---

### Method 2: HTTP (Streamable HTTP)

The HTTP method runs the server as a standalone web service. Clients connect via HTTP requests with Server-Sent Events (SSE) for streaming responses.

```bash
# Run in HTTP mode
dotnet run --project WzImgMCP -- --http

# Or specify a custom port
dotnet run --project WzImgMCP -- --http --port 8080
```

**Claude Code Configuration** (`.mcp.json`):

```json
{
  "mcpServers": {
    "wzimg": {
      "type": "http",
      "url": "http://127.0.0.1:13339/mcp",
      "env": {
        "WZIMGMCP_DATA_PATH": "D:\\Extract\\v83"
      }
    }
  }
}
```

> **Note:** When using HTTP mode, the `WZIMGMCP_DATA_PATH` environment variable should be set when starting the server, not in the client config. The `env` block above is shown for reference but is only applied by the client when spawning the server (stdio mode). For HTTP mode, set it before running the server:
> ```bash
> # Windows
> set WZIMGMCP_DATA_PATH=D:\Extract\v83
> dotnet run --project WzImgMCP -- --http
>
> # Linux/macOS
> WZIMGMCP_DATA_PATH=/path/to/data dotnet run --project WzImgMCP -- --http
> ```

**Using with curl (testing):**

```bash
# Initialize connection (returns session ID)
curl -X POST http://127.0.0.1:13339/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"initialize","params":{"capabilities":{}},"id":1}'

# Call a tool
curl -X POST http://127.0.0.1:13339/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"list_categories","arguments":{}},"id":2}'
```

---

### Environment Variables

| Variable | Description |
|----------|-------------|
| `WZIMGMCP_DATA_PATH` | Path to IMG filesystem extracted by HaCreator (must contain `manifest.json`) |
| `WZIMGMCP_PORT` | Server port for HTTP mode (default: 13339) |
| `WZIMGMCP_TRANSPORT` | Transport mode: `stdio` (default) or `http` |

### Command Line Arguments

| Argument | Description |
|----------|-------------|
| `--http` | Run in HTTP mode instead of stdio |
| `--port <port>` | Set HTTP server port (default: 13339) |
| `--data-path <path>` | Set data source path |

---

### Comparison: Stdio vs HTTP

| Feature | Stdio | HTTP |
|---------|-------|------|
| **Startup** | Launched per-session by client | Runs as persistent service |
| **Performance** | Low latency (direct IPC) | Slight overhead (TCP/HTTP) |
| **Multiple clients** | One client per process | Multiple concurrent clients |
| **Debugging** | Harder (subprocess) | Easier (standalone process) |
| **Deployment** | Embedded | Can run on remote server |
| **Best for** | Claude Desktop, local dev | CI/CD, remote access, shared servers |

**Expected Directory Structure:**
```
D:\Extract\v83\
├── manifest.json          # Version metadata (required)
├── Character/
│   ├── 00002000.img
│   ├── 00012000.img
│   └── ...
├── Map/
│   ├── Map0/
│   │   ├── 000010000.img
│   │   └── ...
│   └── ...
├── Mob/
├── Npc/
├── Sound/
└── ...
```

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              AI Clients                                 │
├─────────────────┬─────────────────┬─────────────────────────────────────┤
│  Claude Desktop │   Claude Code   │        Other MCP Clients            │
│   (subprocess)  │  (CLI/remote)   │      (custom applications)          │
└────────┬────────┴────────┬────────┴─────────────────┬───────────────────┘
         │                 │                          │
         │ stdio           │ stdio or HTTP            │ HTTP
         │                 │                          │
         ▼                 ▼                          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           WzImg MCP Server                              │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                      Transport Layer                               │ │
│  │  ┌─────────────────────┐      ┌──────────────────────────────────┐ │ │
│  │  │   Stdio Transport   │      │       HTTP Transport             │ │ │
│  │  │   (stdin/stdout)    │      │   (Streamable HTTP + SSE)        │ │ │
│  │  │                     │      │   Port: 13339 (default)          │ │ │
│  │  └──────────┬──────────┘      └────────────────┬─────────────────┘ │ │
│  └─────────────┼──────────────────────────────────┼───────────────────┘ │
│                │                                  │                     │
│                └────────────────┬─────────────────┘                     │
│                                 ▼                                       │
│                    ┌─────────────────────────┐                          │
│                    │   MCP Protocol Handler  │                          │
│                    │   (JSON-RPC 2.0)        │                          │
│                    └────────────┬────────────┘                          │
│                                 ▼                                       │
│                    ┌─────────────────────────┐                          │
│                    │      Tool Classes       │                          │
│                    │   (74 tools, ToolBase)  │                          │
│                    └────────────┬────────────┘                          │
│                                 ▼                                       │
│                    ┌─────────────────────────┐                          │
│                    │   WzSessionManager      │                          │
│                    │   (cache, state)        │                          │
│                    └────────────┬────────────┘                          │
│                                 ▼                                       │
│                    ┌─────────────────────────┐                          │
│                    │       MapleLib          │                          │
│                    │    (WZ/IMG I/O)         │                          │
│                    └─────────────────────────┘                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Core Components

| Component | Description |
|-----------|-------------|
| `ToolBase` | Base class providing session validation and error handling |
| `Result<T>` | Internal wrapper used by tools before Markdown rendering |
| `WzSessionManager` | Manages loaded data sources and image cache |
| `WzDataConverter` | Converts WZ properties to serializable formats |

---

## Response Format

All MCP tools now return **Markdown text** (plain `string` output) instead of JSON objects.

Typical success output:
```md
- success: true
- data:
  - category: Map
  - image: 100000000.img
```

Typical failure output:
```md
- success: false
- error: No data source initialized
```

---

## Response Size Optimization

Tools support pagination and compact modes to minimize response size. These are enabled by default.

### Pagination

Paginated responses include `offset`, `limit`, `totalCount`, and `hasMore`:

```json
{ "totalCount": 150, "offset": 0, "limit": 50, "hasMore": true, "items": [...] }
```

### Compact Mode

`compact=true` (default) returns minimal fields. Set `compact=false` for full property metadata.

### Configuration

Limits are configurable in `appsettings.json`:

```json
{
  "HaMCP": {
    "ResponseLimits": {
      "MaxJsonResponseKB": 512,
      "MaxBase64ImageKB": 256,
      "DefaultSearchResults": 20,
      "DefaultPropertyPageSize": 50
    }
  }
}
```

---

## Tool Reference

All tools accept `category` and `image` parameters unless noted. Paths use `/` separator (e.g., `info/bgm`).

### File Operations

| Tool | Description |
|------|-------------|
| `init_data_source` | Initialize data source from directory containing `manifest.json`. Params: `basePath` |
| `scan_img_directories` | Scan directory for available data sources. Params: `path`, `recursive=true` |
| `get_data_source_info` | Get current data source metadata and cache stats |
| `list_categories` | List available categories (Map, Mob, Npc, etc.) |
| `list_images_in_category` | List .img files. Params: `category`, `subdirectory?` |
| `get_cache_stats` | Get cache hit ratio and memory usage |
| `clear_cache` | Clear loaded image cache |

---

### Navigation

| Tool | Description |
|------|-------------|
| `get_subdirectories` | List subdirectories in a category |
| `list_properties` | List child properties. Params: `path?`, `compact=true`, `offset=0`, `limit=50` (max 500) |
| `get_tree_structure` | Get property tree. Params: `path?`, `depth=2` (max 5), `maxChildrenPerNode=50` (max 200). Hard limit: 1000 nodes |
| `search_by_name` | Search by name pattern (`*` wildcards). Params: `pattern`, `category?`, `image?`, `compact=true`, `maxResults=20` (max 200) |
| `search_by_value` | Search by value. Params: `value`, `type?`, `category?`, `image?`, `compact=true`, `maxResults=20` (max 200) |
| `get_property_path` | Get full path of a property |

---

### Property Access

| Tool | Description |
|------|-------------|
| `get_property` | Get property with full metadata |
| `get_property_value` | Get just the value |
| `get_string` | Get string value. Params: `path`, `defaultValue?` |
| `get_int` | Get integer value. Params: `path`, `defaultValue?` |
| `get_float` | Get float value. Params: `path`, `defaultValue?` |
| `get_vector` | Get vector (X, Y) |
| `resolve_uol` | Resolve UOL link to target property |
| `get_children` | Get child properties. Params: `path?`, `compact=true`, `offset=0`, `limit=50` (max 500) |
| `get_property_count` | Count child properties |
| `iterate_properties` | Iterate with full metadata. Params: `path?`, `offset=0`, `limit=50` |
| `get_properties_batch` | Batch get. Params: `requests[]` with `{category, image, path}` |

**Property Types:** `Null`, `Short`, `Int`, `Long`, `Float`, `Double`, `String`, `SubProperty`, `Canvas`, `Vector`, `Sound`, `UOL`, `Lua`, `Convex`

---

### Canvas Operations

| Tool | Description |
|------|-------------|
| `get_canvas_bitmap` | Get image as base64 PNG |
| `get_canvas_info` | Get metadata (dimensions, format, origin, delay) without image data |
| `get_canvas_origin` | Get draw offset point (X, Y) |
| `get_canvas_head` | Get head position for character rendering |
| `get_canvas_bounds` | Get lt/rb bounds |
| `get_canvas_delay` | Get frame delay in milliseconds |
| `get_animation_frames` | Get frames. Params: `path`, `metadataOnly=true`, `offset=0`, `limit=5` (max 50). Returns `frameCount`, `totalDuration`, `hasMore` |
| `list_canvas_in_image` | List all canvases. Params: `maxDepth=10` |
| `resolve_canvas_link` | Resolve `_inlink`/`_outlink` references |

**Frame structure (Markdown response):**
```md
- index: 0
- width: 100
- height: 120
- origin:
  - x: 50
  - y: 100
- delay: 120
```
When `metadataOnly=false`, each frame also includes:
```md
- base64_png: <base64 data>
```
When `metadataOnly=true`, `base64_png` is omitted.

---

### Audio Operations

| Tool | Description |
|------|-------------|
| `get_sound_info` | Get metadata (duration, format, frequency) |
| `get_sound_data` | Get audio as base64 |
| `list_sounds_in_image` | List all sounds. Params: `maxDepth=10` |
| `resolve_sound_link` | Resolve UOL to sound |

---

### Export Operations

| Tool | Description |
|------|-------------|
| `export_to_json` | Export to JSON. Params: `path`, `maxDepth=5` (max 10), `outputPath?`. Error if >100KB without `outputPath` |
| `export_to_xml` | Export to XML file. Params: `path`, `outputPath` |
| `export_png` | Export canvas to PNG. Params: `path`, `outputPath` |
| `export_mp3` | Export sound to MP3. Params: `path`, `outputPath` |
| `export_all_images` | Batch export canvases. Params: `outputPath` |
| `export_all_sounds` | Batch export sounds. Params: `outputPath` |

---

### Analysis

| Tool | Description |
|------|-------------|
| `get_statistics` | Get data source statistics |
| `get_category_summary` | Summarize category (image count, size) |
| `find_broken_uols` | Find broken UOL references |
| `compare_properties` | Compare two properties. Params: `source{category,image,path}`, `target{...}` |
| `get_version_info` | Get server version |
| `validate_image` | Validate image structure |

---

### Modification

| Tool | Description |
|------|-------------|
| `set_string` | Set string value |
| `set_int` | Set integer value |
| `set_float` | Set float value |
| `set_vector` | Set vector. Params: `path`, `x`, `y` |
| `add_property` | Add property. Params: `path`, `name`, `type`, `value` |
| `delete_property` | Delete property |
| `rename_property` | Rename. Params: `path`, `newName` |
| `copy_property` | Deep copy. Params: `source{...}`, `target{...}` |
| `set_canvas_bitmap` | Replace image. Params: `path`, `base64Png` |
| `set_canvas_origin` | Set origin. Params: `path`, `x`, `y` |
| `import_png` | Import PNG. Params: `path`, `pngPath` |
| `import_sound` | Import audio. Params: `path`, `soundPath` |
| `save_image` | Save changes to disk |
| `discard_changes` | Revert unsaved changes |

---

### Batch Operations

| Tool | Description |
|------|-------------|
| `extract_to_img` | Extract WZ to IMG. Params: `wzPath`, `outputPath` |
| `pack_to_wz` | Pack IMG to WZ. Params: `imgPath`, `outputPath` |
| `batch_export_images` | Export category images. Params: `category`, `outputPath` |
| `batch_search` | Search across categories. Params: `pattern`, `categories[]`, `maxResults` |

---

### Lifecycle

| Tool | Description |
|------|-------------|
| `parse_image` | Parse image into memory |
| `unparse_image` | Free image memory |
| `is_image_parsed` | Check if parsed |
| `get_parsed_images` | List parsed images |
| `preload_category` | Preload all images in category |
| `unload_category` | Unload all images in category |

---

## Code Architecture

### ToolBase Pattern

All tool classes extend `ToolBase` for consistent error handling:

```csharp
public class MyTools : ToolBase
{
    public MyTools(WzSessionManager session) : base(session) { }

    [McpServerTool(Name = "my_tool")]
    public string MyTool(string param)
    {
        return Execute(() =>
        {
            // Business logic here
            return new MyData { ... };
        });
    }
}
```

### Execute Helpers

| Method | Description |
|--------|-------------|
| `Execute<T>()` | Validates session, catches exceptions |
| `ExecuteRaw<T>()` | No session check (for init operations) |
| `GetImage()` | Get parsed WzImage |
| `GetProperty()` | Get property by path |
| `GetRequiredProperty<T>()` | Get typed property (throws if not found) |

### Result Types

```csharp
// Internal result wrapper used before Markdown conversion
public class Result<T>
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public T? Data { get; init; }
}

// Common data types
public record Point2D(int X, int Y);
public record Size2D(int Width, int Height);
```

---

## Project Structure

```
WzImgMCP/
├── Program.cs                 # Entry point
├── Core/
│   ├── Result.cs              # Generic result types
│   └── ToolBase.cs            # Base class for tools
├── Server/
│   └── WzSessionManager.cs    # Session management
├── Tools/
│   ├── FileTools.cs           # 7 tools
│   ├── NavigationTools.cs     # 6 tools
│   ├── PropertyTools.cs       # 11 tools
│   ├── ImageTools.cs          # 9 tools
│   ├── AudioTools.cs          # 4 tools
│   ├── ExportTools.cs         # 6 tools
│   ├── AnalysisTools.cs       # 6 tools
│   ├── ModifyTools.cs         # 14 tools
│   ├── BatchTools.cs          # 4 tools
│   └── LifecycleTools.cs      # 6 tools
└── Utils/
    └── WzDataConverter.cs     # Type conversion utilities
```

---

## Testing

```bash
# Run unit tests
dotnet test WzImgMCP.Tests

# Test with MCP Inspector
npx @modelcontextprotocol/inspector dotnet run --project WzImgMCP
```

---

## Dependencies

```xml
<PackageReference Include="ModelContextProtocol" Version="0.1.0-preview" />
<ProjectReference Include="..\MapleLib\MapleLib.csproj" />
```

---

## License

MIT

```
Copyright (c) 2026, LastBattle https://github.com/lastbattle
 
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
