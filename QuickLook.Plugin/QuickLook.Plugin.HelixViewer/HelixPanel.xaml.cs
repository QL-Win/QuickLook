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
using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace QuickLook.Plugin.HelixViewer;

public partial class HelixPanel : UserControl
{
    private readonly string _path;

    public HelixPanel(string path) : this()
    {
        _path = path;
        Load();
    }

    public HelixPanel()
    {
        InitializeComponent();
    }

    private void Load()
    {
        if (_path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
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
}
