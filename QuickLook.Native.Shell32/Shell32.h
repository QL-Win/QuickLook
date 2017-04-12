#pragma once

#include "stdafx.h"

class Shell32
{
public:
	static void SaveCurrentSelection();
	static UINT GetCurrentSelectionCount();
	static void GetCurrentSelectionBuffer(PWCHAR buffer);

private:
	enum FocusedWindowType
	{
		INVALID,
		DESKTOP,
		EXPLORER,
	};

	static std::vector<std::wstring> vector_items;

	static void SaveSelectedFromDesktop();
	static void SaveSelectedFromExplorer();

	static FocusedWindowType GetFocusedWindowType();
	static CComQIPtr<IWebBrowser2> AttachDesktopShellWindow();
	static void vectorFromDataObject(CComPtr<IDataObject> dao);
};

