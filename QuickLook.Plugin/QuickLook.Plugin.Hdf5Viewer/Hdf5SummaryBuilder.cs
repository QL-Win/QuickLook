using HDF5.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuickLook.Plugin.Hdf5Viewer;

internal static class Hdf5SummaryBuilder
{
    private const int MaxDepth = 16;
    private const int MaxChildrenPerGroup = 250;
    private const int MaxAttributesPerObject = 20;

    public static string Build(string path)
    {
        var sb = new StringBuilder(32 * 1024);

        using (var file = H5File.OpenRead(path))
        {
            sb.AppendLine("HDF5 structure summary");
            sb.AppendLine();

            AppendObject(file, sb, depth: 0);
        }

        return sb.ToString();
    }

    private static void AppendObject(H5Object h5Object, StringBuilder sb, int depth)
    {
        if (depth > MaxDepth)
        {
            sb.AppendLine($"{Indent(depth)}... depth limit reached");
            return;
        }

        switch (h5Object)
        {
            case H5Group group:
                AppendGroup(group, sb, depth);
                return;
            case H5Dataset dataset:
                AppendDataset(dataset, sb, depth);
                return;
            case H5CommitedDatatype committedDatatype:
                sb.AppendLine($"{Indent(depth)}[DATATYPE] {SafeName(committedDatatype.Name)}");
                return;
            case H5UnresolvedLink unresolvedLink:
                sb.AppendLine($"{Indent(depth)}[UNRESOLVED] {SafeName(unresolvedLink.Name)}");
                return;
            default:
                sb.AppendLine($"{Indent(depth)}[{h5Object.GetType().Name}] {SafeName(h5Object.Name)}");
                return;
        }
    }

    private static void AppendGroup(H5Group group, StringBuilder sb, int depth)
    {
        sb.AppendLine($"{Indent(depth)}[GROUP] {SafeName(group.Name)}");
        AppendAttributes(group, sb, depth + 1);

        var children = SafeReadChildren(group).ToList();
        var visibleChildren = children.Take(MaxChildrenPerGroup).ToList();

        foreach (var child in visibleChildren)
            AppendObject(child, sb, depth + 1);

        if (children.Count > MaxChildrenPerGroup)
            sb.AppendLine($"{Indent(depth + 1)}... {children.Count - MaxChildrenPerGroup} more children");
    }

    private static void AppendDataset(H5Dataset dataset, StringBuilder sb, int depth)
    {
        var dimensions = string.Join(" x ", dataset.Space.Dimensions.Select(d => d.ToString()));
        var shape = string.IsNullOrWhiteSpace(dimensions) ? "scalar" : dimensions;

        sb.AppendLine(
            $"{Indent(depth)}[DATASET] {SafeName(dataset.Name)} | " +
            $"shape={shape}, dtype={dataset.Type.Class}, itemSize={dataset.Type.Size}B, layout={dataset.Layout.Class}");

        AppendAttributes(dataset, sb, depth + 1);
    }

    private static void AppendAttributes(H5AttributableObject attributable, StringBuilder sb, int depth)
    {
        var visible = new List<H5Attribute>(MaxAttributesPerObject);
        var hasMoreAttributes = false;

        try
        {
            var count = 0;

            foreach (var attribute in attributable.Attributes)
            {
                count++;

                if (count <= MaxAttributesPerObject)
                {
                    visible.Add(attribute);
                }
                else
                {
                    hasMoreAttributes = true;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"{Indent(depth)}@attributes: <error: {ex.Message}>");
            return;
        }

        if (visible.Count == 0)
            return;

        foreach (var attribute in visible)
        {
            var dimensions = string.Join(" x ", attribute.Space.Dimensions.Select(d => d.ToString()));
            var shape = string.IsNullOrWhiteSpace(dimensions) ? "scalar" : dimensions;

            sb.AppendLine(
                $"{Indent(depth)}@{SafeName(attribute.Name)} " +
                $"(type={attribute.Type.Class}, shape={shape}, itemSize={attribute.Type.Size}B)");
        }

        if (hasMoreAttributes)
            sb.AppendLine($"{Indent(depth)}... more attributes");
    }

    private static IEnumerable<H5Object> SafeReadChildren(H5Group group)
    {
        try
        {
            return group.Children;
        }
        catch
        {
            return Array.Empty<H5Object>();
        }
    }

    private static string SafeName(string name)
    {
        return string.IsNullOrEmpty(name) ? "/" : name;
    }

    private static string Indent(int level)
    {
        return new string(' ', level * 2);
    }
}
