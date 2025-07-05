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

using System.Collections.Generic;
using System.IO;

namespace QuickLook.Plugin.ImageViewer.Webview.Lottie;

internal static class LottieDetector
{
    public static bool IsVaild(string path)
    {
        try
        {
            var jsonString = File.ReadAllText(path);

            // No exception will be thrown here
            var jsonLottie = LottieParser.Parse<Dictionary<string, object>>(jsonString);

            if (jsonLottie != null
             && jsonLottie.ContainsKey("v")
             && jsonLottie.ContainsKey("fr")
             && jsonLottie.ContainsKey("ip")
             && jsonLottie.ContainsKey("op")
             && jsonLottie.ContainsKey("layers"))
            {
                return true;
            }
        }
        catch
        {
            // If any exception occurs, assume it's not a valid Lottie file
        }

        return false;
    }
}
