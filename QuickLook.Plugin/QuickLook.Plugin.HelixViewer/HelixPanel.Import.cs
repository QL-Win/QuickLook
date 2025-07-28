// Copyright © 2017-2025 QL-Win Contributors
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

using Assimp;
using HelixToolkit.Wpf;
using PcdSharp.IO;
using PcdSharp.Struct;
using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace QuickLook.Plugin.HelixViewer;

public partial class HelixPanel
{
    private void Load()
    {
        var importerType = Importer.GetImporterType(_path);

        try
        {
            if (importerType == ImporterType.Extended)
            {
                var context = new AssimpContext();
                var scene = context.ImportFile(_path, PostProcessSteps.Triangulate);

                foreach (var mesh in scene.Meshes)
                {
                    var geometry = new MeshGeometry3D()
                    {
                        Positions = [.. mesh.Vertices.Select(v => new Point3D(v.X, v.Y, v.Z))],
                        TriangleIndices = [.. mesh.GetIndices()],
                    };
                    var model = new GeometryModel3D()
                    {
                        Geometry = geometry,
                        Material = Materials.Gray,
                    };

                    modelVisual.Content = model;
                }
            }
            if (importerType == ImporterType.Extended_PCD)
            {
                // Only support PCD files with PointXYZ format
                // Not supported for Color or Intensity formats
                var xyzCloud = PCDReader.Read<PointXYZ>(_path);

                // Create a single geometry for all points to improve performance
                var pointCloudGeometry = new MeshGeometry3D();
                var positions = new Point3DCollection();
                var triangleIndices = new Int32Collection();

                // Create material for points
                var pointMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0x60, 0x80, 0xFF)));

                // Adaptive point size based on total number of points
                var totalPoints = xyzCloud.Points.Count;
                var pointSize = totalPoints > 10000 ? 0.05d : totalPoints > 1000 ? 0.1d : 0.2d;

                // Limit points for performance (show every Nth point if too many)
                var step = Math.Max(1, totalPoints / 50000); // Limit to ~50k points max

                var vertexIndex = 0;
                for (int i = 0; i < xyzCloud.Points.Count; i += step)
                {
                    var point = xyzCloud.Points[i];

                    // Create a small cube for each point
                    var halfSize = pointSize / 2;

                    // Add 8 vertices for the cube
                    var baseIndex = vertexIndex;

                    // Front face vertices
                    positions.Add(new Point3D(point.X - halfSize, point.Y - halfSize, point.Z + halfSize));
                    positions.Add(new Point3D(point.X + halfSize, point.Y - halfSize, point.Z + halfSize));
                    positions.Add(new Point3D(point.X + halfSize, point.Y + halfSize, point.Z + halfSize));
                    positions.Add(new Point3D(point.X - halfSize, point.Y + halfSize, point.Z + halfSize));

                    // Back face vertices
                    positions.Add(new Point3D(point.X - halfSize, point.Y - halfSize, point.Z - halfSize));
                    positions.Add(new Point3D(point.X + halfSize, point.Y - halfSize, point.Z - halfSize));
                    positions.Add(new Point3D(point.X + halfSize, point.Y + halfSize, point.Z - halfSize));
                    positions.Add(new Point3D(point.X - halfSize, point.Y + halfSize, point.Z - halfSize));

                    // Add triangle indices for the cube (12 triangles, 36 indices)
                    // Front face
                    triangleIndices.Add(baseIndex + 0); triangleIndices.Add(baseIndex + 1); triangleIndices.Add(baseIndex + 2);
                    triangleIndices.Add(baseIndex + 0); triangleIndices.Add(baseIndex + 2); triangleIndices.Add(baseIndex + 3);
                    // Back face
                    triangleIndices.Add(baseIndex + 4); triangleIndices.Add(baseIndex + 6); triangleIndices.Add(baseIndex + 5);
                    triangleIndices.Add(baseIndex + 4); triangleIndices.Add(baseIndex + 7); triangleIndices.Add(baseIndex + 6);
                    // Left face
                    triangleIndices.Add(baseIndex + 4); triangleIndices.Add(baseIndex + 0); triangleIndices.Add(baseIndex + 3);
                    triangleIndices.Add(baseIndex + 4); triangleIndices.Add(baseIndex + 3); triangleIndices.Add(baseIndex + 7);
                    // Right face
                    triangleIndices.Add(baseIndex + 1); triangleIndices.Add(baseIndex + 5); triangleIndices.Add(baseIndex + 6);
                    triangleIndices.Add(baseIndex + 1); triangleIndices.Add(baseIndex + 6); triangleIndices.Add(baseIndex + 2);
                    // Top face
                    triangleIndices.Add(baseIndex + 3); triangleIndices.Add(baseIndex + 2); triangleIndices.Add(baseIndex + 6);
                    triangleIndices.Add(baseIndex + 3); triangleIndices.Add(baseIndex + 6); triangleIndices.Add(baseIndex + 7);
                    // Bottom face
                    triangleIndices.Add(baseIndex + 4); triangleIndices.Add(baseIndex + 5); triangleIndices.Add(baseIndex + 1);
                    triangleIndices.Add(baseIndex + 4); triangleIndices.Add(baseIndex + 1); triangleIndices.Add(baseIndex + 0);

                    vertexIndex += 8;
                }

                pointCloudGeometry.Positions = positions;
                pointCloudGeometry.TriangleIndices = triangleIndices;

                // Create the model
                var pointCloudModel = new GeometryModel3D
                {
                    Geometry = pointCloudGeometry,
                    Material = pointMaterial,
                    BackMaterial = pointMaterial
                };

                modelVisual.Content = pointCloudModel;
            }
            else
            {
                var modelImporter = new ModelImporter();
                var model3DGroup = modelImporter.Load(_path);
                var diffuseMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)));

                foreach (GeometryModel3D child in model3DGroup.Children.Cast<GeometryModel3D>())
                {
                    child.Material = diffuseMaterial;
                    child.BackMaterial = diffuseMaterial;
                }

                modelVisual.Content = model3DGroup;
            }
        }
        catch (Exception ex)
        {
            errorInfo.Text = $"[{nameof(ImporterType)}.{importerType}] {ex}";
            errorInfo.Visibility = System.Windows.Visibility.Visible;
            viewer.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}

file static class Importer
{
    public static ImporterType GetImporterType(string path)
    {
        if (string.IsNullOrEmpty(path))
            return ImporterType.Unknown;

        return Path.GetExtension(path).ToLower() switch
        {
            ".stl" or ".obj" or ".3ds" or ".lwo" or ".ply" => ImporterType.Default,
            ".fbx" or ".3mf" or ".glb" or ".gltf" or ".dae" or ".dxf" => ImporterType.Extended,
            ".pmx" => ImporterType.Extended_MMD,
            ".pcd" => ImporterType.Extended_PCD,
            _ => ImporterType.Unknown,
        };
    }
}

file enum ImporterType
{
    /// <summary>
    /// Reserved or unspecified import type
    /// </summary>
    Unknown,

    /// <summary>
    /// Default importer supported by HelixToolkit
    /// </summary>
    Default,

    /// <summary>
    /// Extended importer supported by Assimp
    /// </summary>
    Extended,

    /// <summary>
    /// Extended MMD (MikuMikuDance) importer
    /// </summary>
    Extended_MMD,

    /// <summary>
    /// Extended PCD (Point Cloud Data) importer for 3D spatial data
    /// </summary>
    Extended_PCD,
}
