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

using System;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace QuickLook.Helpers;

public class CommandLineParser
{
    public StringDictionary Values { get; private set; } = [];

    public CommandLineParser(string[] args = null)
    {
        args ??= Environment.GetCommandLineArgs();
        Regex spliter = new(@"^-{1,2}|^/|=|:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        Regex remover = new(@"^['""]?(.*?)['""]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        string param = null!;
        string[] parts;

        foreach (string txt in args)
        {
            parts = spliter.Split(txt, 3);

            switch (parts.Length)
            {
                case 1:
                    if (param != null)
                    {
                        if (!Values.ContainsKey(param))
                        {
                            parts[0] = remover.Replace(parts[0], "$1");

                            Values.Add(param, parts[0]);
                        }
                        param = null!;
                    }
                    break;

                case 2:
                    if (param != null)
                    {
                        if (!Values.ContainsKey(param))
                        {
                            Values.Add(param, "true");
                        }
                    }
                    param = parts[1];
                    break;

                case 3:
                    if (param != null)
                    {
                        if (!Values.ContainsKey(param))
                        {
                            Values.Add(param, "true");
                        }
                    }

                    param = parts[1];
                    if (!Values.ContainsKey(param))
                    {
                        parts[2] = remover.Replace(parts[2], "$1");
                        Values.Add(param, parts[2]);
                    }

                    param = null!;
                    break;
            }
        }
        if (param != null)
        {
            if (!Values.ContainsKey(param))
            {
                Values.Add(param, bool.TrueString);
            }
        }
    }

    public bool Has(string key) => Values.ContainsKey(key);

    public bool? GetValueBoolean(string key)
    {
        bool? ret = null;

        try
        {
            string value = Values[key];

            if (!string.IsNullOrEmpty(value))
            {
                ret = Convert.ToBoolean(value);
            }
        }
        catch
        {
        }
        return ret;
    }

    public int? GetValueInt32(string key)
    {
        int? ret = null;

        try
        {
            string value = Values[key];

            if (!string.IsNullOrEmpty(value))
            {
                ret = Convert.ToInt32(value);
            }
        }
        catch
        {
        }
        return ret;
    }

    public double? GetValueDouble(string key)
    {
        double? ret = null;

        try
        {
            string value = Values[key];

            if (!string.IsNullOrEmpty(value))
            {
                ret = Convert.ToDouble(value);
            }
        }
        catch
        {
        }
        return ret;
    }

    public bool IsValueBoolean(string key)
    {
        return GetValueBoolean(key) ?? false;
    }
}
