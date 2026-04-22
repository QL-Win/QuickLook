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
#include "IDMan.h"
#include <UIAutomation.h>
#include <regex>
#include <string>

#pragma comment(lib, "UIAutomationCore.lib")

void IDMan::GetSelected(PWCHAR buffer)
{
    // Step 1: Get the selected item name from the IDM list via UIAutomation
    WCHAR selectedName[MAX_PATH] = { L'\0' };
    if (!GetSelectedItemName(selectedName))
        return;

    // Step 2: Resolve the file path from the IDM registry
    GetFilePath(selectedName, buffer);
}

bool IDMan::GetSelectedItemName(PWCHAR nameBuffer)
{
    HWND hwnd = GetForegroundWindow();
    if (hwnd == nullptr)
        return false;

    IUIAutomation* pAutomation = nullptr;
    HRESULT hr = CoCreateInstance(
        __uuidof(CUIAutomation8),
        nullptr,
        CLSCTX_INPROC_SERVER,
        __uuidof(IUIAutomation),
        reinterpret_cast<void**>(&pAutomation));

    if (FAILED(hr))
    {
        // Fallback to older interface
        hr = CoCreateInstance(
            __uuidof(CUIAutomation),
            nullptr,
            CLSCTX_INPROC_SERVER,
            __uuidof(IUIAutomation),
            reinterpret_cast<void**>(&pAutomation));
        if (FAILED(hr))
            return false;
    }

    // Get UIAutomation element from the IDM window handle
    IUIAutomationElement* pIDMWindow = nullptr;
    hr = pAutomation->ElementFromHandle(hwnd, &pIDMWindow);
    if (FAILED(hr) || pIDMWindow == nullptr)
    {
        pAutomation->Release();
        return false;
    }

    // Build OR condition: DataGrid || List
    IUIAutomationCondition* pDataGridCond = nullptr;
    VARIANT varDataGrid;
    VariantInit(&varDataGrid);
    varDataGrid.vt = VT_I4;
    varDataGrid.intVal = UIA_DataGridControlTypeId;
    pAutomation->CreatePropertyCondition(UIA_ControlTypePropertyId, varDataGrid, &pDataGridCond);

    IUIAutomationCondition* pListCond = nullptr;
    VARIANT varList;
    VariantInit(&varList);
    varList.vt = VT_I4;
    varList.intVal = UIA_ListControlTypeId;
    pAutomation->CreatePropertyCondition(UIA_ControlTypePropertyId, varList, &pListCond);

    IUIAutomationCondition* pOrCond = nullptr;
    pAutomation->CreateOrCondition(pDataGridCond, pListCond, &pOrCond);
    pDataGridCond->Release();
    pListCond->Release();

    // Find the list view (DataGrid or List) inside the IDM window
    IUIAutomationElement* pListView = nullptr;
    pIDMWindow->FindFirst(TreeScope_Descendants, pOrCond, &pListView);
    pOrCond->Release();
    pIDMWindow->Release();

    if (pListView == nullptr)
    {
        pAutomation->Release();
        return false;
    }

    // Enumerate all children and find the selected item
    IUIAutomationCondition* pTrueCond = nullptr;
    pAutomation->CreateTrueCondition(&pTrueCond);

    IUIAutomationElementArray* pChildren = nullptr;
    pListView->FindAll(TreeScope_Children, pTrueCond, &pChildren);
    pTrueCond->Release();
    pListView->Release();

    if (pChildren == nullptr)
    {
        pAutomation->Release();
        return false;
    }

    bool found = false;
    int count = 0;
    pChildren->get_Length(&count);

    for (int i = 0; i < count && !found; i++)
    {
        IUIAutomationElement* pItem = nullptr;
        if (SUCCEEDED(pChildren->GetElement(i, &pItem)) && pItem != nullptr)
        {
            IUIAutomationSelectionItemPattern* pSelPattern = nullptr;
            if (SUCCEEDED(pItem->GetCurrentPatternAs(
                    UIA_SelectionItemPatternId,
                    __uuidof(IUIAutomationSelectionItemPattern),
                    reinterpret_cast<void**>(&pSelPattern))) &&
                pSelPattern != nullptr)
            {
                BOOL isSelected = FALSE;
                pSelPattern->get_CurrentIsSelected(&isSelected);
                pSelPattern->Release();

                if (isSelected)
                {
                    BSTR name = nullptr;
                    if (SUCCEEDED(pItem->get_CurrentName(&name)) && name != nullptr)
                    {
                        wcscpy_s(nameBuffer, MAX_PATH, name);
                        SysFreeString(name);
                        found = true;
                    }
                }
            }
            pItem->Release();
        }
    }

    pChildren->Release();
    pAutomation->Release();
    return found;
}

static void PathCombine(PWCHAR buffer, size_t bufferSize, std::initializer_list<std::wstring> parts)
{
    if (!buffer || bufferSize == 0)
        return;

    buffer[0] = L'\0';

    for (const auto& part : parts)
    {
        if (part.empty()) continue;

        // Override when the path is absolute
        if ((part.size() >= 2 && part[1] == L':') ||
            (part[0] == L'\\' || part[0] == L'/'))
        {
            wcsncpy_s(buffer, bufferSize, part.c_str(), _TRUNCATE);
            continue;
        }

        size_t len = wcslen(buffer);

        // Append '\'
        if (len > 0 && buffer[len - 1] != L'\\' && buffer[len - 1] != L'/')
        {
            wcscat_s(buffer, bufferSize, L"\\");
        }

        wcscat_s(buffer, bufferSize, part.c_str());
    }
}

static std::wstring RegexEscape(const std::wstring& s)
{
    static const std::wstring kMeta = L"\\.^$|()[]{}\"+?";
    std::wstring out;
    out.reserve(s.size() * 2);
    for (wchar_t ch : s)
    {
        if (kMeta.find(ch) != std::wstring::npos)
            out.push_back(L'\\');
        out.push_back(ch);
    }
    return out;
}

static bool IsMatch(const std::wstring& input, const std::wstring& pattern)
{
    std::wstring regexPattern = L"^" + RegexEscape(pattern) + L"$";
    size_t pos = 0;
    while ((pos = regexPattern.find(L"\\*", pos)) != std::wstring::npos)
    {
        regexPattern.replace(pos, 2, L".*");
        pos += 2;
    }
    try
    {
        std::wregex re(regexPattern, std::regex_constants::ECMAScript | std::regex_constants::icase);
        return std::regex_match(input, re);
    }
    catch (const std::regex_error&) { return false; }
}

__pragma(warning(suppress:6262))
void IDMan::GetFilePath(PCWSTR name, PWCHAR buffer)
{
    // Extract the file extension (e.g. ".mp4")
    WCHAR ext[64] = { L'\0' };
    const WCHAR* dot = wcsrchr(name, L'.');
    if (dot != nullptr)
        wcscpy_s(ext, dot);

    // Read the default download path from LocalPathW (stored as raw Unicode bytes)
    WCHAR defaultPath[MAX_PATH] = { L'\0' };
    {
        HKEY hBase = nullptr;
        if (RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\DownloadManager", 0, KEY_READ, &hBase) == ERROR_SUCCESS)
        {
            BYTE data[MAX_PATH * sizeof(WCHAR)] = {};
            DWORD dataSize = sizeof(data);
            DWORD dataType = 0;

            if (RegQueryValueExW(hBase, L"LocalPathW", nullptr, &dataType, data, &dataSize) == ERROR_SUCCESS)
            {
                // IDM does not set the type as expected binary or string
                if (dataType == REG_NONE)
                {
                    wcsncpy_s(defaultPath, reinterpret_cast<const wchar_t*>(data), _TRUNCATE);
                }
            }

            RegCloseKey(hBase);
        }
    }

    // Try to match extension against FoldersTree entries
    if (ext[0] != L'\0')
    {
        HKEY hFolders = nullptr;
        if (RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\DownloadManager\\FoldersTree", 0, KEY_READ, &hFolders) == ERROR_SUCCESS)
        {
            WCHAR subKeyName[MAX_PATH] = { L'\0' };
            DWORD subKeyIdx = 0;
            while (RegEnumKeyW(hFolders, subKeyIdx++, subKeyName, MAX_PATH) == ERROR_SUCCESS)
            {
                HKEY hSub = nullptr;
                if (RegOpenKeyExW(hFolders, subKeyName, 0, KEY_READ, &hSub) == ERROR_SUCCESS)
                {
                    WCHAR maskValue[512] = { L'\0' };
                    DWORD maskSize = sizeof(maskValue);
                    if (RegQueryValueExW(hSub, L"mask", nullptr, nullptr, reinterpret_cast<LPBYTE>(maskValue), &maskSize) == ERROR_SUCCESS)
                    {
                        // maskValue is space-delimited list of extensions without dots (e.g. "mp4 mkv avi")
                        WCHAR* ctx = nullptr;
                        WCHAR* tok = wcstok_s(maskValue, L" ", &ctx);
                        while (tok != nullptr)
                        {
                            WCHAR tokExt[64] = L".";
                            wcscat_s(tokExt, tok);
                            if (IsMatch(std::wstring(ext), std::wstring(tokExt)))
                            {
                                // Compose: defaultPath\subKeyName\filename
                                PathCombine(buffer, MAX_PATH_EX, { defaultPath, subKeyName, name });
                                RegCloseKey(hSub);
                                RegCloseKey(hFolders);
                                return;
                            }
                            tok = wcstok_s(nullptr, L" ", &ctx);
                        }
                    }
                    RegCloseKey(hSub);
                }
            }
            RegCloseKey(hFolders);
        }

        // Extension not in FoldersTree — use the default download folder
        if (defaultPath[0] != L'\0')
        {
            PathCombine(buffer, MAX_PATH_EX, { defaultPath, name });
            return;
        }
    }

    // Fallback: search individual download entries in the registry for FR_FNCD match
    HKEY hBase = nullptr;
    if (RegOpenKeyExW(HKEY_CURRENT_USER, L"Software\\DownloadManager", 0, KEY_READ, &hBase) == ERROR_SUCCESS)
    {
        WCHAR subKeyName[MAX_PATH] = { L'\0' };
        DWORD subKeyIdx = 0;
        while (RegEnumKeyW(hBase, subKeyIdx++, subKeyName, MAX_PATH) == ERROR_SUCCESS)
        {
            HKEY hSub = nullptr;
            if (RegOpenKeyExW(hBase, subKeyName, 0, KEY_READ, &hSub) == ERROR_SUCCESS)
            {
                WCHAR frFncd[MAX_PATH] = { L'\0' };
                DWORD frSize = sizeof(frFncd);
                if (RegQueryValueExW(hSub, L"FR_FNCD", nullptr, nullptr, reinterpret_cast<LPBYTE>(frFncd), &frSize) == ERROR_SUCCESS)
                {
                    if (_wcsicmp(frFncd, name) == 0)
                    {
                        WCHAR localFileName[MAX_PATH_EX] = { L'\0' };
                        DWORD lfSize = sizeof(localFileName);
                        if (RegQueryValueExW(hSub, L"LocalFileName", nullptr, nullptr, reinterpret_cast<LPBYTE>(localFileName), &lfSize) == ERROR_SUCCESS)
                        {
                            wcscpy_s(buffer, MAX_PATH_EX, localFileName);
                            RegCloseKey(hSub);
                            RegCloseKey(hBase);
                            return;
                        }
                    }
                }
                RegCloseKey(hSub);
            }
        }
        RegCloseKey(hBase);
    }
}
