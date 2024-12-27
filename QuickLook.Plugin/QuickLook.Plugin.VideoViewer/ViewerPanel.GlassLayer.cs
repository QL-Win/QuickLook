// Copyright © 2024 QL-Win Contributors
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

using QuickLook.Common.Helpers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace QuickLook.Plugin.VideoViewer;

public partial class ViewerPanel : UserControl, IDisposable, INotifyPropertyChanged
{
    /// <summary>
    /// Load and insert the GlassLayer control to the videoControlContainer.
    /// </summary>
    private partial void LoadAndInsertGlassLayer()
    {
        // Replace XAML with C# dynamic construction
        // Implementation without dependencies of QuickLook.exe assembly

        //<glassLayer:GlassLayer xmlns:glassLayer='clr-namespace:QuickLook.Controls.GlassLayer;assembly=QuickLook'
        //                        ColorOverlayVisibility='{Binding ElementName=viewerPanel, Path=HasVideo, Converter={StaticResource BooleanToVisibilityConverter}}'
        //                        GlassVisibility='{Binding ElementName=viewerPanel, Path=HasVideo, Converter={StaticResource BooleanToVisibilityConverter}}'
        //                        OverlayColor='{DynamicResource CaptionBackground}'>
        //    <glassLayer:GlassLayer.Style>
        //        <Style TargetType='glassLayer:GlassLayer'>
        //            <Setter Property='BlurredElement' Value='{Binding ElementName=mediaElement}' />
        //            <Style.Triggers>
        //                <DataTrigger Binding='{Binding ElementName=viewerPanel, Path=HasVideo}' Value='True'>
        //                    <Setter Property='BlurredElement' Value='{Binding ElementName=mediaElement}' />
        //                </DataTrigger>
        //            </Style.Triggers>
        //        </Style>
        //    </glassLayer:GlassLayer.Style>
        //</glassLayer:GlassLayer>

        try
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName(false).Name == "QuickLook");

            var glassLayerType = loadedAssemblies?.GetType("QuickLook.Controls.GlassLayer.GlassLayer")
                ?? throw new TypeLoadException
                (
                    """
                    The type 'QuickLook.Controls.GlassLayer.GlassLayer' could not be found in the loaded assembly 'QuickLook.exe'.
                    Make sure the assembly is correctly loaded and the type exists.
                    """
                );

            // glassLayer:GlassLayer
            var glassLayerInstance = Activator.CreateInstance(glassLayerType);

            // Prepare the `SetBinding` method
            var setBindingMethod = glassLayerType.GetMethod("SetBinding", BindingFlags.Public | BindingFlags.Instance, null, [typeof(DependencyProperty), typeof(BindingBase)], null);

            // Prepare the `SetResourceReference` method
            var setResourceReferenceMethod = glassLayerType.GetMethod("SetResourceReference", BindingFlags.Public | BindingFlags.Instance);

            // ColorOverlayVisibility="{Binding ElementName=viewerPanel, Path=HasVideo, Converter={StaticResource BooleanToVisibilityConverter}}"
            var colorOverlayVisibilityProperty = glassLayerType.GetField("ColorOverlayVisibilityProperty", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                ?? throw new InvalidOperationException("ColorOverlayVisibilityProperty not found.");

            Binding colorOverlayVisibilityBinding = new(nameof(HasVideo))
            {
                ElementName = nameof(viewerPanel),
                Converter = (BooleanToVisibilityConverter)Resources[nameof(BooleanToVisibilityConverter)]
            };

            setBindingMethod.Invoke(glassLayerInstance, [colorOverlayVisibilityProperty, colorOverlayVisibilityBinding]);

            // GlassVisibility="{Binding ElementName=viewerPanel, Path=HasVideo, Converter={StaticResource BooleanToVisibilityConverter}}"
            var glassVisibilityProperty = glassLayerType.GetField("GlassVisibilityProperty", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                ?? throw new InvalidOperationException("GlassVisibilityProperty not found.");

            Binding glassVisibilityBinding = new(nameof(HasVideo))
            {
                ElementName = nameof(viewerPanel),
                Converter = (BooleanToVisibilityConverter)Resources[nameof(BooleanToVisibilityConverter)]
            };

            setBindingMethod.Invoke(glassLayerInstance, [glassVisibilityProperty, glassVisibilityBinding]);

            // OverlayColor="{DynamicResource CaptionBackground}"
            var overlayColorProperty = glassLayerType.GetField("OverlayColorProperty", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                ?? throw new InvalidOperationException("OverlayColorProperty not found.");

            setResourceReferenceMethod.Invoke(glassLayerInstance, [overlayColorProperty, "CaptionBackground"]);

            // <Style TargetType="glassLayer:GlassLayer">
            var styleConstructor = typeof(Style).GetConstructor([typeof(Type)]);
            var style = (Style)styleConstructor.Invoke([glassLayerType]);

            // <Setter Property="BlurredElement" Value="{Binding ElementName=mediaElement}" />
            var blurredElementProperty = glassLayerType.GetField("BlurredElementProperty", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                ?? throw new InvalidOperationException("BlurredElementProperty not found.");

            var blurredElementSetter = new Setter((DependencyProperty)blurredElementProperty, new Binding()
            {
                ElementName = "mediaElement"
            });
            style.Setters.Add(blurredElementSetter);

            // <DataTrigger Binding="{Binding ElementName=viewerPanel, Path=HasVideo}" Value="True">
            var dataTrigger = new DataTrigger()
            {
                Binding = new Binding("HasVideo")
                {
                    ElementName = "viewerPanel"
                },
                Value = true,
            };

            var dataTriggerSetter = new Setter((DependencyProperty)blurredElementProperty, new Binding()
            {
                ElementName = "mediaElement"
            });
            dataTrigger.Setters.Add(dataTriggerSetter);

            style.Triggers.Add(dataTrigger);

            // <glassLayer:GlassLayer.Style>
            glassLayerType.GetProperty(nameof(Style)).SetValue(glassLayerInstance, style);

            // Insert `glassLayer:GlassLayer` to `videoControlContainer` in XAML
            videoControlContainer.Children.Insert(0, (UIElement)glassLayerInstance);
        }
        catch (Exception e)
        {
            ProcessHelper.WriteLog(e.ToString());
        }
    }
}
