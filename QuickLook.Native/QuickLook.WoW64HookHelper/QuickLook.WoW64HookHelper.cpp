// QuickLook.WoW64HookHelper.cpp : Defines the entry point for the application.
//

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
                           _In_opt_ HINSTANCE hPrevInstance,
                           _In_ LPWSTR lpCmdLine,
                           _In_ int nCmdShow)
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
