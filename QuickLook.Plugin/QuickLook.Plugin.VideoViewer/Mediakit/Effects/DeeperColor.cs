using System;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WPFMediaKit.Effects;

/// <summary>
/// This is a WPF pixel shader effect that will scale 16-235 HD-TV pixel output to
/// 0-255 pixel values for deeper color on video.
/// </summary>
public class DeeperColorEffect : ShaderEffect
{
    private static string m_assemblyShortName;

    public static DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty("Input", typeof(DeeperColorEffect), 0);

    public DeeperColorEffect()
    {
        var u = new Uri(@"pack://application:,,,/" + AssemblyShortName + ";component/Effects/DeeperColor.ps");
        PixelShader = new PixelShader { UriSource = u };
        UpdateShaderValue(InputProperty);
    }

    private static string AssemblyShortName
    {
        get
        {
            if (m_assemblyShortName == null)
            {
                Assembly a = typeof(DeeperColorEffect).Assembly;
                m_assemblyShortName = a.ToString().Split(',')[0];
            }

            return m_assemblyShortName;
        }
    }

    public Brush Input
    {
        get
        {
            return ((Brush)(GetValue(InputProperty)));
        }
        set
        {
            SetValue(InputProperty, value);
        }
    }
}
