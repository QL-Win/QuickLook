// Copyright © 2018 Paddy Xu
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
using System.Threading;
using System.Threading.Tasks;
using Shell32;

namespace QuickLook.Helpers
{
    public class ShellLinkHelper
    {
        public static ShellLinkObject OpenShellLink(string path)
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                return StartSTATask(() => OpenShellLink(path)).Result;

            var shl = new Shell();
            var dir = shl.NameSpace(Path.GetDirectoryName(path));
            var itm = dir.Items().Item(Path.GetFileName(path));
            var lnk = (ShellLinkObject) itm.GetLink;
            return lnk;
        }

        public static string GetTarget(string path)
        {
            if (Path.GetExtension(path).ToLower() != ".lnk")
                return path;

            try
            {
                return Thread.CurrentThread.GetApartmentState() != ApartmentState.STA
                    ? StartSTATask(() => GetTarget(path)).Result
                    : OpenShellLink(path).Target.Path;
            }
            catch (Exception)
            {
                // ignored
            }

            return path;
        }

        private static Task<T> StartSTATask<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            var thread = new Thread(() =>
            {
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
    }
}