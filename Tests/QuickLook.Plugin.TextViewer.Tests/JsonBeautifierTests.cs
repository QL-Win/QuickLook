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

using QuickLook.Plugin.TextViewer;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace QuickLook.Plugin.TextViewer.Tests;

public class JsonBeautifierTests
{
    [Fact]
    public void IndentsNestedObjectsAndArrays()
    {
        Assert.True(JsonBeautifier.TryFormat("{\"a\":[1,{\"b\":true}],\"empty\":{}}", out var formatted));
        Assert.Equal(string.Join(Environment.NewLine,
            "{", "  \"a\": [", "    1,", "    {", "      \"b\": true", "    }", "  ],", "  \"empty\": {}", "}"), formatted);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("-0")]
    [InlineData("123")]
    [InlineData("1.2300E+99999")]
    [InlineData("\"Привет 東京 🌍\"")]
    [InlineData("{\"x\":\" \\u041f \\b \\n \\\\ \\\"\"}")]
    [InlineData(" \n[1,2,{},[],[true,false,null]]\t")]
    public void PreservesTokensAndIsIdempotent(string source)
    {
        Assert.True(JsonBeautifier.TryFormat(source, out var formatted));
        Assert.Equal(Tokens(source), Tokens(formatted));
        Assert.True(JsonBeautifier.TryFormat(formatted, out var twice));
        Assert.Equal(formatted, twice);
    }

    [Fact]
    public void PreservesExactNumbersAndDuplicateKeys()
    {
        const string source = "{\"n\":900719925474099312345678901234567890,\"n\":0.123456789012345678901234567890,\"n\":1e-10000,\"n\":1.234567890123456789e+10000,\"n\":-0,\"n\":1.2300}";
        Assert.True(JsonBeautifier.TryFormat(source, out var formatted));
        Assert.Equal(source, Tokens(formatted));
    }

    [Fact]
    public void AcceptsLeadingDecodedBom()
    {
        Assert.True(JsonBeautifier.TryFormat("\uFEFF{\"a\":1}", out var formatted));
        Assert.Equal("{\"a\":1}", Tokens(formatted));
    }

    [Fact]
    public void HandlesCompactSourceLongerThanPreviewLineLimit()
    {
        string source = "[" + string.Join(",", Enumerable.Repeat("{\"name\":\"Привет\",\"id\":12345678901234567890}", 1000)) + "]";
        Assert.True(source.Length > 10000);
        Assert.True(JsonBeautifier.TryFormat(source, out var formatted));
        Assert.Equal(source, Tokens(formatted));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("{")]
    [InlineData("[")]
    [InlineData("{\"a\":1,}")]
    [InlineData("[1,2,]")]
    [InlineData("[01]")]
    [InlineData("[-01]")]
    [InlineData("[+1]")]
    [InlineData("[1.]")]
    [InlineData("[.1]")]
    [InlineData("[1e]")]
    [InlineData("[1e+]")]
    [InlineData("[NaN]")]
    [InlineData("[Infinity]")]
    [InlineData("undefined")]
    [InlineData("true false")]
    [InlineData("{}[]")]
    [InlineData("{a:1}")]
    [InlineData("{'a':1}")]
    [InlineData("{\"a\" 1}")]
    [InlineData("[1 2]")]
    [InlineData("\"\\x12\"")]
    [InlineData("\"\\u123\"")]
    [InlineData("\"\\uZZZZ\"")]
    [InlineData("\"raw\nline\"")]
    [InlineData("\"unterminated")]
    [InlineData("/*hi*/{}")]
    [InlineData("[1,//hi\n2]")]
    [InlineData("[truex]")]
    [InlineData("\u00a0{}")]
    [InlineData("{}\uFEFF")]
    public void RejectsInvalidJsonWithoutReturningPartialOutput(string source)
    {
        Assert.False(JsonBeautifier.TryFormat(source, out var formatted));
        Assert.Null(formatted);
    }

    [Fact]
    public void EnforcesDepthAndOutputLimits()
    {
        Assert.True(JsonBeautifier.TryFormat(new string('[', 128) + "0" + new string(']', 128), out _));
        Assert.False(JsonBeautifier.TryFormat(new string('[', 129) + "0" + new string(']', 129), out _));
        Assert.False(JsonBeautifier.TryFormat("\"" + new string('a', 21 * 1024 * 1024) + "\"", out _));
    }

    [Fact]
    public void EverySupportedLocaleHasBothNewStrings()
    {
        var translations = XDocument.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations.config"));
        foreach (var locale in translations.Root.Elements())
        {
            foreach (string key in new[] { "MW_BeautifyJson", "MW_BeautifyJsonInvalid" })
                Assert.False(string.IsNullOrWhiteSpace(locale.Element(key)?.Value), locale.Name + ": " + key);
        }
    }

    private static string Tokens(string text)
    {
        var result = new StringBuilder();
        bool quoted = false, escaped = false;
        foreach (char c in text)
        {
            if (quoted)
            {
                result.Append(c);
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') quoted = false;
            }
            else if (c == '"') { result.Append(c); quoted = true; }
            else if (!(c is ' ' or '\t' or '\n' or '\r')) result.Append(c);
        }
        return result.ToString();
    }
}
