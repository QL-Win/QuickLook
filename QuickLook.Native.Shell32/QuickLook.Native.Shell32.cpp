// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"

#include "Shell32.h"

#define EXPORT extern "C" __declspec(dllexport)

EXPORT Shell32::FocusedWindowType GetFocusedWindowType()
{
	return Shell32::GetFocusedWindowType();
}

EXPORT void GetCurrentSelection(PWCHAR buffer)
{
	Shell32::GetCurrentSelection(buffer);
}
