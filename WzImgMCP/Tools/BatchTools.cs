using System.ComponentModel;
using ModelContextProtocol.Server;
using WzImgMCP.Core;
using WzImgMCP.Server;
using MapleLib.Img;
using MapleLib.WzLib;
using MapleLib.WzLib.Util;

namespace WzImgMCP.Tools;

/// <summary>
/// MCP tools for batch operations and WZ conversion
/// </summary>
[McpServerToolType]
public class BatchTools
{
    private readonly WzSessionManager _session;

    public BatchTools(WzSessionManager session)
    {
        _session = session;
    }

    [McpServerTool(Name = "extract_to_img"), Description("Extract WZ files to IMG filesystem format")]
    public string ExtractToImg(
        [Description("Path to WZ file or directory containing WZ files")] string wzPath,
        [Description("Output directory for IMG filesystem")] string outputDir,
        [Description("Version key for WZ decryption (empty for auto-detect)")] string? versionKey = null,
        [Description("Create version manifest")] bool createManifest = true,
        [Description("Categories to extract (comma-separated, optional). For a single WZ file this is inferred from the file name.")] string? categories = null,
        [Description("Version id for the extracted IMG filesystem manifest")] string? versionId = null,
        [Description("Display name for the extracted IMG filesystem manifest")] string? displayName = null,
        [Description("Resolve _inlink/_outlink canvas references during extraction")] bool resolveLinks = false)
    {
        try
        {
            if (!Directory.Exists(wzPath) && !File.Exists(wzPath))
            {
                return new ExtractResult { Success = false, Error = $"Path not found: {wzPath}" };
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var mapleStoryPath = File.Exists(wzPath)
                ? Path.GetDirectoryName(Path.GetFullPath(wzPath)) ?? Directory.GetCurrentDirectory()
                : Path.GetFullPath(wzPath);
            var categoriesToExtract = GetExtractionCategories(wzPath, categories);
            var encryption = ResolveEncryption(versionKey, wzPath);
            var effectiveVersionId = string.IsNullOrWhiteSpace(versionId)
                ? $"extracted_{DateTime.UtcNow:yyyyMMddHHmmss}"
                : versionId.Trim();
            var effectiveDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? $"Extracted from {Path.GetFileName(mapleStoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))}"
                : displayName.Trim();

            var extractor = new WzExtractionService();
            var result = categoriesToExtract.Count > 0
                ? extractor.ExtractAsync(
                    mapleStoryPath,
                    outputDir,
                    effectiveVersionId,
                    effectiveDisplayName,
                    encryption,
                    categoriesToExtract,
                    resolveLinks).GetAwaiter().GetResult()
                : extractor.ExtractAsync(
                    mapleStoryPath,
                    outputDir,
                    effectiveVersionId,
                    effectiveDisplayName,
                    encryption,
                    resolveLinks).GetAwaiter().GetResult();

            var errors = result.CategoriesExtracted.Values
                .SelectMany(c => c.Errors.Select(e => $"{c.CategoryName}: {e}"))
                .ToList();

            return new ExtractResult
            {
                Success = result.Success && errors.Count == 0,
                Error = result.ErrorMessage ?? (errors.Count > 0 ? "Extraction completed with category errors" : null),
                OutputDirectory = outputDir,
                CategoriesExtracted = result.CategoriesExtracted.Count,
                ImagesExtracted = result.TotalImagesExtracted,
                TotalSize = result.TotalSize,
                DurationSeconds = result.Duration.TotalSeconds,
                ManifestCreated = createManifest && File.Exists(Path.Combine(outputDir, "manifest.json")),
                ExtractedCategories = result.CategoriesExtracted.Keys.OrderBy(c => c).ToList(),
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return new ExtractResult { Success = false, Error = ex.Message };
        }
    }

    [McpServerTool(Name = "pack_to_wz"), Description("Pack IMG filesystem back to WZ files")]
    public string PackToWz(
        [Description("Path to IMG filesystem directory")] string imgPath,
        [Description("Output directory for WZ files")] string outputDir,
        [Description("WZ version to create")] int wzVersion = 83,
        [Description("Category to pack (optional - packs all if not specified)")] string? category = null,
        [Description("WZ encryption/version key (GMS, EMS, BMS, CLASSIC; empty uses manifest/default)")] string? versionKey = null,
        [Description("Save as 64-bit WZ format")] bool saveAs64Bit = false,
        [Description("Separate canvas data for 64-bit WZ format")] bool separateCanvas = false)
    {
        try
        {
            if (!Directory.Exists(imgPath))
            {
                return new PackResult { Success = false, Error = $"Directory not found: {imgPath}" };
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var categoriesToPack = GetPackingCategories(imgPath, category);
            if (categoriesToPack.Count == 0)
            {
                return new PackResult { Success = false, Error = $"No IMG categories found in {imgPath}" };
            }

            var existingWzFiles = Directory.EnumerateFiles(outputDir, "*.wz", SearchOption.TopDirectoryOnly)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var packer = new WzPackingService();
            var result = packer.PackCategoriesAsync(
                imgPath,
                outputDir,
                categoriesToPack,
                saveAs64Bit,
                overridePatchVersion: ToPatchVersion(wzVersion),
                separateCanvas: separateCanvas,
                overrideEncryption: ParseEncryption(versionKey)).GetAwaiter().GetResult();

            var createdFiles = Directory.EnumerateFiles(outputDir, "*.wz", SearchOption.TopDirectoryOnly)
                .Where(path => !existingWzFiles.Contains(path))
                .OrderBy(path => path)
                .ToList();
            if (createdFiles.Count == 0)
            {
                createdFiles = result.CategoriesPacked.Values
                    .Select(r => r.OutputFilePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(path => path)
                    .ToList()!;
            }

            var errors = result.CategoriesPacked.Values
                .SelectMany(c => c.Errors.Select(e => $"{c.CategoryName}: {e}"))
                .ToList();

            return new PackResult
            {
                Success = result.Success && createdFiles.Count > 0 && errors.Count == 0,
                Error = result.ErrorMessage ?? (createdFiles.Count == 0 ? "Packing completed without creating any WZ files" : null),
                OutputDirectory = outputDir,
                FilesCreated = createdFiles.Count,
                TotalSize = createdFiles.Count > 0 ? createdFiles.Sum(path => new FileInfo(path).Length) : result.TotalOutputSize,
                ImagesPacked = result.TotalImagesPacked,
                DurationSeconds = result.Duration.TotalSeconds,
                PackedCategories = result.CategoriesPacked.Keys.OrderBy(c => c).ToList(),
                CreatedFiles = createdFiles,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return new PackResult { Success = false, Error = ex.Message };
        }
    }

    [McpServerTool(Name = "batch_export_images"), Description("Export all images from multiple categories")]
    public string BatchExportImages(
        [Description("Categories to export (comma-separated, or 'all')")] string categories,
        [Description("Output directory")] string outputDir,
        [Description("Output format (png, jpg)")] string format = "png",
        [Description("Maximum images to export")] int maxImages = 1000)
    {
        if (!_session.IsInitialized)
        {
            return new BatchExportImagesResult { Success = false, Error = "No data source initialized" };
        }

        try
        {
            var ds = _session.DataSource;
            IEnumerable<string> categoryList;

            if (categories.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                categoryList = ds.GetCategories();
            }
            else
            {
                categoryList = categories.Split(',').Select(c => c.Trim());
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var exported = new List<string>();
            var failed = new List<string>();
            int totalExported = 0;

            foreach (var category in categoryList)
            {
                if (totalExported >= maxImages) break;

                var categoryDir = Path.Combine(outputDir, category);
                if (!Directory.Exists(categoryDir))
                {
                    Directory.CreateDirectory(categoryDir);
                }

                foreach (var img in ds.GetImagesInCategory(category))
                {
                    if (totalExported >= maxImages) break;

                    try
                    {
                        var wasParsed = img.Parsed;
                        if (!img.Parsed) img.ParseImage();

                        var imageDir = Path.Combine(categoryDir, Path.GetFileNameWithoutExtension(img.Name));
                        ExportCanvasesFromImage(img, imageDir, format, exported, failed, ref totalExported, maxImages);

                        if (!wasParsed) img.UnparseImage();
                    }
                    catch (Exception ex)
                    {
                        failed.Add($"{category}/{img.Name}: {ex.Message}");
                    }
                }
            }

            return new BatchExportImagesResult
            {
                Success = true,
                OutputDirectory = outputDir,
                ExportedCount = exported.Count,
                FailedCount = failed.Count,
                Truncated = totalExported >= maxImages,
                SampleExported = exported.Take(20).ToList(),
                Failed = failed.Take(20).ToList()
            };
        }
        catch (Exception ex)
        {
            return new BatchExportImagesResult { Success = false, Error = ex.Message };
        }
    }

    [McpServerTool(Name = "batch_search"), Description("Search across multiple categories")]
    public string BatchSearch(
        [Description("Search pattern (supports wildcards)")] string pattern,
        [Description("Categories to search (comma-separated, or 'all')")] string categories = "all",
        [Description("Search type: name, value, or both")] string searchType = "name",
        [Description("Maximum results (default: 30)")] int maxResults = 30)
    {
        if (!_session.IsInitialized)
        {
            return new BatchSearchResult { Success = false, Error = "No data source initialized" };
        }

        try
        {
            var ds = _session.DataSource;
            IEnumerable<string> categoryList;

            if (categories.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                categoryList = ds.GetCategories();
            }
            else
            {
                categoryList = categories.Split(',').Select(c => c.Trim());
            }

            var results = new List<BatchSearchMatch>();
            var regex = WildcardToRegex(pattern);

            foreach (var category in categoryList)
            {
                if (results.Count >= maxResults) break;

                foreach (var img in ds.GetImagesInCategory(category))
                {
                    if (results.Count >= maxResults) break;

                    try
                    {
                        var wasParsed = img.Parsed;
                        if (!img.Parsed) img.ParseImage();

                        SearchInImage(img, category, img.Name, regex, searchType, results, maxResults);

                        if (!wasParsed) img.UnparseImage();
                    }
                    catch
                    {
                        // Skip images that can't be parsed
                    }
                }
            }

            return new BatchSearchResult
            {
                Success = true,
                Pattern = pattern,
                SearchType = searchType,
                ResultCount = results.Count,
                Truncated = results.Count >= maxResults,
                Results = results
            };
        }
        catch (Exception ex)
        {
            return new BatchSearchResult { Success = false, Error = ex.Message };
        }
    }

    private void ExportCanvasesFromImage(MapleLib.WzLib.WzImage img, string outputDir,
        string format, List<string> exported, List<string> failed, ref int count, int max)
    {
        if (count >= max) return;

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        foreach (var prop in img.WzProperties ?? Enumerable.Empty<MapleLib.WzLib.WzImageProperty>())
        {
            ExportCanvasesRecursive(prop, "", outputDir, format, exported, failed, ref count, max);
        }
    }

    private void ExportCanvasesRecursive(MapleLib.WzLib.WzImageProperty prop, string path,
        string outputDir, string format, List<string> exported, List<string> failed, ref int count, int max)
    {
        if (count >= max) return;

        if (prop is MapleLib.WzLib.WzProperties.WzCanvasProperty canvas)
        {
            try
            {
                var bitmap = canvas.GetLinkedWzCanvasBitmap();
                if (bitmap != null)
                {
                    var fileName = string.IsNullOrEmpty(path) ? prop.Name : path.Replace("/", "_");
                    var filePath = Path.Combine(outputDir, $"{fileName}.{format}");

                    var imageFormat = format.ToLowerInvariant() == "jpg"
                        ? System.Drawing.Imaging.ImageFormat.Jpeg
                        : System.Drawing.Imaging.ImageFormat.Png;

                    bitmap.Save(filePath, imageFormat);
                    exported.Add(filePath);
                    count++;
                }
            }
            catch (Exception ex)
            {
                failed.Add($"{path}: {ex.Message}");
            }
        }

        var childPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}/{prop.Name}";
        foreach (var child in prop.WzProperties ?? Enumerable.Empty<MapleLib.WzLib.WzImageProperty>())
        {
            ExportCanvasesRecursive(child, childPath, outputDir, format, exported, failed, ref count, max);
        }
    }

    private void SearchInImage(MapleLib.WzLib.WzImage img, string category, string imageName,
        System.Text.RegularExpressions.Regex regex, string searchType, List<BatchSearchMatch> results, int max)
    {
        foreach (var prop in img.WzProperties ?? Enumerable.Empty<MapleLib.WzLib.WzImageProperty>())
        {
            SearchInProperty(prop, category, imageName, "", regex, searchType, results, max);
        }
    }

    private void SearchInProperty(MapleLib.WzLib.WzImageProperty prop, string category, string imageName,
        string path, System.Text.RegularExpressions.Regex regex, string searchType, List<BatchSearchMatch> results, int max)
    {
        if (results.Count >= max) return;

        var currentPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}/{prop.Name}";
        var matched = false;

        // Check name
        if (searchType == "name" || searchType == "both")
        {
            if (regex.IsMatch(prop.Name))
            {
                matched = true;
            }
        }

        // Check value
        if (!matched && (searchType == "value" || searchType == "both"))
        {
            var valueStr = GetPropertyValueString(prop);
            if (valueStr != null && regex.IsMatch(valueStr))
            {
                matched = true;
            }
        }

        if (matched)
        {
            results.Add(new BatchSearchMatch
            {
                Category = category,
                Image = imageName,
                Path = currentPath,
                Name = prop.Name,
                Type = prop.PropertyType.ToString(),
                Value = GetPropertyValueString(prop)
            });
        }

        // Recurse
        foreach (var child in prop.WzProperties ?? Enumerable.Empty<MapleLib.WzLib.WzImageProperty>())
        {
            SearchInProperty(child, category, imageName, currentPath, regex, searchType, results, max);
        }
    }

    private static string? GetPropertyValueString(MapleLib.WzLib.WzImageProperty prop)
    {
        return prop switch
        {
            MapleLib.WzLib.WzProperties.WzStringProperty s => s.Value,
            MapleLib.WzLib.WzProperties.WzIntProperty i => i.Value.ToString(),
            MapleLib.WzLib.WzProperties.WzShortProperty sh => sh.Value.ToString(),
            MapleLib.WzLib.WzProperties.WzLongProperty l => l.Value.ToString(),
            MapleLib.WzLib.WzProperties.WzFloatProperty f => f.Value.ToString(),
            MapleLib.WzLib.WzProperties.WzDoubleProperty d => d.Value.ToString(),
            MapleLib.WzLib.WzProperties.WzUOLProperty u => u.Value,
            _ => null
        };
    }

    private static System.Text.RegularExpressions.Regex WildcardToRegex(string pattern)
    {
        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return new System.Text.RegularExpressions.Regex(regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static List<string> GetExtractionCategories(string wzPath, string? categories)
    {
        var parsed = ParseCategoryList(categories);
        if (parsed.Count > 0)
        {
            return parsed;
        }

        if (!File.Exists(wzPath))
        {
            return parsed;
        }

        var inferred = Path.GetFileNameWithoutExtension(wzPath);
        return string.IsNullOrWhiteSpace(inferred) ? parsed : new List<string> { inferred };
    }

    private static List<string> GetPackingCategories(string imgPath, string? category)
    {
        var parsed = ParseCategoryList(category);
        if (parsed.Count > 0)
        {
            return parsed;
        }

        return Directory.EnumerateDirectories(imgPath)
            .Where(dir => Directory.EnumerateFiles(dir, "*.img", SearchOption.AllDirectories).Any())
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name)
            .ToList();
    }

    private static List<string> ParseCategoryList(string? categories)
    {
        if (string.IsNullOrWhiteSpace(categories) || categories.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>();
        }

        return categories.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static WzMapleVersion ResolveEncryption(string? versionKey, string wzPath)
    {
        var parsed = ParseEncryption(versionKey);
        if (parsed.HasValue)
        {
            return parsed.Value;
        }

        var candidateWz = File.Exists(wzPath)
            ? wzPath
            : Directory.EnumerateFiles(wzPath, "*.wz", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(candidateWz))
        {
            try
            {
                return WzTool.DetectMapleVersion(candidateWz, out _);
            }
            catch
            {
                // Fall through to the common GMS default.
            }
        }

        return WzMapleVersion.GMS;
    }

    private static WzMapleVersion? ParseEncryption(string? versionKey)
    {
        if (string.IsNullOrWhiteSpace(versionKey) ||
            versionKey.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Enum.TryParse<WzMapleVersion>(versionKey.Trim(), true, out var parsed) &&
            parsed != WzMapleVersion.UNKNOWN &&
            parsed != WzMapleVersion.GENERATE &&
            parsed != WzMapleVersion.GETFROMZLZ)
        {
            return parsed;
        }

        throw new ArgumentException($"Unsupported WZ version key '{versionKey}'. Use GMS, EMS, BMS, CLASSIC, CUSTOM, or empty/auto.");
    }

    private static short ToPatchVersion(int wzVersion)
    {
        if (wzVersion < short.MinValue || wzVersion > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(wzVersion), "WZ version must fit in a signed 16-bit patch version.");
        }

        return (short)wzVersion;
    }
}

// Result types

public class ExtractResult : MarkdownResultBase
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? OutputDirectory { get; set; }
    public int CategoriesExtracted { get; set; }
    public int ImagesExtracted { get; set; }
    public long TotalSize { get; set; }
    public double DurationSeconds { get; set; }
    public bool ManifestCreated { get; set; }
    public List<string>? ExtractedCategories { get; set; }
    public List<string>? Errors { get; set; }
}

public class PackResult : MarkdownResultBase
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? OutputDirectory { get; set; }
    public int FilesCreated { get; set; }
    public long TotalSize { get; set; }
    public int ImagesPacked { get; set; }
    public double DurationSeconds { get; set; }
    public List<string>? PackedCategories { get; set; }
    public List<string>? CreatedFiles { get; set; }
    public List<string>? Errors { get; set; }
}

public class BatchExportImagesResult : MarkdownResultBase
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? OutputDirectory { get; set; }
    public int ExportedCount { get; set; }
    public int FailedCount { get; set; }
    public bool Truncated { get; set; }
    public List<string>? SampleExported { get; set; }
    public List<string>? Failed { get; set; }
}

public class BatchSearchResult : MarkdownResultBase
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Pattern { get; set; }
    public string? SearchType { get; set; }
    public int ResultCount { get; set; }
    public bool Truncated { get; set; }
    public List<BatchSearchMatch>? Results { get; set; }
}

public class BatchSearchMatch
{
    public required string Category { get; set; }
    public required string Image { get; set; }
    public required string Path { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public string? Value { get; set; }
}



