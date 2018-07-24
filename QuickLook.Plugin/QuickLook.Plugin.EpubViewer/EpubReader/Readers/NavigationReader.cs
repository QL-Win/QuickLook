// Copyright © 2018 Marco Gavelli and Paddy Xu
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
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using VersOne.Epub.Schema;

namespace VersOne.Epub.Internal
{
    internal static class NavigationReader
    {
        public static async Task<EpubNavigation> ReadNavigationAsync(ZipArchive epubArchive,
            string contentDirectoryPath, EpubPackage package)
        {
            var result = new EpubNavigation();
            var tocId = package.Spine.Toc;
            if (string.IsNullOrEmpty(tocId)) throw new Exception("EPUB parsing error: TOC ID is empty.");
            var tocManifestItem = package.Manifest.FirstOrDefault(item =>
                string.Compare(item.Id, tocId, StringComparison.OrdinalIgnoreCase) == 0);
            if (tocManifestItem == null)
                throw new Exception(
                    string.Format("EPUB parsing error: TOC item {0} not found in EPUB manifest.", tocId));
            var tocFileEntryPath = ZipPathUtils.Combine(contentDirectoryPath, tocManifestItem.Href);
            var tocFileEntry = epubArchive.GetEntry(tocFileEntryPath);
            if (tocFileEntry == null)
                throw new Exception(string.Format("EPUB parsing error: TOC file {0} not found in archive.",
                    tocFileEntryPath));
            if (tocFileEntry.Length > int.MaxValue)
                throw new Exception(string.Format("EPUB parsing error: TOC file {0} is larger than 2 Gb.",
                    tocFileEntryPath));
            XDocument containerDocument;
            using (var containerStream = tocFileEntry.Open())
            {
                containerDocument = await XmlUtils.LoadDocumentAsync(containerStream).ConfigureAwait(false);
            }

            XNamespace ncxNamespace = "http://www.daisy.org/z3986/2005/ncx/";
            var ncxNode = containerDocument.Element(ncxNamespace + "ncx");
            if (ncxNode == null) throw new Exception("EPUB parsing error: TOC file does not contain ncx element.");
            var headNode = ncxNode.Element(ncxNamespace + "head");
            if (headNode == null) throw new Exception("EPUB parsing error: TOC file does not contain head element.");
            var navigationHead = ReadNavigationHead(headNode);
            result.Head = navigationHead;
            var docTitleNode = ncxNode.Element(ncxNamespace + "docTitle");
            if (docTitleNode == null)
                throw new Exception("EPUB parsing error: TOC file does not contain docTitle element.");
            var navigationDocTitle = ReadNavigationDocTitle(docTitleNode);
            result.DocTitle = navigationDocTitle;
            result.DocAuthors = new List<EpubNavigationDocAuthor>();
            foreach (var docAuthorNode in ncxNode.Elements(ncxNamespace + "docAuthor"))
            {
                var navigationDocAuthor = ReadNavigationDocAuthor(docAuthorNode);
                result.DocAuthors.Add(navigationDocAuthor);
            }

            var navMapNode = ncxNode.Element(ncxNamespace + "navMap");
            if (navMapNode == null)
                throw new Exception("EPUB parsing error: TOC file does not contain navMap element.");
            var navMap = ReadNavigationMap(navMapNode);
            result.NavMap = navMap;
            var pageListNode = ncxNode.Element(ncxNamespace + "pageList");
            if (pageListNode != null)
            {
                var pageList = ReadNavigationPageList(pageListNode);
                result.PageList = pageList;
            }

            result.NavLists = new List<EpubNavigationList>();
            foreach (var navigationListNode in ncxNode.Elements(ncxNamespace + "navList"))
            {
                var navigationList = ReadNavigationList(navigationListNode);
                result.NavLists.Add(navigationList);
            }

            return result;
        }

        private static EpubNavigationHead ReadNavigationHead(XElement headNode)
        {
            var result = new EpubNavigationHead();
            foreach (var metaNode in headNode.Elements())
                if (string.Compare(metaNode.Name.LocalName, "meta", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var meta = new EpubNavigationHeadMeta();
                    foreach (var metaNodeAttribute in metaNode.Attributes())
                    {
                        var attributeValue = metaNodeAttribute.Value;
                        switch (metaNodeAttribute.Name.LocalName.ToLowerInvariant())
                        {
                            case "name":
                                meta.Name = attributeValue;
                                break;
                            case "content":
                                meta.Content = attributeValue;
                                break;
                            case "scheme":
                                meta.Scheme = attributeValue;
                                break;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(meta.Name))
                        throw new Exception("Incorrect EPUB navigation meta: meta name is missing.");
                    if (meta.Content == null)
                        throw new Exception("Incorrect EPUB navigation meta: meta content is missing.");
                    result.Add(meta);
                }

            return result;
        }

        private static EpubNavigationDocTitle ReadNavigationDocTitle(XElement docTitleNode)
        {
            var result = new EpubNavigationDocTitle();
            foreach (var textNode in docTitleNode.Elements())
                if (string.Compare(textNode.Name.LocalName, "text", StringComparison.OrdinalIgnoreCase) == 0)
                    result.Add(textNode.Value);
            return result;
        }

        private static EpubNavigationDocAuthor ReadNavigationDocAuthor(XElement docAuthorNode)
        {
            var result = new EpubNavigationDocAuthor();
            foreach (var textNode in docAuthorNode.Elements())
                if (string.Compare(textNode.Name.LocalName, "text", StringComparison.OrdinalIgnoreCase) == 0)
                    result.Add(textNode.Value);
            return result;
        }

        private static EpubNavigationMap ReadNavigationMap(XElement navigationMapNode)
        {
            var result = new EpubNavigationMap();
            foreach (var navigationPointNode in navigationMapNode.Elements())
                if (string.Compare(navigationPointNode.Name.LocalName, "navPoint",
                        StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var navigationPoint = ReadNavigationPoint(navigationPointNode);
                    result.Add(navigationPoint);
                }

            return result;
        }

        private static EpubNavigationPoint ReadNavigationPoint(XElement navigationPointNode)
        {
            var result = new EpubNavigationPoint();
            foreach (var navigationPointNodeAttribute in navigationPointNode.Attributes())
            {
                var attributeValue = navigationPointNodeAttribute.Value;
                switch (navigationPointNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "id":
                        result.Id = attributeValue;
                        break;
                    case "class":
                        result.Class = attributeValue;
                        break;
                    case "playOrder":
                        result.PlayOrder = attributeValue;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(result.Id))
                throw new Exception("Incorrect EPUB navigation point: point ID is missing.");
            result.NavigationLabels = new List<EpubNavigationLabel>();
            result.ChildNavigationPoints = new List<EpubNavigationPoint>();
            foreach (var navigationPointChildNode in navigationPointNode.Elements())
                switch (navigationPointChildNode.Name.LocalName.ToLowerInvariant())
                {
                    case "navlabel":
                        var navigationLabel = ReadNavigationLabel(navigationPointChildNode);
                        result.NavigationLabels.Add(navigationLabel);
                        break;
                    case "content":
                        var content = ReadNavigationContent(navigationPointChildNode);
                        result.Content = content;
                        break;
                    case "navpoint":
                        var childNavigationPoint = ReadNavigationPoint(navigationPointChildNode);
                        result.ChildNavigationPoints.Add(childNavigationPoint);
                        break;
                }
            if (!result.NavigationLabels.Any())
                throw new Exception(string.Format(
                    "EPUB parsing error: navigation point {0} should contain at least one navigation label.",
                    result.Id));
            if (result.Content == null)
                throw new Exception(string.Format("EPUB parsing error: navigation point {0} should contain content.",
                    result.Id));
            return result;
        }

        private static EpubNavigationLabel ReadNavigationLabel(XElement navigationLabelNode)
        {
            var result = new EpubNavigationLabel();
            var navigationLabelTextNode = navigationLabelNode.Element(navigationLabelNode.Name.Namespace + "text");
            if (navigationLabelTextNode == null)
                throw new Exception("Incorrect EPUB navigation label: label text element is missing.");
            result.Text = navigationLabelTextNode.Value;
            return result;
        }

        private static EpubNavigationContent ReadNavigationContent(XElement navigationContentNode)
        {
            var result = new EpubNavigationContent();
            foreach (var navigationContentNodeAttribute in navigationContentNode.Attributes())
            {
                var attributeValue = navigationContentNodeAttribute.Value;
                switch (navigationContentNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "id":
                        result.Id = attributeValue;
                        break;
                    case "src":
                        result.Source = attributeValue;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(result.Source))
                throw new Exception("Incorrect EPUB navigation content: content source is missing.");
            return result;
        }

        private static EpubNavigationPageList ReadNavigationPageList(XElement navigationPageListNode)
        {
            var result = new EpubNavigationPageList();
            foreach (var pageTargetNode in navigationPageListNode.Elements())
                if (string.Compare(pageTargetNode.Name.LocalName, "pageTarget", StringComparison.OrdinalIgnoreCase) ==
                    0)
                {
                    var pageTarget = ReadNavigationPageTarget(pageTargetNode);
                    result.Add(pageTarget);
                }

            return result;
        }

        private static EpubNavigationPageTarget ReadNavigationPageTarget(XElement navigationPageTargetNode)
        {
            var result = new EpubNavigationPageTarget();
            foreach (var navigationPageTargetNodeAttribute in navigationPageTargetNode.Attributes())
            {
                var attributeValue = navigationPageTargetNodeAttribute.Value;
                switch (navigationPageTargetNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "id":
                        result.Id = attributeValue;
                        break;
                    case "value":
                        result.Value = attributeValue;
                        break;
                    case "type":
                        EpubNavigationPageTargetType type;
                        if (!Enum.TryParse(attributeValue, out type))
                            throw new Exception(string.Format(
                                "Incorrect EPUB navigation page target: {0} is incorrect value for page target type.",
                                attributeValue));
                        result.Type = type;
                        break;
                    case "class":
                        result.Class = attributeValue;
                        break;
                    case "playOrder":
                        result.PlayOrder = attributeValue;
                        break;
                }
            }

            if (result.Type == default(EpubNavigationPageTargetType))
                throw new Exception("Incorrect EPUB navigation page target: page target type is missing.");
            foreach (var navigationPageTargetChildNode in navigationPageTargetNode.Elements())
                switch (navigationPageTargetChildNode.Name.LocalName.ToLowerInvariant())
                {
                    case "navlabel":
                        var navigationLabel = ReadNavigationLabel(navigationPageTargetChildNode);
                        result.NavigationLabels.Add(navigationLabel);
                        break;
                    case "content":
                        var content = ReadNavigationContent(navigationPageTargetChildNode);
                        result.Content = content;
                        break;
                }
            if (!result.NavigationLabels.Any())
                throw new Exception(
                    "Incorrect EPUB navigation page target: at least one navLabel element is required.");
            return result;
        }

        private static EpubNavigationList ReadNavigationList(XElement navigationListNode)
        {
            var result = new EpubNavigationList();
            foreach (var navigationListNodeAttribute in navigationListNode.Attributes())
            {
                var attributeValue = navigationListNodeAttribute.Value;
                switch (navigationListNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "id":
                        result.Id = attributeValue;
                        break;
                    case "class":
                        result.Class = attributeValue;
                        break;
                }
            }

            foreach (var navigationListChildNode in navigationListNode.Elements())
                switch (navigationListChildNode.Name.LocalName.ToLowerInvariant())
                {
                    case "navlabel":
                        var navigationLabel = ReadNavigationLabel(navigationListChildNode);
                        result.NavigationLabels.Add(navigationLabel);
                        break;
                    case "navTarget":
                        var navigationTarget = ReadNavigationTarget(navigationListChildNode);
                        result.NavigationTargets.Add(navigationTarget);
                        break;
                }
            if (!result.NavigationLabels.Any())
                throw new Exception(
                    "Incorrect EPUB navigation page target: at least one navLabel element is required.");
            return result;
        }

        private static EpubNavigationTarget ReadNavigationTarget(XElement navigationTargetNode)
        {
            var result = new EpubNavigationTarget();
            foreach (var navigationPageTargetNodeAttribute in navigationTargetNode.Attributes())
            {
                var attributeValue = navigationPageTargetNodeAttribute.Value;
                switch (navigationPageTargetNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "id":
                        result.Id = attributeValue;
                        break;
                    case "value":
                        result.Value = attributeValue;
                        break;
                    case "class":
                        result.Class = attributeValue;
                        break;
                    case "playOrder":
                        result.PlayOrder = attributeValue;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(result.Id))
                throw new Exception("Incorrect EPUB navigation target: navigation target ID is missing.");
            foreach (var navigationTargetChildNode in navigationTargetNode.Elements())
                switch (navigationTargetChildNode.Name.LocalName.ToLowerInvariant())
                {
                    case "navlabel":
                        var navigationLabel = ReadNavigationLabel(navigationTargetChildNode);
                        result.NavigationLabels.Add(navigationLabel);
                        break;
                    case "content":
                        var content = ReadNavigationContent(navigationTargetChildNode);
                        result.Content = content;
                        break;
                }
            if (!result.NavigationLabels.Any())
                throw new Exception("Incorrect EPUB navigation target: at least one navLabel element is required.");
            return result;
        }
    }
}