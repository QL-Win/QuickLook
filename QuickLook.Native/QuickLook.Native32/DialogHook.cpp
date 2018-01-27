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
#include "DialogHook.h"
#include "WoW64HookHelper.h"
#include "HelperMethods.h"


#pragma comment(linker, "/SECTION:.shared,RWS")
#pragma data_seg(".shared")

static HHOOK ghHook = nullptr; // Hook handle
static UINT WM_HOOK_NOTIFY = 0;
static WCHAR filePathBuffer[MAX_PATH] = {'\0'};

#pragma data_seg()

#define SHARED_MEM_NAME L"QUICKLOOK_WOW64HOOKHELPER_MEM"

void DialogHook::GetSelected(PWCHAR buffer)
{
	if (HelperMethods::IsUWP())
		return;

	auto hwndfg = GetForegroundWindow();
	DWORD pid = 0;
	auto tid = GetWindowThreadProcessId(hwndfg, &pid);

	auto hProc = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
	if (hProc == nullptr)
		return;

	if (WM_HOOK_NOTIFY == 0)
		WM_HOOK_NOTIFY = RegisterWindowMessage(L"WM_QUICKLOOK_HOOK_NOTIFY_MSG");

	BOOL isTargetWoW64 = false;
	if (!IsWow64Process(hProc, &isTargetWoW64))
		return;

	CloseHandle(hProc);

	BOOL isSelfWoW64 = false;
	if (!IsWow64Process(GetCurrentProcess(), &isSelfWoW64))
		return;

	if (isTargetWoW64 && !isSelfWoW64)
	{
		// if self is 64bit and target is 32bit, do this
		GetSelectedFromWoW64HookHelper(buffer);
	}
	else
	{
		if (ghHook != nullptr)
			UnhookWindowsHookEx(ghHook);
		ghHook = SetWindowsHookEx(WH_CALLWNDPROC, static_cast<HOOKPROC>(MsgHookProc), ModuleFromAddress(MsgHookProc), tid);
		if (ghHook == nullptr)
			return;

		SendMessage(hwndfg, WM_HOOK_NOTIFY, 0, 0);
		wcscpy_s(buffer, MAX_PATH, filePathBuffer);
	}
}

void DialogHook::getSelectedInternal(CComPtr<IShellBrowser> psb, PWCHAR buffer)
{
	CComPtr<IShellView> psv;
	if (FAILED(psb->QueryActiveShellView(&psv)))
		return;

	CComPtr<IDataObject> dao;
	if (FAILED(psv->GetItemObject(SVGIO_SELECTION, IID_IDataObject, reinterpret_cast<void**>(&dao))))
		return;

	return HelperMethods::ObtainFirstItem(dao, buffer);
}

void DialogHook::GetSelectedFromWoW64HookHelper(PWCHAR buffer)
{
	if (!WoW64HookHelper::CheckStatus())
		if (!WoW64HookHelper::Launch())
			return;

	auto hHelperWnd = FindWindowEx(HWND_MESSAGE, nullptr, WoW64HookHelper::GetMsgWindowClassName(), nullptr);
	if (hHelperWnd == nullptr)
		return;

	auto hMapFile = CreateFileMapping(INVALID_HANDLE_VALUE, nullptr, PAGE_READWRITE, 0, MAX_PATH * sizeof WCHAR,
	                                  SHARED_MEM_NAME);
	if (hMapFile == nullptr)
		return;

	auto sharedBuffer = static_cast<PWCHAR>(MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, MAX_PATH * sizeof WCHAR));
	if (sharedBuffer == nullptr)
	{
		CloseHandle(hMapFile);
		return;
	}

	SendMessage(hHelperWnd, WM_HOOK_NOTIFY, 0, 0);

	// the sharedBuffer should now ready
	wcscpy_s(buffer, MAX_PATH, sharedBuffer);

	UnmapViewOfFile(sharedBuffer);
	CloseHandle(hMapFile);
}

HMODULE DialogHook::ModuleFromAddress(PVOID pv)
{
	MEMORY_BASIC_INFORMATION mbi;
	return VirtualQuery(pv, &mbi, sizeof mbi) != 0 ? static_cast<HMODULE>(mbi.AllocationBase) : nullptr;
}

LRESULT DialogHook::MsgHookProc(int nCode, WPARAM wParam, LPARAM lParam)
{
	if (nCode < 0)
		goto CONTINUE;

	auto pMSG = reinterpret_cast<CWPSTRUCT*>(lParam);

	// only response to WM_HOOK_NOTIFY
	if (pMSG->message != WM_HOOK_NOTIFY)
		goto CONTINUE;

	UnhookWindowsHookEx(ghHook);
	ghHook = nullptr;

	// get current selected item
	auto psb = reinterpret_cast<IShellBrowser*>(SendMessage(GetForegroundWindow(), WM_USER + 7, 0, 0));
	if (psb == nullptr)
		goto CONTINUE;

	getSelectedInternal(psb, filePathBuffer);

	return 0;

CONTINUE:
	return CallNextHookEx(ghHook, nCode, wParam, lParam);
}
