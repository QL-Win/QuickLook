#include "stdafx.h"

#include "Shell32.h"
#include "WoW64HookHelper.h"

#define EXPORT extern "C" __declspec(dllexport)

EXPORT void LaunchWoW64HookHelper()
{
	WoW64HookHelper::Launch();
}

EXPORT Shell32::FocusedWindowType GetFocusedWindowType()
{
	return Shell32::GetFocusedWindowType();
}

EXPORT void GetCurrentSelection(PWCHAR buffer)
{
	Shell32::GetCurrentSelection(buffer);
}
