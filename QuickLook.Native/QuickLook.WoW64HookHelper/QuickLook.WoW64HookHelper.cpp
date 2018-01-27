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

#include "QuickLook.WoW64HookHelper.h"
#include <winuser.h>
#include <cwchar>

typedef void (__cdecl *PGCS)(PWCHAR);

#define RUN_ARG L"033A853A-E4B2-4552-9A91-E88789761C48"

#define SHARED_MEM_NAME L"QUICKLOOK_WOW64HOOKHELPER_MEM"
#define MSG_WINDOW_CLASS L"QUICKLOOK_WOW64HOOKHELPER_MSG_CLASS"

HMODULE pDll = nullptr;
PGCS pGCS = nullptr;
HWND hMsgWindow = nullptr;
UINT WM_HOOK_NOTIFY = 0;

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
                           _In_opt_                           HINSTANCE hPrevInstance,
                           _In_                           LPWSTR lpCmdLine,
                           _In_                           int nCmdShow)
{
	// do not run when double-clicking
	if (wcsstr(GetCommandLine(), RUN_ARG) == nullptr)
	{
		MessageBox(nullptr, L"This executable is not designed to launch directly.", L"QuickLook.WoW64HookHelper", 0);
		return 0;
	}

	pDll = LoadLibrary(L"QuickLook.Native32.dll");
	pGCS = reinterpret_cast<PGCS>(GetProcAddress(pDll, "GetCurrentSelection"));

	WM_HOOK_NOTIFY = RegisterWindowMessage(L"WM_QUICKLOOK_HOOK_NOTIFY_MSG");

	WNDCLASS wc = {};
	wc.lpfnWndProc = WndProc;
	wc.lpszClassName = MSG_WINDOW_CLASS;
	if (!RegisterClass(&wc))
		return 0;

	hMsgWindow = CreateWindow(MSG_WINDOW_CLASS, nullptr, 0, 0, 0, 0, 0, HWND_MESSAGE, nullptr, nullptr, nullptr);
	if (hMsgWindow == nullptr)
		return 0;

	MSG msg;
	while (GetMessage(&msg, nullptr, 0, 0) > 0)
	{
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
	return msg.wParam;
}

LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	if (msg == WM_HOOK_NOTIFY)
	{
		GetCurrentSelection();
		return 0;
	}
	return DefWindowProc(hwnd, msg, wParam, lParam);
}

void GetCurrentSelection()
{
	WCHAR dllBuffer[MAX_PATH] = {'\0'};
	pGCS(dllBuffer);

	auto hMapFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, false, SHARED_MEM_NAME);
	if (hMapFile == nullptr)
		return;

	auto buffer = static_cast<PWCHAR>(MapViewOfFile(hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, MAX_PATH * sizeof WCHAR));
	if (buffer == nullptr)
	{
		CloseHandle(hMapFile);
		return;
	}

	wcscpy_s(buffer, MAX_PATH, dllBuffer);

	UnmapViewOfFile(buffer);
	CloseHandle(hMapFile);
}
