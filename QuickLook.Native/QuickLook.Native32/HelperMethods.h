// Copyright © 2017 Paddy Xu
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

#pragma once
class HelperMethods
{
public:
	static void GetSelectedInternal(CComQIPtr<IWebBrowserApp> pWebBrowserApp, PWCHAR buffer);
	static void ObtainFirstItem(CComPtr<IDataObject> dao, PWCHAR buffer);
	static bool IsCursorActivated(HWND hwndfg);
	static bool HelperMethods::IsUWP();

private:
	static bool IsListaryToolbarVisible();
	static bool IsSearchBoxFocused();
	static HWND GetFocusedControl();
};
