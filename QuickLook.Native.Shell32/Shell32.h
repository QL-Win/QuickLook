#pragma once

#include "stdafx.h"

class Shell32
{
public:
	enum FocusedWindowType
	{
		INVALID = 0,
		DESKTOP = 1,
		EXPLORER = 2,
		DIALOG = 3,
	};

	static FocusedWindowType GetFocusedWindowType();
	static void GetCurrentSelection(PWCHAR buffer);
	static LRESULT __stdcall WindowProcedure(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

private:
	static void getSelectedFromDesktop(PWCHAR buffer);
	static void getSelectedFromExplorer(PWCHAR buffer);

	static void getSelectedInternal(CComQIPtr<IWebBrowserApp> pWebBrowserApp, PWCHAR buffer);
	static void obtainFirstItem(CComPtr<IDataObject> dao, PWCHAR buffer);
	static bool isCursorActivated(HWND hwndfg);

#pragma region DialogHook
	static void getSelectedFromCommonDialog(PWCHAR buffer);
	static HMODULE ModuleFromAddress(PVOID pv);
	static LRESULT CALLBACK MsgHookProc(int nCode, WPARAM wParam, LPARAM lParam);
	static void getSelectedInternal2(CComPtr<IShellBrowser> psb, PWCHAR buffer);
#pragma endregion DialogHook
};
