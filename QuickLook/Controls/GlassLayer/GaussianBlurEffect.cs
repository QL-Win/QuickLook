// Copyright © 2017 Paddy Xu
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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace QuickLook.Controls.GlassLayer;

public class GaussianBlurEffect : ShaderEffect
{
    public static readonly DependencyProperty InputProperty =
        RegisterPixelShaderSamplerProperty("Input", typeof(GaussianBlurEffect), 0);

    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register("Direction", typeof(Point), typeof(GaussianBlurEffect),
            new UIPropertyMetadata(new Point(0, 1), PixelShaderConstantCallback(0)));

    public static DependencyProperty ShaderProperty =
        DependencyProperty.Register("Shader", typeof(Uri), typeof(GaussianBlurEffect),
            new PropertyMetadata(null, UpdateShader()));

    public Uri Shader
    {
        get => (Uri)GetValue(ShaderProperty);
        set => SetValue(ShaderProperty, value);
    }

    public Brush Input
    {
        get => (Brush)GetValue(InputProperty);
        set => SetValue(InputProperty, value);
    }

    public Point Direction
    {
        get => (Point)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    private static PropertyChangedCallback UpdateShader()
    {
        return (obj, ea) =>
        {
            if (obj is GaussianBlurEffect instance)
            {
                instance.PixelShader = new PixelShader { UriSource = ea.NewValue as Uri };

                instance.UpdateShaderValue(InputProperty); // S0
                instance.UpdateShaderValue(DirectionProperty); // C0
                instance.DdxUvDdyUvRegisterIndex = 1; // C1
            }
        };
    }
}
