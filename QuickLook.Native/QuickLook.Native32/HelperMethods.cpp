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

#include "stdafx.h"
#include "strsafe.h"

#include "HelperMethods.h"

void HelperMethods::GetSelectedInternal(CComPtr<IShellBrowser> psb, PWCHAR buffer)
{
    CComPtr<IShellView> psv;
    if (FAILED(psb->QueryActiveShellView(&psv)))
        return;

    CComPtr<IDataObject> dao;
    if (FAILED(psv->GetItemObject(SVGIO_SELECTION, IID_IDataObject, reinterpret_cast<void**>(&dao))))
        return;

    return ObtainFirstItem(dao, buffer);
}

void HelperMethods::ObtainFirstItem(CComPtr<IDataObject> dao, PWCHAR buffer)
{
    if (!dao || !buffer)
        return;

    FORMATETC formatetc = {};
    STGMEDIUM medium = {};

    formatetc.cfFormat = CF_HDROP;
    formatetc.ptd = nullptr;
    formatetc.dwAspect = DVASPECT_CONTENT;
    formatetc.lindex = -1;
    formatetc.tymed = TYMED_HGLOBAL;

    medium.tymed = TYMED_HGLOBAL;

    // Try CF_HDROP first
    if (SUCCEEDED(dao->GetData(&formatetc, &medium)))
    {
        HDROP hDrop = HDROP(medium.hGlobal);
        int count = DragQueryFile(hDrop, 0xFFFFFFFF, nullptr, 0);
        if (count >= 1)
        {
            WCHAR localBuffer[MAX_PATH] = { '\0' };
            if (DragQueryFileW(hDrop, 0, localBuffer, MAX_PATH) > 0)
            {
                GetLongPathName(localBuffer, buffer, MAX_PATH_EX);
                ReleaseStgMedium(&medium);
                return;
            }
            ReleaseStgMedium(&medium);
        }
    }

    // If CF_HDROP fails, try CFSTR_SHELLIDLIST
    // Support Desktop Icons (This PC, Recycle Bin and so on)
    // https://github.com/QL-Win/QuickLook/issues/1610
    static const CLIPFORMAT cfShellIDList = (CLIPFORMAT)RegisterClipboardFormatW(CFSTR_SHELLIDLIST);
    formatetc.cfFormat = cfShellIDList;

    if (SUCCEEDED(dao->GetData(&formatetc, &medium))) 
    {
        CIDA* pida = (CIDA*)GlobalLock(medium.hGlobal);
        if (!pida)
        {
            ReleaseStgMedium(&medium);
            return;
        }

        ITEMIDLIST* pidlFolder = (ITEMIDLIST*)((BYTE*)pida + pida->aoffset[0]);
        ITEMIDLIST* pidlItem = (ITEMIDLIST*)((BYTE*)pida + pida->aoffset[1]);
        PIDLIST_ABSOLUTE pidlFull = ILCombine(pidlFolder, pidlItem);
        GlobalUnlock(medium.hGlobal);
        ReleaseStgMedium(&medium);

        if (!pidlFull)
            return;

        // Convert to IShellItem to get canonical parsing path
        CComPtr<IShellItem> shellItem;
        if (SUCCEEDED(SHCreateItemFromIDList(pidlFull, IID_PPV_ARGS(&shellItem))))
        {
            PWSTR pszPath = nullptr;
            if (SUCCEEDED(shellItem->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &pszPath)))
            {
                StringCchCopyW(buffer, MAX_PATH, pszPath); // returns e.g., ::{645FF040-5081-101B-9F08-00AA002F954E}
                CoTaskMemFree(pszPath);
            }
        }

        ILFree(pidlFull);
    }
}

bool HelperMethods::IsListaryToolbarVisible()
{
    auto CALLBACK findListaryWindowProc = [](__in HWND hwnd, __in LPARAM lParam)-> BOOL
    {
        WCHAR classBuffer[MAX_PATH] = {'\0'};
        if (FAILED(GetClassName(hwnd, classBuffer, MAX_PATH)))
            return TRUE;

        if (wcsncmp(classBuffer, L"Listary_WidgetWin_", 18) == 0)
        {
            if (IsWindowVisible(hwnd))
            {
                *reinterpret_cast<bool*>(lParam) = true;
                return FALSE;
            }
        }
        return TRUE;
    };

    auto found = false;
    EnumWindows(findListaryWindowProc, reinterpret_cast<LPARAM>(&found));

    return found;
}

// Windows 10 1909 replaced the search box in the File Explorer by a UWP control.
// gti.flags is always 0 for UWP applications.
bool HelperMethods::IsExplorerSearchBoxFocused()
{
    auto* hwnd = GetFocusedControl();

    WCHAR classBuffer[MAX_PATH] = { '\0' };
    if (FAILED(GetClassName(hwnd, classBuffer, MAX_PATH)))
        return false;

    return wcscmp(classBuffer, L"Windows.UI.Core.CoreWindow") == 0;
}

bool HelperMethods::IsCursorActivated(HWND hwnd)
{
    auto tId = GetWindowThreadProcessId(hwnd, nullptr);

    GUITHREADINFO gti = { sizeof gti };
    GetGUIThreadInfo(tId, &gti);

    return gti.flags || gti.hwndCaret || IsListaryToolbarVisible();
}

bool HelperMethods::IsUWP()
{
    auto pGCPFN = decltype(&GetCurrentPackageFullName)(
        GetProcAddress(GetModuleHandle(L"kernel32.dll"), "GetCurrentPackageFullName"));

    if (!pGCPFN)
        return false;

    UINT32 pn = 0;
    return pGCPFN(&pn, nullptr) == ERROR_INSUFFICIENT_BUFFER;
}

HWND HelperMethods::GetFocusedControl()
{
    auto tid = GetWindowThreadProcessId(GetForegroundWindow(), nullptr);

       if (0 == AttachThreadInput(GetCurrentThreadId(), tid, TRUE))
        return nullptr;

    auto* hwnd = GetFocus();

     AttachThreadInput(GetCurrentThreadId(), tid, FALSE);

    return hwnd;
}
