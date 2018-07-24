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

namespace VersOne.Epub.Schema
{
    public class EpubManifestItem
    {
        public string Id { get; set; }
        public string Href { get; set; }
        public string MediaType { get; set; }
        public string RequiredNamespace { get; set; }
        public string RequiredModules { get; set; }
        public string Fallback { get; set; }
        public string FallbackStyle { get; set; }

        public override string ToString()
        {
            return string.Format("Id: {0}, Href = {1}, MediaType = {2}", Id, Href, MediaType);
        }
    }
}