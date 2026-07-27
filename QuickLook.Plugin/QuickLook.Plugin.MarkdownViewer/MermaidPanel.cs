// Copyright © 2017-2026 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;

namespace QuickLook.Plugin.MarkdownViewer;

public class MermaidPanel : MarkdownPanel
{
    public static readonly string[] Extensions =
    [
        ".mermaid", // Mermaid diagram source file
    ];

    public static bool CanHandle(string path)
    {
        if (string.IsNullOrEmpty(path) || Directory.Exists(path))
            return false;

        var extension = Path.GetExtension(path);
        return Extensions.Any(ext => extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    public void PreviewMermaid(string path)
    {
        PreviewMarkdown(path);
    }

    protected override string PrepareMarkdownContent(string path, string content)
    {
        return WrapAsMermaidCodeFence(content);
    }

    internal static string WrapAsMermaidCodeFence(string content)
    {
        var normalized = content.Replace("\r\n", "\n").Trim('\n');
        return $"```mermaid\n{normalized}\n```";
    }

    internal static bool IsLikelyMermaidDocument(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        if (content.IndexOf("```mermaid", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        using var reader = new StringReader(content);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("%%", StringComparison.Ordinal))
                continue;

            return trimmed.StartsWith("graph ", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("graph", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("flowchart ", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("flowchart", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("sequenceDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("classDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("stateDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("stateDiagram-v2", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("erDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("journey", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("gantt", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("pie", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("mindmap", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("timeline", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("gitGraph", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("quadrantChart", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("requirementDiagram", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Context", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Container", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Component", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Dynamic", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("c4Deployment", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("xychart-beta", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("block-beta", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("packet-beta", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("architecture-beta", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("kanban", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("sankey-beta", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
