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

private:
	static void getSelectedFromDesktop(PWCHAR buffer);
	static void getSelectedFromExplorer(PWCHAR buffer);
};
