using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Reads key=value pairs from StreamingAssets/ApiConfig.txt at runtime.
/// The file is gitignored — copy ApiConfig.example.txt, rename it to ApiConfig.txt,
/// and fill in your real key. It is never committed to version control.
///
/// Usage:
///   string key = ConfigLoader.GetKey("DEEPSEEK_API_KEY");
/// </summary>
public static class ConfigLoader
{
    private static Dictionary<string, string> _cache;

    /// <summary>
    /// Returns the value for the given key from ApiConfig.txt.
    /// Logs an error and returns defaultValue if the file or key is missing.
    /// </summary>
    public static string GetKey(string keyName, string defaultValue = "")
    {
        if (_cache == null)
            Load();

        if (_cache.TryGetValue(keyName, out string value))
            return value;

        Debug.LogWarning($"[ConfigLoader] Key '{keyName}' not found in ApiConfig.txt. Returning default.");
        return defaultValue;
    }

    /// <summary>
    /// Loads and caches all key=value pairs from ApiConfig.txt.
    /// Called automatically on the first GetKey() call.
    /// Lines starting with '#' and blank lines are ignored.
    /// </summary>
    private static void Load()
    {
        _cache = new Dictionary<string, string>();

        string path = Path.Combine(Application.streamingAssetsPath, "ApiConfig.txt");

        if (!File.Exists(path))
        {
            Debug.LogError(
                $"[ConfigLoader] ApiConfig.txt not found at:\n{path}\n" +
                "Copy ApiConfig.example.txt → ApiConfig.txt and fill in your key."
            );
            return;
        }

        string[] lines = File.ReadAllLines(path);

        foreach (string line in lines)
        {
            string trimmed = line.Trim();

            // Skip blank lines and comments
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;

            // Split on the first '=' only
            int sep = trimmed.IndexOf('=');
            if (sep < 0)
                continue;

            string key   = trimmed.Substring(0, sep).Trim();
            string value = trimmed.Substring(sep + 1).Trim();

            _cache[key] = value;
        }

        Debug.Log($"[ConfigLoader] Loaded {_cache.Count} key(s) from ApiConfig.txt");
    }
}
