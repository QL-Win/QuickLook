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
	};

	static FocusedWindowType GetFocusedWindowType();
	static void SaveCurrentSelection();
	static UINT GetCurrentSelectionCount();
	static void GetCurrentSelectionBuffer(PWCHAR buffer);

private:
	static std::vector<std::wstring> vector_items;

	static void SaveSelectedFromDesktop();
	static void SaveSelectedFromExplorer();

	static CComQIPtr<IWebBrowser2> AttachDesktopShellWindow();
	static void vectorFromDataObject(CComPtr<IDataObject> dao);
};
