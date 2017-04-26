// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"

#include "Shell32.h"

#define EXPORT extern "C" __declspec(dllexport)

EXPORT int GetFocusedWindowType()
{
	return Shell32::GetFocusedWindowType();
}

EXPORT void SaveCurrentSelection()
{
	Shell32::SaveCurrentSelection();
}

EXPORT int GetCurrentSelectionCount()
{
	return Shell32::GetCurrentSelectionCount();
}

EXPORT void GetCurrentSelectionBuffer(PWCHAR buffer)
{
	Shell32::GetCurrentSelectionBuffer(buffer);
}
