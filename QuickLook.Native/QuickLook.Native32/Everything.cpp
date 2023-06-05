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
#include "Everything.h"

#define EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASSW							L"EVERYTHING"
#define EVERYTHING_IPC_ID_FILE_COPY_FULL_PATH_AND_NAME					41007
static HANDLE backupData = nullptr;
static HANDLE backupMutex = nullptr;

void Everything::GetSelected(PWCHAR buffer)
{
	if (SendMessage(
		FindWindow(EVERYTHING_IPC_SEARCH_CLIENT_WNDCLASSW, nullptr),
		WM_COMMAND,
		MAKEWPARAM(EVERYTHING_IPC_ID_FILE_COPY_FULL_PATH_AND_NAME, 0),
		0))
		return;

	if (!OpenClipboard(nullptr))
		return;

	auto hData = GetClipboardData(CF_UNICODETEXT);
	if (hData == nullptr)
		return;

	auto pText = static_cast<PWCHAR>(GlobalLock(hData));
	if (pText == nullptr)
		return;

	auto p = wcsstr(pText, L"\r\n");
	auto l = p == nullptr ? wcslen(pText) : p - pText;
	wcsncpy_s(buffer, MAX_PATH_EX, pText, l); // Everything supports Long Path

	GlobalUnlock(hData);

	CloseClipboard();
}

void Everything::backupClipboard()
{
	if (!OpenClipboard(nullptr))
		return;

	auto hData = GetClipboardData(CF_UNICODETEXT);
	if (hData == nullptr)
	{
		CloseClipboard();
		return;
	}

	backupMutex = CreateMutex(nullptr, FALSE, nullptr);
	if (backupMutex == nullptr)
	{
		CloseClipboard();
		return;
	}

	WaitForSingleObject(backupMutex, INFINITE);
	backupData = GlobalAlloc(GMEM_MOVEABLE, sizeof(WCHAR) * (wcslen(static_cast<PWCHAR>(GlobalLock(hData))) + 1));
	if (backupData != nullptr)
	{
		memcpy(GlobalLock(backupData), GlobalLock(hData), sizeof(WCHAR) * GlobalSize(hData));
		GlobalUnlock(backupData);
	}
	GlobalUnlock(hData);
	ReleaseMutex(backupMutex);

	CloseClipboard();
}

void Everything::restoreClipboard()
{
	backupMutex = OpenMutex(MUTEX_ALL_ACCESS, FALSE, L"backupMutex");
	if (backupMutex == nullptr)
		return;

	WaitForSingleObject(backupMutex, INFINITE);
	if (backupData == nullptr)
	{
		ReleaseMutex(backupMutex);
		return;
	}

	if (!OpenClipboard(nullptr))
	{
		ReleaseMutex(backupMutex);
		return;
	}

	EmptyClipboard();

	auto success = SetClipboardData(CF_UNICODETEXT, backupData);
	CloseClipboard();

	if (!success)
	{
		GlobalFree(backupData);
	}

	backupData = nullptr;
	ReleaseMutex(backupMutex);
}