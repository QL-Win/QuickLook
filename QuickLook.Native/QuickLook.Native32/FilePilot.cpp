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
#include "FilePilot.h"
#include <UIAutomation.h>

#pragma comment(lib, "UIAutomationCore.lib")

namespace
{
    constexpr auto CLIPBOARD_TIMEOUT_MS = 250;
    constexpr auto CLIPBOARD_POLL_INTERVAL_MS = 5;
    constexpr auto MAX_CLASS_NAME_LENGTH = 256;

    bool StartsWith(PCWSTR value, PCWSTR prefix)
    {
        return wcsncmp(value, prefix, wcslen(prefix)) == 0;
    }

    bool IsFilePilotProcess(HWND hwnd)
    {
        DWORD processId = 0;
        GetWindowThreadProcessId(hwnd, &processId);

        if (processId == 0)
            return false;

        auto process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, processId);
        if (process == nullptr)
            process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, processId);
        if (process == nullptr)
            return false;

        WCHAR processPath[MAX_PATH_EX] = { L'\0' };
        DWORD processPathLength = MAX_PATH_EX;
        auto success = QueryFullProcessImageNameW(process, 0, processPath, &processPathLength);
        CloseHandle(process);

        if (!success)
            return false;

        auto processName = wcsrchr(processPath, L'\\');
        processName = processName == nullptr ? processPath : processName + 1;

        return _wcsicmp(processName, L"FPilot.exe") == 0 ||
               _wcsicmp(processName, L"FilePilot.exe") == 0;
    }

    bool IsTextInputClass(PCWSTR className)
    {
        if (className == nullptr || className[0] == L'\0')
            return false;

        return StartsWith(className, L"Edit") ||
               StartsWith(className, L"RichEdit") ||
               StartsWith(className, L"WindowsForms10.EDIT") ||
               StartsWith(className, L"Scintilla");
    }

    bool IsWin32TextInputFocused(HWND hwnd)
    {
        auto foregroundThread = GetWindowThreadProcessId(hwnd, nullptr);
        auto currentThread = GetCurrentThreadId();
        auto attached = FALSE;

        if (foregroundThread != 0 && foregroundThread != currentThread)
            attached = AttachThreadInput(currentThread, foregroundThread, TRUE);

        auto focused = GetFocus();

        if (attached)
            AttachThreadInput(currentThread, foregroundThread, FALSE);

        if (focused == nullptr)
            return false;

        WCHAR className[MAX_CLASS_NAME_LENGTH] = { L'\0' };
        if (GetClassNameW(focused, className, MAX_CLASS_NAME_LENGTH) == 0)
            return false;

        return IsTextInputClass(className);
    }

    bool CreateAutomation(IUIAutomation** automation)
    {
        auto hr = CoCreateInstance(
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

    bool IsAutomationTextInputFocused(HWND hwnd)
    {
        DWORD foregroundProcessId = 0;
        GetWindowThreadProcessId(hwnd, &foregroundProcessId);
        if (foregroundProcessId == 0)
            return false;

        auto coInit = CoInitialize(nullptr);
        if (FAILED(coInit))
            return false;

        IUIAutomation* automation = nullptr;
        if (!CreateAutomation(&automation))
        {
            CoUninitialize();
            return false;
        }

        IUIAutomationElement* focused = nullptr;
        auto result = false;

        if (SUCCEEDED(automation->GetFocusedElement(&focused)) && focused != nullptr)
        {
            int focusedProcessId = 0;
            if (SUCCEEDED(focused->get_CurrentProcessId(&focusedProcessId)) &&
                focusedProcessId == static_cast<int>(foregroundProcessId))
            {
                CONTROLTYPEID controlType = 0;
                if (SUCCEEDED(focused->get_CurrentControlType(&controlType)) &&
                    (controlType == UIA_EditControlTypeId || controlType == UIA_ComboBoxControlTypeId))
                {
                    result = true;
                }

                if (!result)
                {
                    BSTR className = nullptr;
                    if (SUCCEEDED(focused->get_CurrentClassName(&className)) && className != nullptr)
                    {
                        result = IsTextInputClass(className);
                        SysFreeString(className);
                    }
                }
            }

            focused->Release();
        }

        automation->Release();
        CoUninitialize();

        return result;
    }

    bool IsTextInputFocused(HWND hwnd)
    {
        return IsWin32TextInputFocused(hwnd) || IsAutomationTextInputFocused(hwnd);
    }

    void SendCopyPathHotkey()
    {
        INPUT inputs[6] = {};

        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].ki.wVk = VK_CONTROL;

        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].ki.wVk = VK_SHIFT;

        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].ki.wVk = L'C';

        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].ki.wVk = L'C';
        inputs[3].ki.dwFlags = KEYEVENTF_KEYUP;

        inputs[4].type = INPUT_KEYBOARD;
        inputs[4].ki.wVk = VK_SHIFT;
        inputs[4].ki.dwFlags = KEYEVENTF_KEYUP;

        inputs[5].type = INPUT_KEYBOARD;
        inputs[5].ki.wVk = VK_CONTROL;
        inputs[5].ki.dwFlags = KEYEVENTF_KEYUP;

        SendInput(_countof(inputs), inputs, sizeof(INPUT));
    }

    bool ClearClipboard()
    {
        auto start = GetTickCount64();

        while (GetTickCount64() - start < CLIPBOARD_TIMEOUT_MS)
        {
            if (OpenClipboard(nullptr))
            {
                EmptyClipboard();
                CloseClipboard();
                return true;
            }

            Sleep(CLIPBOARD_POLL_INTERVAL_MS);
        }

        return false;
    }

    bool ReadClipboardPath(PWCHAR buffer)
    {
        if (!OpenClipboard(nullptr))
            return false;

        auto data = GetClipboardData(CF_UNICODETEXT);
        if (data == nullptr)
        {
            CloseClipboard();
            return false;
        }

        auto text = static_cast<PWCHAR>(GlobalLock(data));
        if (text == nullptr)
        {
            CloseClipboard();
            return false;
        }

        auto start = text;
        while (*start == L' ' || *start == L'\t' || *start == L'\r' || *start == L'\n')
            start++;

        auto end = start;
        while (*end != L'\0' && *end != L'\r' && *end != L'\n')
            end++;

        while (end > start && (*(end - 1) == L' ' || *(end - 1) == L'\t'))
            end--;

        if (end - start >= 2 && *start == L'"' && *(end - 1) == L'"')
        {
            start++;
            end--;
        }

        auto length = static_cast<size_t>(end - start);
        if (length >= MAX_PATH_EX)
            length = MAX_PATH_EX - 1;

        if (length > 0)
            wcsncpy_s(buffer, MAX_PATH_EX, start, length);

        GlobalUnlock(data);
        CloseClipboard();

        return length > 0;
    }

    bool WaitForClipboardPath(PWCHAR buffer)
    {
        auto start = GetTickCount64();

        while (GetTickCount64() - start < CLIPBOARD_TIMEOUT_MS)
        {
            if (ReadClipboardPath(buffer))
                return true;

            Sleep(CLIPBOARD_POLL_INTERVAL_MS);
        }

        return false;
    }
}

bool FilePilot::MatchWindow(HWND hwnd)
{
    return hwnd != nullptr && IsFilePilotProcess(hwnd) && !IsTextInputFocused(hwnd);
}

void FilePilot::GetSelected(PWCHAR buffer)
{
    if (!MatchWindow(GetForegroundWindow()))
        return;

    auto coInit = CoInitialize(nullptr);
    CComPtr<IDataObject> clipboardBackup;

    if (SUCCEEDED(coInit))
        OleGetClipboard(&clipboardBackup);

    auto clipboardCleared = ClearClipboard();
    if (clipboardCleared)
    {
        SendCopyPathHotkey();
        WaitForClipboardPath(buffer);
    }

    if (clipboardCleared && clipboardBackup != nullptr)
    {
        OleSetClipboard(clipboardBackup);
        OleFlushClipboard();
    }
    else if (clipboardCleared)
    {
        ClearClipboard();
    }

    if (SUCCEEDED(coInit))
        CoUninitialize();
}
