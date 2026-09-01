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

namespace
{
    void FormatVirtualItem(IShellItem* shellItem, PIDLIST_ABSOLUTE pidlFull, PCWSTR pszPath, PWCHAR buffer)
    {
        ATL::CComHeapPtr<WCHAR> name;
        if (SUCCEEDED(shellItem->GetDisplayName(SIGDN_NORMALDISPLAY, &name)) && name)
        {
            for (PWSTR p = name; *p; ++p)
                if (*p == L'|') *p = L'_';
        }

        SFGAOF attribs = 0;
        shellItem->GetAttributes(SFGAO_FOLDER, &attribs);
        bool isFolder = (attribs & SFGAO_FOLDER) != 0;

        LONGLONG size = -1LL;
        ULONGLONG ft = 0;

        CComQIPtr<IShellItem2> shellItem2(shellItem);
        CComPtr<IPropertyStore> store;
        if (shellItem2 && SUCCEEDED(shellItem2->GetPropertyStore(GPS_FASTPROPERTIESONLY, IID_PPV_ARGS(&store))))
        {
            if (!isFolder)
            {
                PROPVARIANT propSize = {};
                if (SUCCEEDED(store->GetValue(PKEY_Size, &propSize)))
                {
                    if (propSize.vt == VT_UI8)
                        size = (LONGLONG)propSize.uhVal.QuadPart;
                }
                PropVariantClear(&propSize);
            }

            PROPVARIANT propDate = {};
            if (SUCCEEDED(store->GetValue(PKEY_DateModified, &propDate)))
            {
                if (propDate.vt == VT_FILETIME)
                {
                    ULARGE_INTEGER uli = { propDate.filetime.dwLowDateTime, propDate.filetime.dwHighDateTime };
                    ft = uli.QuadPart;
                }
            }
            PropVariantClear(&propDate);
        }

        SHFILEINFOW sfi = {};
        int iconIndex = (pidlFull && SHGetFileInfoW((PCWSTR)pidlFull, 0, &sfi, sizeof(sfi), SHGFI_PIDL | SHGFI_SYSICONINDEX)) ? sfi.iIcon : -1;

        if (FAILED(StringCchPrintfW(buffer, MAX_PATH_EX, L"::QL_VIRTUAL|%lld|%llu|%d|%s|%s",
            size, ft, iconIndex, name ? (PCWSTR)name : L"", pszPath)))
        {
            buffer[0] = L'\0';
        }
    }
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
    // If the file path is too long, the call might fail but CFSTR_SHELLIDLIST will do it
    // https://github.com/QL-Win/QuickLook/issues/1643
    if (SUCCEEDED(dao->GetData(&formatetc, &medium)))
    {
        HDROP hDrop = HDROP(medium.hGlobal);
        int count = DragQueryFile(hDrop, 0xFFFFFFFF, nullptr, 0);
        if (count >= 1)
        {
            WCHAR localBuffer[MAX_PATH] = { '\0' };
            if (DragQueryFileW(hDrop, 0, localBuffer, MAX_PATH) > 0)
            {
                DWORD length = GetLongPathNameW(localBuffer, buffer, MAX_PATH_EX);
                if (length == 0 || length >= MAX_PATH_EX)
                {
                    if (FAILED(StringCchCopyW(buffer, MAX_PATH_EX, localBuffer)))
                        buffer[0] = L'\0';
                }
                ReleaseStgMedium(&medium);
                return;
            }
        }
        ReleaseStgMedium(&medium);
    }

    // If CF_HDROP fails, try CFSTR_SHELLIDLIST
    // Support Desktop Icons (This PC, Recycle Bin and so on)
    // https://github.com/QL-Win/QuickLook/issues/1610
    static const CLIPFORMAT cfShellIDList = (CLIPFORMAT)RegisterClipboardFormatW(CFSTR_SHELLIDLIST);
    formatetc.cfFormat = cfShellIDList;

    if (SUCCEEDED(dao->GetData(&formatetc, &medium)))
    {
        CIDA* pida = (CIDA*)GlobalLock(medium.hGlobal);
        if (!pida || pida->cidl < 1)
        {
            if (pida)
                GlobalUnlock(medium.hGlobal);
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
            ATL::CComHeapPtr<WCHAR> filePath;
            if (SUCCEEDED(shellItem->GetDisplayName(SIGDN_FILESYSPATH, &filePath)) && filePath)
            {
                if (FAILED(StringCchCopyW(buffer, MAX_PATH_EX, filePath)))
                    buffer[0] = L'\0';
            }
            else
            {
                ATL::CComHeapPtr<WCHAR> parsingPath;
                if (SUCCEEDED(shellItem->GetDisplayName(SIGDN_DESKTOPABSOLUTEPARSING, &parsingPath)) && parsingPath)
                {
                    bool isPureClsid = wcslen(parsingPath) == 40 && parsingPath[0] == L':' && parsingPath[1] == L':' && parsingPath[2] == L'{' && parsingPath[39] == L'}';
                    if (isPureClsid)
                    {
                        if (FAILED(StringCchCopyW(buffer, MAX_PATH_EX, parsingPath)))
                            buffer[0] = L'\0';
                    }
                    else
                        FormatVirtualItem(shellItem, pidlFull, parsingPath, buffer);
                }
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
