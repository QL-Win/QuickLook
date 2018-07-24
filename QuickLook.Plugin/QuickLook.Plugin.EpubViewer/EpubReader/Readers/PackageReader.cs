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
using System.Threading.Tasks;
using System.Xml.Linq;
using VersOne.Epub.Schema;

namespace VersOne.Epub.Internal
{
    internal static class PackageReader
    {
        public static async Task<EpubPackage> ReadPackageAsync(ZipArchive epubArchive, string rootFilePath)
        {
            var rootFileEntry = epubArchive.GetEntry(rootFilePath);
            if (rootFileEntry == null) throw new Exception("EPUB parsing error: root file not found in archive.");
            XDocument containerDocument;
            using (var containerStream = rootFileEntry.Open())
            {
                containerDocument = await XmlUtils.LoadDocumentAsync(containerStream).ConfigureAwait(false);
            }

            XNamespace opfNamespace = "http://www.idpf.org/2007/opf";
            var packageNode = containerDocument.Element(opfNamespace + "package");
            var result = new EpubPackage();
            var epubVersionValue = packageNode.Attribute("version").Value;
            if (epubVersionValue == "2.0")
                result.EpubVersion = EpubVersion.EPUB_2;
            else if (epubVersionValue == "3.0")
                result.EpubVersion = EpubVersion.EPUB_3;
            else
                throw new Exception(string.Format("Unsupported EPUB version: {0}.", epubVersionValue));
            var metadataNode = packageNode.Element(opfNamespace + "metadata");
            if (metadataNode == null) throw new Exception("EPUB parsing error: metadata not found in the package.");
            var metadata = ReadMetadata(metadataNode, result.EpubVersion);
            result.Metadata = metadata;
            var manifestNode = packageNode.Element(opfNamespace + "manifest");
            if (manifestNode == null) throw new Exception("EPUB parsing error: manifest not found in the package.");
            var manifest = ReadManifest(manifestNode);
            result.Manifest = manifest;
            var spineNode = packageNode.Element(opfNamespace + "spine");
            if (spineNode == null) throw new Exception("EPUB parsing error: spine not found in the package.");
            var spine = ReadSpine(spineNode);
            result.Spine = spine;
            var guideNode = packageNode.Element(opfNamespace + "guide");
            if (guideNode != null)
            {
                var guide = ReadGuide(guideNode);
                result.Guide = guide;
            }

            return result;
        }

        private static EpubMetadata ReadMetadata(XElement metadataNode, EpubVersion epubVersion)
        {
            var result = new EpubMetadata
            {
                Titles = new List<string>(),
                Creators = new List<EpubMetadataCreator>(),
                Subjects = new List<string>(),
                Publishers = new List<string>(),
                Contributors = new List<EpubMetadataContributor>(),
                Dates = new List<EpubMetadataDate>(),
                Types = new List<string>(),
                Formats = new List<string>(),
                Identifiers = new List<EpubMetadataIdentifier>(),
                Sources = new List<string>(),
                Languages = new List<string>(),
                Relations = new List<string>(),
                Coverages = new List<string>(),
                Rights = new List<string>(),
                MetaItems = new List<EpubMetadataMeta>()
            };
            foreach (var metadataItemNode in metadataNode.Elements())
            {
                var innerText = metadataItemNode.Value;
                switch (metadataItemNode.Name.LocalName.ToLowerInvariant())
                {
                    case "title":
                        result.Titles.Add(innerText);
                        break;
                    case "creator":
                        var creator = ReadMetadataCreator(metadataItemNode);
                        result.Creators.Add(creator);
                        break;
                    case "subject":
                        result.Subjects.Add(innerText);
                        break;
                    case "description":
                        result.Description = innerText;
                        break;
                    case "publisher":
                        result.Publishers.Add(innerText);
                        break;
                    case "contributor":
                        var contributor = ReadMetadataContributor(metadataItemNode);
                        result.Contributors.Add(contributor);
                        break;
                    case "date":
                        var date = ReadMetadataDate(metadataItemNode);
                        result.Dates.Add(date);
                        break;
                    case "type":
                        result.Types.Add(innerText);
                        break;
                    case "format":
                        result.Formats.Add(innerText);
                        break;
                    case "identifier":
                        var identifier = ReadMetadataIdentifier(metadataItemNode);
                        result.Identifiers.Add(identifier);
                        break;
                    case "source":
                        result.Sources.Add(innerText);
                        break;
                    case "language":
                        result.Languages.Add(innerText);
                        break;
                    case "relation":
                        result.Relations.Add(innerText);
                        break;
                    case "coverage":
                        result.Coverages.Add(innerText);
                        break;
                    case "rights":
                        result.Rights.Add(innerText);
                        break;
                    case "meta":
                        if (epubVersion == EpubVersion.EPUB_2)
                        {
                            var meta = ReadMetadataMetaVersion2(metadataItemNode);
                            result.MetaItems.Add(meta);
                        }
                        else if (epubVersion == EpubVersion.EPUB_3)
                        {
                            var meta = ReadMetadataMetaVersion3(metadataItemNode);
                            result.MetaItems.Add(meta);
                        }

                        break;
                }
            }

            return result;
        }

        private static EpubMetadataCreator ReadMetadataCreator(XElement metadataCreatorNode)
        {
            var result = new EpubMetadataCreator();
            foreach (var metadataCreatorNodeAttribute in metadataCreatorNode.Attributes())
            {
                var attributeValue = metadataCreatorNodeAttribute.Value;
                switch (metadataCreatorNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "role":
                        result.Role = attributeValue;
                        break;
                    case "file-as":
                        result.FileAs = attributeValue;
                        break;
                }
            }

            result.Creator = metadataCreatorNode.Value;
            return result;
        }

        private static EpubMetadataContributor ReadMetadataContributor(XElement metadataContributorNode)
        {
            var result = new EpubMetadataContributor();
            foreach (var metadataContributorNodeAttribute in metadataContributorNode.Attributes())
            {
                var attributeValue = metadataContributorNodeAttribute.Value;
                switch (metadataContributorNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "role":
                        result.Role = attributeValue;
                        break;
                    case "file-as":
                        result.FileAs = attributeValue;
                        break;
                }
            }

            result.Contributor = metadataContributorNode.Value;
            return result;
        }

        private static EpubMetadataDate ReadMetadataDate(XElement metadataDateNode)
        {
            var result = new EpubMetadataDate();
            var eventAttribute = metadataDateNode.Attribute(metadataDateNode.Name.Namespace + "event");
            if (eventAttribute != null) result.Event = eventAttribute.Value;
            result.Date = metadataDateNode.Value;
            return result;
        }

        private static EpubMetadataIdentifier ReadMetadataIdentifier(XElement metadataIdentifierNode)
        {
            var result = new EpubMetadataIdentifier();
            foreach (var metadataIdentifierNodeAttribute in metadataIdentifierNode.Attributes())
            {
                var attributeValue = metadataIdentifierNodeAttribute.Value;
                switch (metadataIdentifierNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "id":
                        result.Id = attributeValue;
                        break;
                    case "opf:scheme":
                        result.Scheme = attributeValue;
                        break;
                }
            }

            result.Identifier = metadataIdentifierNode.Value;
            return result;
        }

        private static EpubMetadataMeta ReadMetadataMetaVersion2(XElement metadataMetaNode)
        {
            var result = new EpubMetadataMeta();
            foreach (var metadataMetaNodeAttribute in metadataMetaNode.Attributes())
            {
                var attributeValue = metadataMetaNodeAttribute.Value;
                switch (metadataMetaNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "name":
                        result.Name = attributeValue;
                        break;
                    case "content":
                        result.Content = attributeValue;
                        break;
                }
            }

            return result;
        }

        private static EpubMetadataMeta ReadMetadataMetaVersion3(XElement metadataMetaNode)
        {
            var result = new EpubMetadataMeta();
            foreach (var metadataMetaNodeAttribute in metadataMetaNode.Attributes())
            {
                var attributeValue = metadataMetaNodeAttribute.Value;
                switch (metadataMetaNodeAttribute.Name.LocalName.ToLowerInvariant())
                {
                    case "id":
                        result.Id = attributeValue;
                        break;
                    case "refines":
                        result.Refines = attributeValue;
                        break;
                    case "property":
                        result.Property = attributeValue;
                        break;
                    case "scheme":
                        result.Scheme = attributeValue;
                        break;
                }
            }

            result.Content = metadataMetaNode.Value;
            return result;
        }

        private static EpubManifest ReadManifest(XElement manifestNode)
        {
            var result = new EpubManifest();
            foreach (var manifestItemNode in manifestNode.Elements())
                if (string.Compare(manifestItemNode.Name.LocalName, "item", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var manifestItem = new EpubManifestItem();
                    foreach (var manifestItemNodeAttribute in manifestItemNode.Attributes())
                    {
                        var attributeValue = manifestItemNodeAttribute.Value;
                        switch (manifestItemNodeAttribute.Name.LocalName.ToLowerInvariant())
                        {
                            case "id":
                                manifestItem.Id = attributeValue;
                                break;
                            case "href":
                                manifestItem.Href = Uri.UnescapeDataString(attributeValue);
                                break;
                            case "media-type":
                                manifestItem.MediaType = attributeValue;
                                break;
                            case "required-namespace":
                                manifestItem.RequiredNamespace = attributeValue;
                                break;
                            case "required-modules":
                                manifestItem.RequiredModules = attributeValue;
                                break;
                            case "fallback":
                                manifestItem.Fallback = attributeValue;
                                break;
                            case "fallback-style":
                                manifestItem.FallbackStyle = attributeValue;
                                break;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(manifestItem.Id))
                        throw new Exception("Incorrect EPUB manifest: item ID is missing");
                    if (string.IsNullOrWhiteSpace(manifestItem.Href))
                        throw new Exception("Incorrect EPUB manifest: item href is missing");
                    if (string.IsNullOrWhiteSpace(manifestItem.MediaType))
                        throw new Exception("Incorrect EPUB manifest: item media type is missing");
                    result.Add(manifestItem);
                }

            return result;
        }

        private static EpubSpine ReadSpine(XElement spineNode)
        {
            var result = new EpubSpine();
            var tocAttribute = spineNode.Attribute("toc");
            if (tocAttribute == null || string.IsNullOrWhiteSpace(tocAttribute.Value))
                throw new Exception("Incorrect EPUB spine: TOC is missing");
            result.Toc = tocAttribute.Value;
            foreach (var spineItemNode in spineNode.Elements())
                if (string.Compare(spineItemNode.Name.LocalName, "itemref", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var spineItemRef = new EpubSpineItemRef();
                    var idRefAttribute = spineItemNode.Attribute("idref");
                    if (idRefAttribute == null || string.IsNullOrWhiteSpace(idRefAttribute.Value))
                        throw new Exception("Incorrect EPUB spine: item ID ref is missing");
                    spineItemRef.IdRef = idRefAttribute.Value;
                    var linearAttribute = spineItemNode.Attribute("linear");
                    spineItemRef.IsLinear = linearAttribute == null ||
                                            string.Compare(linearAttribute.Value, "no",
                                                StringComparison.OrdinalIgnoreCase) != 0;
                    result.Add(spineItemRef);
                }

            return result;
        }

        private static EpubGuide ReadGuide(XElement guideNode)
        {
            var result = new EpubGuide();
            foreach (var guideReferenceNode in guideNode.Elements())
                if (string.Compare(guideReferenceNode.Name.LocalName, "reference",
                        StringComparison.OrdinalIgnoreCase) == 0)
                {
                    var guideReference = new EpubGuideReference();
                    foreach (var guideReferenceNodeAttribute in guideReferenceNode.Attributes())
                    {
                        var attributeValue = guideReferenceNodeAttribute.Value;
                        switch (guideReferenceNodeAttribute.Name.LocalName.ToLowerInvariant())
                        {
                            case "type":
                                guideReference.Type = attributeValue;
                                break;
                            case "title":
                                guideReference.Title = attributeValue;
                                break;
                            case "href":
                                guideReference.Href = Uri.UnescapeDataString(attributeValue);
                                break;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(guideReference.Type))
                        throw new Exception("Incorrect EPUB guide: item type is missing");
                    if (string.IsNullOrWhiteSpace(guideReference.Href))
                        throw new Exception("Incorrect EPUB guide: item href is missing");
                    result.Add(guideReference);
                }

            return result;
        }
    }
}