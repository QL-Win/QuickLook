// Copyright © 2017-2026 QL-Win Contributors
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

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;

public class YAMLHighlightingDefinition : DarkHighlightingDefinition
{
    public override string Name => "YAML";

    public override string Extension => ".yaml;.yml;.clang-format";

    public override HighlightingRuleSet MainRuleSet => new()
    {
        Rules =
        {
            new HighlightingRule
            {
                Regex = new Regex(@"#.*", RegexOptions.Compiled),
                Color = GetNamedColor("Comment")
            }
        }
    };

    public override HighlightingColor GetNamedColor(string name)
    {
        return name switch
        {
            "Comment" => new HighlightingColor
            {
                Name = "Comment",
                Foreground = new SimpleHighlightingBrush("#6A9949".ToColor()),
            },
            "DocumentSeparator" => new HighlightingColor
            {
                Name = "DocumentSeparator",
                Foreground = new SimpleHighlightingBrush("#A2A2A2".ToColor()),
            },
            _ => null
        };
    }

    public override IEnumerable<HighlightingColor> NamedHighlightingColors =>
    [
        GetNamedColor("Comment"),
        GetNamedColor("DocumentSeparator"),
    ];

    public override DocumentColorizingTransformer[] LineTransformers { get; } = [new KeyHighlighter()];

    public class KeyHighlighter : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            var text = CurrentContext.Document.GetText(line);

            // Handle YAML document separator lines (---)
            var trimmedStart = text.TrimStart();
            if (trimmedStart.StartsWith("---") || trimmedStart.StartsWith("..."))
            {
                int idx = trimmedStart.StartsWith("---") ? text.IndexOf("---") : text.IndexOf("...");
                if (idx < 0)
                    return;

                int sepEnd = idx + 3; // end of the '---' or '...' sequence

                // If there's a comment after the separator, color the separator and the comment separately
                int idxSharp = text.IndexOf('#', sepEnd);
                if (idxSharp >= 0)
                {
                    // Separator
                    ChangeLinePart(line.Offset + idx, line.Offset + sepEnd, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush("#A2A2A2".ToBrush());
                    });

                    // Comment
                    ChangeLinePart(line.Offset + idxSharp, line.Offset + text.Length, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush("#6A9949".ToBrush());
                    });
                }
                else
                {
                    // Only separator
                    ChangeLinePart(line.Offset + idx, line.Offset + sepEnd, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush("#A2A2A2".ToBrush());
                    });
                }

                // Return early so other colorization rules don't override this line
                return;
            }

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(text) || text.TrimStart().StartsWith("#"))
                return;

            // Detect as array or object notation
            if (text.TrimStart().StartsWith("-"))
            {
                int idx = text.IndexOf('-');
                var val = text.Substring(idx + 1); // Here +1 to skip symbol '-'
                var valTrimmed = val.Trim();
                var type = DetecteType(val);

                if (valTrimmed.Contains("#") && !(valTrimmed.StartsWith("\"") && !valTrimmed.StartsWith("'")))
                {
                    int idxSharp = text.IndexOf('#');
                    var valSharp = text.Substring(idxSharp).Trim();

                    // Value
                    ChangeLinePart(line.Offset, line.Offset + text.Length - valSharp.Length, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                    });

                    // Comment
                    ChangeLinePart(line.Offset + text.Length - valSharp.Length, line.Offset + text.Length, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush("#6A9949".ToBrush());
                    });
                }
                else
                {
                    // Value
                    ChangeLinePart(line.Offset + idx + 1, line.Offset + text.Length, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                    });
                }
                return;
            }
            // Detect as normal key-value pair
            else
            {
                int idx = text.IndexOf(':');
                var val = idx >= 0 ? text.Substring(idx + 1) : string.Empty;
                var valTrimmed = val.Trim();

                if (idx <= 0)
                {
                    // If no marker is found, it is considered a multi-line string
                    if (valTrimmed.Contains("#") && !(valTrimmed.StartsWith("\"") && !valTrimmed.StartsWith("'")))
                    {
                        int idxSharp = text.IndexOf('#');
                        var valSharp = text.Substring(idxSharp).Trim();

                        // Value
                        ChangeLinePart(line.Offset, line.Offset + text.Length - valSharp.Length, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush(GetValueColor(ValueType.String).ToBrush());
                        });

                        // Comment
                        ChangeLinePart(line.Offset + text.Length - valSharp.Length, line.Offset + text.Length, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush("#6A9949".ToBrush());
                        });
                    }
                    else
                    {
                        // Value
                        ChangeLinePart(line.Offset, line.Offset + text.Length, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush(GetValueColor(ValueType.String).ToBrush());
                        });
                    }
                    return;
                }

                // Key
                ChangeLinePart(line.Offset, line.Offset + idx, el =>
                {
                    el.TextRunProperties.SetForegroundBrush("#719BD1".ToBrush());
                });

                // Detect as Literal Block Scalar / Folded Block Scalar
                if (valTrimmed.StartsWith(">") || valTrimmed.StartsWith("|"))
                {
                    // Detect as value with comment
                    if (valTrimmed.Contains("#") && !(valTrimmed.StartsWith("\"") && !valTrimmed.StartsWith("'")))
                    {
                        int idxSharp = text.IndexOf('#');
                        var valSharp = text.Substring(idxSharp).Trim();

                        // Scalar
                        ChangeLinePart(line.Offset + idx + 1, line.Offset + idxSharp, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush("#C586C0".ToBrush());
                        });

                        // Comment
                        ChangeLinePart(line.Offset + text.Length - valSharp.Length, line.Offset + text.Length, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush("#6A9949".ToBrush());
                        });
                    }
                    else
                    {
                        // Scalar
                        ChangeLinePart(line.Offset + idx + 1, line.Offset + text.Length, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush("#C586C0".ToBrush());
                        });
                    }
                }
                // Detect as others
                else
                {
                    // Detect as value with comment
                    if (valTrimmed.Contains("#") && !(valTrimmed.StartsWith("\"") && !valTrimmed.StartsWith("'")))
                    {
                        var valWithoutSharp = valTrimmed.Substring(0, valTrimmed.IndexOf('#')).Trim();
                        int idxSharp = text.IndexOf('#');
                        var valSharp = text.Substring(idxSharp).Trim();
                        var type = DetecteType(valWithoutSharp);

                        // If value is a string, check for array or object notation
                        if (type == ValueType.String)
                        {
                            // Detect as array or object notation
                            if ((valWithoutSharp.StartsWith("[") && valWithoutSharp.EndsWith("]"))
                             || (valWithoutSharp.StartsWith("{") && valWithoutSharp.EndsWith("}")))
                            {
                                var parsed = InlineTextParser.ParseInlineText(val);

                                // Value
                                foreach (var item in parsed)
                                {
                                    if (item.Type == InlineType.None) continue;

                                    var startIndex = (idx + 1) + item.StartIndex;
                                    var count = item.EndIndex - item.StartIndex;
                                    var dd = text.Substring(startIndex, count);

                                    ChangeLinePart(line.Offset + startIndex, line.Offset + startIndex + count, el =>
                                    {
                                        el.TextRunProperties.SetForegroundBrush(item.GetValueColor().ToBrush());
                                    });
                                }
                            }
                            // Detect as normal value
                            else
                            {
                                // Value
                                ChangeLinePart(line.Offset + idx + 1, line.Offset + text.Length, el =>
                                {
                                    el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                                });
                            }
                        }
                        else
                        {
                            // Value
                            ChangeLinePart(line.Offset + idx + 1, line.Offset + text.Length, el =>
                            {
                                el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                            });
                        }

                        // Comment
                        ChangeLinePart(line.Offset + text.Length - valSharp.Length, line.Offset + text.Length, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush("#6A9949".ToBrush());
                        });
                    }
                    // Detect as value without comment
                    else
                    {
                        var type = DetecteType(val);

                        // If value is a string, check for array or object notation
                        if (type == ValueType.String)
                        {
                            // Detect as array or object notation
                            if ((valTrimmed.StartsWith("[") && valTrimmed.EndsWith("]"))
                             || (valTrimmed.StartsWith("{") && valTrimmed.EndsWith("}")))
                            {
                                var parsed = InlineTextParser.ParseInlineText(val);

                                // Value
                                foreach (var item in parsed)
                                {
                                    if (item.Type == InlineType.None) continue;

                                    var startIndex = (idx + 1) + item.StartIndex;
                                    var count = item.EndIndex - item.StartIndex;
                                    var dd = text.Substring(startIndex, count);

                                    ChangeLinePart(line.Offset + startIndex, line.Offset + startIndex + count, el =>
                                    {
                                        el.TextRunProperties.SetForegroundBrush(item.GetValueColor().ToBrush());
                                    });
                                }
                            }
                            else
                            {
                                ChangeLinePart(line.Offset + idx + 1, line.Offset + text.Length, el =>
                                {
                                    el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                                });
                            }
                        }
                        else
                        {
                            ChangeLinePart(line.Offset + idx + 1, line.Offset + text.Length, el =>
                            {
                                el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                            });
                        }
                    }
                }
            }
        }

        private ValueType DetecteType(string input)
        {
            if (double.TryParse(input, out _))
                return ValueType.Numeric;

            if (bool.TryParse(input, out _))
                return ValueType.Boolean;

            return ValueType.String;
        }

        private string GetValueColor(ValueType type)
        {
            return type switch
            {
                ValueType.Numeric => "#B5CEA8",
                ValueType.Boolean => "#719BD1",
                ValueType.String or _ => "#CE9178",
            };
        }

        private enum ValueType
        {
            String,
            Numeric,
            Boolean,
        }

        private static class InlineTextParser
        {
            public static InlineText[] ParseInlineText(string input)
            {
                var result = new List<InlineText>();
                int index = 0;
                var aoSymbol = default(char); // Array/Object symbol
                var prevSymbol = default(char);

                while (index < input.Length)
                {
                    char c = input[index];

                    // Symbol token
                    if ("{}[]:,".Contains(c))
                    {
                        if (c == '[' || c == '{')
                        {
                            aoSymbol = c;
                        }
                        prevSymbol = c;
                        result.Add(new InlineText(c.ToString(), c switch
                        {
                            '[' or ']' or '{' or '}' => InlineType.Symbol,
                            ':' or ',' or _ => InlineType.None,
                        }, index, index + 1));
                        index++;
                    }
                    // Quoted string
                    else if (c == '\'' || c == '"')
                    {
                        int start = index;
                        index++;
                        while (index < input.Length && input[index] != c)
                            index++;

                        index++; // include closing quote
                        result.Add(new InlineText(input.Substring(start, index - start), InlineType.ValueString, start, index));
                    }
                    // Key or value (word or number)
                    else if (char.IsLetterOrDigit(c) || c == '-' || c == '>' || c == '<' || c == '=')
                    {
                        int start = index;
                        while (index < input.Length && (char.IsLetterOrDigit(input[index]) || "><=-.".Contains(input[index])))
                            index++;

                        string token = input.Substring(start, index - start);
                        result.Add(new InlineText(token, aoSymbol switch
                        {
                            '[' => double.TryParse(token, out _) ? InlineType.ValueNumeric : InlineType.ValueString,
                            '{' or _ => prevSymbol switch
                            {
                                ':' => InlineType.ValueNumeric,
                                ',' or _ => InlineType.Key,
                            }
                        }, start, index));
                    }
                    // Whitespace, skip
                    else if (char.IsWhiteSpace(c))
                    {
                        result.Add(new InlineText(c.ToString(), InlineType.None, index, index + 1));
                        index++;
                    }
                    else
                    {
                        // Unknown single char token
                        result.Add(new InlineText(c.ToString(), InlineType.None, index, index + 1));
                        index++;
                    }
                }

                return [.. result];
            }
        }

        [DebuggerDisplay("{Text}: {Type} [{StartIndex}, {EndIndex}]")]
        private sealed class InlineText(string text, InlineType type, int start, int end)
        {
            public string Text { get; set; } = text;

            public InlineType Type { get; set; } = type;

            public int StartIndex { get; set; } = start;

            public int EndIndex { get; set; } = end;

            public string GetValueColor()
            {
                return Type switch
                {
                    InlineType.Symbol => "#FFD700",
                    InlineType.ValueNumeric => "#B5CEA8",
                    InlineType.Key or InlineType.ValueBoolean => "#719BD1",
                    InlineType.ValueString or _ => "#CE9178",
                };
            }
        }

        public enum InlineType
        {
            None,
            Key,
            ValueString,
            ValueNumeric,
            ValueBoolean,
            Symbol, // e.g. [ ] { } etc.
        }
    }
}
