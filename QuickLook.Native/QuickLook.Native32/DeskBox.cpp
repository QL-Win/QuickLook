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
#include "DeskBox.h"
#include <UIAutomation.h>
#include <string>

#pragma comment(lib, "UIAutomationCore.lib")

namespace
{
    bool IsExistingDirectory(PCWSTR path);

    bool TryResolveShortcutDirectory(PCWSTR shortcutPath, PWCHAR outDirectory)
    {
        if (!shortcutPath || !outDirectory)
            return false;

        const DWORD attrs = GetFileAttributesW(shortcutPath);
        if (attrs == INVALID_FILE_ATTRIBUTES || (attrs & FILE_ATTRIBUTE_DIRECTORY) != 0)
            return false;

        CComPtr<IShellLinkW> shellLink;
        if (FAILED(shellLink.CoCreateInstance(CLSID_ShellLink)))
            return false;

        CComQIPtr<IPersistFile> persist(shellLink);
        if (!persist)
            return false;

        if (FAILED(persist->Load(shortcutPath, STGM_READ)))
            return false;

        WCHAR targetPath[MAX_PATH_EX] = { L'\0' };
        WIN32_FIND_DATAW data = {};
        if (FAILED(shellLink->GetPath(targetPath, MAX_PATH_EX, &data, SLGP_RAWPATH)))
            return false;

        if (targetPath[0] == L'\0')
            return false;

        WCHAR expandedTarget[MAX_PATH_EX] = { L'\0' };
        DWORD expandedLength = ExpandEnvironmentStringsW(targetPath, expandedTarget, MAX_PATH_EX);
        PCWSTR finalTarget = targetPath;
        if (expandedLength > 0 && expandedLength < MAX_PATH_EX)
            finalTarget = expandedTarget;

        if (!IsExistingDirectory(finalTarget))
            return false;

        wcscpy_s(outDirectory, MAX_PATH_EX, finalTarget);
        return true;
    }

    std::wstring Trim(const std::wstring& value)
    {
        const auto start = value.find_first_not_of(L" \t\r\n");
        if (start == std::wstring::npos)
            return L"";

        const auto end = value.find_last_not_of(L" \t\r\n");
        return value.substr(start, end - start + 1);
    }

    bool IsIgnoredTitleText(const std::wstring& text)
    {
        if (text.empty())
            return true;

        static const PCWSTR ignored[] = {
            L"+",
            L"...",
            L"Sort",
            L"View",
            L"Name",
            L"Date modified",
            L"Type",
            L"Size"
        };

        for (auto s : ignored)
        {
            if (_wcsicmp(text.c_str(), s) == 0)
                return true;
        }

        return false;
    }

    bool CreateAutomation(IUIAutomation** automation)
    {
        HRESULT hr = CoCreateInstance(
            __uuidof(CUIAutomation8),
            nullptr,
            CLSCTX_INPROC_SERVER,
            __uuidof(IUIAutomation),
            reinterpret_cast<void**>(automation));

        if (SUCCEEDED(hr))
            return true;

        hr = CoCreateInstance(
            __uuidof(CUIAutomation),
            nullptr,
            CLSCTX_INPROC_SERVER,
            __uuidof(IUIAutomation),
            reinterpret_cast<void**>(automation));

        return SUCCEEDED(hr);
    }

    bool IsDeskBoxProcess(HWND hwnd)
    {
        DWORD processId = 0;
        GetWindowThreadProcessId(hwnd, &processId);
        if (processId == 0)
            return false;

        HANDLE process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, processId);
        if (process == nullptr)
            process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, processId);
        if (process == nullptr)
            return false;

        WCHAR processPath[MAX_PATH_EX] = { L'\0' };
        DWORD processPathLength = MAX_PATH_EX;
        bool result = false;

        if (QueryFullProcessImageNameW(process, 0, processPath, &processPathLength))
        {
            WCHAR* processName = wcsrchr(processPath, L'\\');
            processName = processName == nullptr ? processPath : processName + 1;
            result = _wcsicmp(processName, L"DeskBox.exe") == 0;
        }

        CloseHandle(process);
        return result;
    }

    bool IsPseudoTypeName(PCWSTR value)
    {
        if (value == nullptr || value[0] == L'\0')
            return true;

        return wcsstr(value, L"DeskBox.Models.") != nullptr;
    }

    bool TryCopyMeaningfulString(PWCHAR buffer, PCWSTR value)
    {
        if (!buffer || !value || value[0] == L'\0')
            return false;

        if (IsPseudoTypeName(value))
            return false;

        wcscpy_s(buffer, MAX_PATH_EX, value);
        return true;
    }

    bool TryCopyElementName(IUIAutomationElement* element, PWCHAR buffer)
    {
        if (!element || !buffer)
            return false;

        BSTR name = nullptr;
        if (FAILED(element->get_CurrentName(&name)) || name == nullptr)
            return false;

        bool copied = TryCopyMeaningfulString(buffer, name);
        SysFreeString(name);
        return copied;
    }

    bool TryCopyElementValue(IUIAutomationElement* element, PWCHAR buffer)
    {
        if (!element || !buffer)
            return false;

        IUIAutomationValuePattern* valuePattern = nullptr;
        if (FAILED(element->GetCurrentPatternAs(
                UIA_ValuePatternId,
                __uuidof(IUIAutomationValuePattern),
                reinterpret_cast<void**>(&valuePattern))) ||
            valuePattern == nullptr)
            return false;

        BSTR value = nullptr;
        bool copied = false;
        if (SUCCEEDED(valuePattern->get_CurrentValue(&value)) && value != nullptr)
        {
            copied = TryCopyMeaningfulString(buffer, value);
            SysFreeString(value);
        }

        valuePattern->Release();
        return copied;
    }

    bool TryCopyNameFromDescendants(IUIAutomation* automation, IUIAutomationElement* selectedItem, PWCHAR buffer)
    {
        if (!automation || !selectedItem || !buffer)
            return false;

        IUIAutomationCondition* trueCondition = nullptr;
        automation->CreateTrueCondition(&trueCondition);
        if (trueCondition == nullptr)
            return false;

        IUIAutomationElementArray* descendants = nullptr;
        selectedItem->FindAll(TreeScope_Descendants, trueCondition, &descendants);
        trueCondition->Release();

        if (descendants == nullptr)
            return false;

        int count = 0;
        descendants->get_Length(&count);
        bool copied = false;

        for (int i = 0; i < count && !copied; ++i)
        {
            IUIAutomationElement* child = nullptr;
            if (SUCCEEDED(descendants->GetElement(i, &child)) && child != nullptr)
            {
                copied = TryCopyElementName(child, buffer);
                if (!copied)
                    copied = TryCopyElementValue(child, buffer);

                child->Release();
            }
        }

        descendants->Release();
        return copied;
    }

    bool GetSelectedItemName(HWND hwnd, PWCHAR buffer)
    {
        if (!buffer)
            return false;

        IUIAutomation* automation = nullptr;
        if (!CreateAutomation(&automation))
            return false;

        IUIAutomationElement* root = nullptr;
        if (FAILED(automation->ElementFromHandle(hwnd, &root)) || !root)
        {
            automation->Release();
            return false;
        }

        IUIAutomationCondition* trueCondition = nullptr;
        automation->CreateTrueCondition(&trueCondition);
        if (trueCondition == nullptr)
        {
            root->Release();
            automation->Release();
            return false;
        }

        IUIAutomationElementArray* elements = nullptr;
        root->FindAll(TreeScope_Descendants, trueCondition, &elements);
        trueCondition->Release();
        root->Release();

        if (!elements)
        {
            automation->Release();
            return false;
        }

        int count = 0;
        elements->get_Length(&count);
        bool found = false;

        for (int i = 0; i < count && !found; ++i)
        {
            IUIAutomationElement* item = nullptr;
            if (SUCCEEDED(elements->GetElement(i, &item)) && item != nullptr)
            {
                IUIAutomationSelectionItemPattern* selection = nullptr;
                if (SUCCEEDED(item->GetCurrentPatternAs(
                        UIA_SelectionItemPatternId,
                        __uuidof(IUIAutomationSelectionItemPattern),
                        reinterpret_cast<void**>(&selection))) &&
                    selection != nullptr)
                {
                    BOOL isSelected = FALSE;
                    if (SUCCEEDED(selection->get_CurrentIsSelected(&isSelected)) && isSelected)
                    {
                        found = TryCopyElementName(item, buffer);
                        if (!found)
                            found = TryCopyNameFromDescendants(automation, item, buffer);
                        if (!found)
                            found = TryCopyElementValue(item, buffer);
                    }
                    selection->Release();
                }

                item->Release();
            }
        }

        elements->Release();
        automation->Release();
        return found;
    }

    bool GetDeskBoxRootFolder(PWCHAR buffer)
    {
        if (!buffer)
            return false;

        WCHAR userProfile[MAX_PATH_EX] = { L'\0' };
        DWORD length = GetEnvironmentVariableW(L"USERPROFILE", userProfile, MAX_PATH_EX);
        if (length == 0 || length >= MAX_PATH_EX)
            return false;

        std::wstring root = userProfile;
        if (!root.empty() && root.back() != L'\\')
            root += L'\\';
        root += L"DeskBox";

        if (!IsExistingDirectory(root.c_str()))
            return false;

        wcscpy_s(buffer, MAX_PATH_EX, root.c_str());
        return true;
    }

    bool GetTopTitleDirectoryName(HWND hwnd, PWCHAR buffer)
    {
        if (!buffer)
            return false;

        IUIAutomation* automation = nullptr;
        if (!CreateAutomation(&automation))
            return false;

        IUIAutomationElement* root = nullptr;
        if (FAILED(automation->ElementFromHandle(hwnd, &root)) || !root)
        {
            automation->Release();
            return false;
        }

        IUIAutomationCondition* trueCondition = nullptr;
        automation->CreateTrueCondition(&trueCondition);
        if (trueCondition == nullptr)
        {
            root->Release();
            automation->Release();
            return false;
        }

        IUIAutomationElementArray* elements = nullptr;
        root->FindAll(TreeScope_Descendants, trueCondition, &elements);
        trueCondition->Release();
        root->Release();

        if (!elements)
        {
            automation->Release();
            return false;
        }

        RECT windowRect = {};
        if (!GetWindowRect(hwnd, &windowRect))
        {
            elements->Release();
            automation->Release();
            return false;
        }

        const auto windowHeight = static_cast<double>(windowRect.bottom - windowRect.top);
        const auto maxTop = static_cast<double>(windowRect.top) + windowHeight * 0.35;

        bool found = false;
        double bestTop = 1e30;
        double bestLeft = 1e30;
        std::wstring bestText;

        auto consider = [&](PCWSTR raw, double top, double left)
        {
            if (!raw)
                return;

            auto text = Trim(raw);
            if (text.empty())
                return;
            if (IsPseudoTypeName(text.c_str()))
                return;
            if (IsIgnoredTitleText(text))
                return;
            if (text.find(L'\\') != std::wstring::npos || text.find(L'/') != std::wstring::npos)
                return;
            if (text.find(L'>') != std::wstring::npos)
                return;
            if (top > maxTop)
                return;

            if (!found || top < bestTop ||
                (top == bestTop && left < bestLeft) ||
                (top == bestTop && left == bestLeft && text.size() > bestText.size()))
            {
                found = true;
                bestTop = top;
                bestLeft = left;
                bestText = text;
            }
        };

        int count = 0;
        elements->get_Length(&count);
        for (int i = 0; i < count; ++i)
        {
            IUIAutomationElement* item = nullptr;
            if (SUCCEEDED(elements->GetElement(i, &item)) && item != nullptr)
            {
                RECT rect = {};
                if (SUCCEEDED(item->get_CurrentBoundingRectangle(&rect)))
                {
                    const auto top = static_cast<double>(rect.top);
                    const auto left = static_cast<double>(rect.left);

                    BSTR name = nullptr;
                    if (SUCCEEDED(item->get_CurrentName(&name)) && name != nullptr)
                    {
                        consider(name, top, left);
                        SysFreeString(name);
                    }

                    IUIAutomationValuePattern* valuePattern = nullptr;
                    if (SUCCEEDED(item->GetCurrentPatternAs(
                            UIA_ValuePatternId,
                            __uuidof(IUIAutomationValuePattern),
                            reinterpret_cast<void**>(&valuePattern))) &&
                        valuePattern != nullptr)
                    {
                        BSTR value = nullptr;
                        if (SUCCEEDED(valuePattern->get_CurrentValue(&value)) && value != nullptr)
                        {
                            consider(value, top, left);
                            SysFreeString(value);
                        }
                        valuePattern->Release();
                    }
                }

                item->Release();
            }
        }

        elements->Release();
        automation->Release();

        if (!found)
            return false;

        wcscpy_s(buffer, MAX_PATH_EX, bestText.c_str());
        return true;
    }

    bool BuildDeskBoxSearchDirectory(HWND hwnd, PWCHAR buffer)
    {
        if (!buffer)
            return false;

        WCHAR root[MAX_PATH_EX] = { L'\0' };
        if (!GetDeskBoxRootFolder(root))
            return false;

        WCHAR titleName[MAX_PATH_EX] = { L'\0' };
        if (!GetTopTitleDirectoryName(hwnd, titleName))
        {
            wcscpy_s(buffer, MAX_PATH_EX, root);
            return true;
        }

        std::wstring path = root;
        if (!path.empty() && path.back() != L'\\')
            path += L'\\';
        path += titleName;

        if (IsExistingDirectory(path.c_str()))
        {
            wcscpy_s(buffer, MAX_PATH_EX, path.c_str());
        }
        else
        {
            std::wstring shortcutPath = path + L".lnk";
            WCHAR resolvedShortcutDirectory[MAX_PATH_EX] = { L'\0' };

            if (TryResolveShortcutDirectory(shortcutPath.c_str(), resolvedShortcutDirectory))
                wcscpy_s(buffer, MAX_PATH_EX, resolvedShortcutDirectory);
            else
                wcscpy_s(buffer, MAX_PATH_EX, root);
        }

        return true;
    }

    bool IsExistingDirectory(PCWSTR path)
    {
        if (path == nullptr || path[0] == L'\0')
            return false;

        const DWORD attrs = GetFileAttributesW(path);
        return attrs != INVALID_FILE_ATTRIBUTES && (attrs & FILE_ATTRIBUTE_DIRECTORY) != 0;
    }



    std::wstring FileStemFromName(PCWSTR fileName)
    {
        if (!fileName)
            return L"";

        std::wstring name(fileName);
        const auto dotPos = name.find_last_of(L'.');
        if (dotPos == std::wstring::npos || dotPos == 0)
            return name;

        return name.substr(0, dotPos);
    }

    bool FindFileIgnoringExtension(PCWSTR directory, PCWSTR itemName, PWCHAR outPath)
    {
        if (!directory || !itemName || !outPath)
            return false;

        std::wstring pattern = directory;
        if (!pattern.empty() && pattern.back() != L'\\')
            pattern += L'\\';
        pattern += L"*";

        WIN32_FIND_DATAW data = {};
        HANDLE findHandle = FindFirstFileW(pattern.c_str(), &data);
        if (findHandle == INVALID_HANDLE_VALUE)
            return false;

        const std::wstring targetStem = FileStemFromName(itemName);
        bool found = false;

        do
        {
            const auto candidateStem = FileStemFromName(data.cFileName);
            if (_wcsicmp(candidateStem.c_str(), targetStem.c_str()) != 0)
                continue;

            std::wstring fullPath = directory;
            if (!fullPath.empty() && fullPath.back() != L'\\')
                fullPath += L'\\';
            fullPath += data.cFileName;

            wcscpy_s(outPath, MAX_PATH_EX, fullPath.c_str());
            found = true;
            break;
        }
        while (FindNextFileW(findHandle, &data));

        FindClose(findHandle);
        return found;
    }
}

bool DeskBox::MatchWindow(HWND hwnd)
{
    WCHAR classBuffer[MAX_PATH] = { L'\0' };
    if (FAILED(GetClassName(hwnd, classBuffer, MAX_PATH)))
        return false;

    if (wcscmp(classBuffer, L"WinUIDesktopWin32WindowClass") != 0)
        return false;

    return IsDeskBoxProcess(hwnd);
}

void DeskBox::GetSelected(PWCHAR buffer)
{
    if (!buffer)
        return;

    HRESULT hr = CoInitialize(nullptr);
    if (FAILED(hr))
        return;

    WCHAR itemName[MAX_PATH_EX] = { L'\0' };
    if (!GetSelectedItemName(GetForegroundWindow(), itemName))
    {
        CoUninitialize();
        return;
    }

    if (itemName[0] == L'\0')
    {
        CoUninitialize();
        return;
    }

    WCHAR folderPath[MAX_PATH_EX] = { L'\0' };
    if (!BuildDeskBoxSearchDirectory(GetForegroundWindow(), folderPath))
    {
        CoUninitialize();
        return;
    }

    FindFileIgnoringExtension(folderPath, itemName, buffer);
    CoUninitialize();
}
