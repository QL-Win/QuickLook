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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace QuickLook.Controls.GlassLayer
{
    /// <summary>
    ///     Interaction logic for GlassLayer.xaml
    /// </summary>
    public partial class GlassLayer : UserControl
    {
        public GlassLayer()
        {
            InitializeComponent();
        }

        #region public Visual BlurredElement

        /// <summary>
        ///     Identifies the BlurredElement dependency property.
        /// </summary>
        public static DependencyProperty BlurredElementProperty =
            DependencyProperty.Register("BlurredElement", typeof(Visual), typeof(GlassLayer), null);

        /// <summary>
        /// </summary>
        public Visual BlurredElement
        {
            get => (Visual) GetValue(BlurredElementProperty);

            set => SetValue(BlurredElementProperty, value);
        }

        #endregion public Visual BlurredElement

        #region public double GlassOpacity

        /// <summary>
        ///     Identifies the GlassOpacity dependency property.
        /// </summary>
        public static DependencyProperty GlassOpacityProperty =
            DependencyProperty.Register("GlassOpacity", typeof(double), typeof(GlassLayer),
                new UIPropertyMetadata(0.6));

        /// <summary>
        /// </summary>
        public double GlassOpacity
        {
            get => (double) GetValue(GlassOpacityProperty);

            set => SetValue(GlassOpacityProperty, value);
        }

        #endregion public double GlassOpacity

        #region public Visibility NoiseVisibility

        /// <summary>
        ///     Identifies the NoiseVisibility dependency property.
        /// </summary>
        public static DependencyProperty NoiseVisibilityProperty =
            DependencyProperty.Register("NoiseVisibility", typeof(Visibility), typeof(GlassLayer),
                new UIPropertyMetadata(Visibility.Visible));

        /// <summary>
        /// </summary>
        public Visibility NoiseVisibility
        {
            get => (Visibility) GetValue(NoiseVisibilityProperty);

            set => SetValue(NoiseVisibilityProperty, value);
        }

        #endregion public Visibility NoiseVisibility
    }
}