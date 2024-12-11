using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace QuickLook.Plugin.MarkdownViewer;

internal class ResourceManager
{
    private readonly string _resourcePath;
    private readonly string _resourcePrefix;
    private readonly string _versionFilePath;
    private readonly string _noUpdateFilePath;
    private readonly string _embeddedHash;

    public ResourceManager(string resourcePath, string resourcePrefix)
    {
        _resourcePath = resourcePath;
        _resourcePrefix = resourcePrefix;
        _versionFilePath = Path.Combine(_resourcePath, ".version");
        _noUpdateFilePath = Path.Combine(_resourcePath, ".noupdate");
        _embeddedHash = GetEmbeddedResourcesHash();
    }

    public void InitializeResources()
    {
        // Extract resources for the first time
        if (!Directory.Exists(_resourcePath))
        {
            ExtractResources();
            return;
        }

        // Check if updates are disabled
        if (File.Exists(_noUpdateFilePath))
            return;

        // Check if resources need updating by comparing hashes
        var versionInfo = ReadVersionFile();

        if (versionInfo == null)
        {
            // No version file exists, create it and extract resources
            ExtractResources();
            return;
        }

        // If embedded hash matches stored hash, no update needed
        if (_embeddedHash == versionInfo.EmbeddedHash) return;

        // Calculate current directory hash
        var currentDirectoryHash = CalculateDirectoryHash(_resourcePath);

        // If current directory matches the stored extracted hash, user hasn't modified files
        if (currentDirectoryHash == versionInfo.ExtractedHash)
        {
            // Safe to update
            ExtractResources();
            return;
        }

        // User has modified files, ask for permission to update
        var result = MessageBox.Show(
            "The MarkdownViewer resources have been updated. Would you like to update to the newest version?\n\n" +
            "Note: Your current resources appear to have been modified. Updating will overwrite your modifications.",
            "MarkdownViewer Update Available",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ExtractResources();
        }
        else
        {
            // Update the embedded hash in the version file to prevent any more prompts for this version
            UpdateVersionFileEmbeddedHash();
        }
    }

    private void ExtractResources()
    {
        // Delete and recreate directory to ensure clean state
        if (Directory.Exists(_resourcePath))
        {
            try
            {
                Directory.Delete(_resourcePath, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete directory {_resourcePath}: {ex.Message}");
                // If we can't delete the directory, we'll try to continue with existing one
            }
        }
        Directory.CreateDirectory(_resourcePath);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        foreach (var resourceName in resourceNames)
        {
            if (!resourceName.StartsWith(_resourcePrefix)) continue;

            var relativePath = resourceName.Substring(_resourcePrefix.Length);
            if (relativePath.Equals("resources", StringComparison.OrdinalIgnoreCase)) continue; // Skip 'resources' binary file

            var targetPath = Path.Combine(_resourcePath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(targetPath);
            if (directory != null)
                Directory.CreateDirectory(directory);

            // Extract the resource
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var fileStream = File.Create(targetPath))
            {
                if (stream == null) continue;
                stream.CopyTo(fileStream);
            }
        }

        // Generate version file after extracting all resources
        GenerateVersionFile();

        // Verify that md2html.html was extracted
        var htmlPath = Path.Combine(_resourcePath, "md2html.html");
        if (!File.Exists(htmlPath))
        {
            throw new FileNotFoundException($"Required template file md2html.html not found in resources. Available resources: {string.Join(", ", resourceNames)}");
        }
    }

    private class VersionInfo
    {
        public string EmbeddedHash { get; set; }
        public string ExtractedHash { get; set; }

        public VersionInfo(string embeddedHash, string extractedHash)
        {
            EmbeddedHash = embeddedHash;
            ExtractedHash = extractedHash;
        }
    }

    private static string CalculateDirectoryHash(string directory)
    {
        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".version"))
            .OrderBy(f => f)
            .ToList();

        using (var sha256 = SHA256.Create())
        {
            var combinedBytes = new List<byte>();
            foreach (var file in files)
            {
                var relativePath = file.Substring(directory.Length + 1);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLowerInvariant());
                combinedBytes.AddRange(pathBytes);

                var contentBytes = File.ReadAllBytes(file);
                combinedBytes.AddRange(contentBytes);
            }

            var hash = sha256.ComputeHash(combinedBytes.ToArray());
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    private string GetEmbeddedResourcesHash()
    {
        try
        {
            return EmbeddedResourcesHash.Hash;
        }
        catch (Exception)
        {
            Debug.WriteLine("QuickLook.Plugin.MarkdownViewer: Embedded resources hash file not found.");
            return CalculateEmbeddedResourcesHash();
        }
    }

    private string CalculateEmbeddedResourcesHash()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (var sha256 = SHA256.Create())
        {
            var combinedBytes = new List<byte>();
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith(_resourcePrefix) &&
                       !name.EndsWith(".version"))
                .OrderBy(name => name)
                .ToList();

            foreach (var resourceName in resourceNames)
            {
                var nameBytes = Encoding.UTF8.GetBytes(resourceName.ToLowerInvariant());
                combinedBytes.AddRange(nameBytes);

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;
                    var buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    combinedBytes.AddRange(buffer);
                }
            }

            var hash = sha256.ComputeHash(combinedBytes.ToArray());
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    private void GenerateVersionFile()
    {
        var extractedHash = CalculateDirectoryHash(_resourcePath);
        var newVersionContent = $"{_embeddedHash}{Environment.NewLine}{extractedHash}";
        File.WriteAllText(_versionFilePath, newVersionContent);
    }

    private void UpdateVersionFileEmbeddedHash()
    {
        var versionInfo = ReadVersionFile() ?? throw new InvalidOperationException("Cannot update version file: no existing version file found");

        var newVersionContent = $"{_embeddedHash}{Environment.NewLine}{versionInfo.ExtractedHash}";
        File.WriteAllText(_versionFilePath, newVersionContent);
    }

    private VersionInfo? ReadVersionFile()
    {
        if (!File.Exists(_versionFilePath)) return null;

        var lines = File.ReadAllLines(_versionFilePath);
        if (lines.Length < 2) return null;

        return new VersionInfo(lines[0], lines[1]);
    }
}
