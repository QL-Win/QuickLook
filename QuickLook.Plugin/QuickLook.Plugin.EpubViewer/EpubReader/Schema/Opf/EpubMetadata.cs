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

using System.Collections.Generic;

namespace VersOne.Epub.Schema
{
    public class EpubMetadata
    {
        public List<string> Titles { get; set; }
        public List<EpubMetadataCreator> Creators { get; set; }
        public List<string> Subjects { get; set; }
        public string Description { get; set; }
        public List<string> Publishers { get; set; }
        public List<EpubMetadataContributor> Contributors { get; set; }
        public List<EpubMetadataDate> Dates { get; set; }
        public List<string> Types { get; set; }
        public List<string> Formats { get; set; }
        public List<EpubMetadataIdentifier> Identifiers { get; set; }
        public List<string> Sources { get; set; }
        public List<string> Languages { get; set; }
        public List<string> Relations { get; set; }
        public List<string> Coverages { get; set; }
        public List<string> Rights { get; set; }
        public List<EpubMetadataMeta> MetaItems { get; set; }
    }
}