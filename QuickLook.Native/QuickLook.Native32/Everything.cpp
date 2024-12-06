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

void Everything::GetSelected(PWCHAR buffer)
{
	auto hWinFG = GetForegroundWindow();
	
	// Everything v1.5 IPC via hidden window.
	HWND hWinI = FindWindowExW(hWinFG, NULL, EVERYTHING_IPC_HIDDEN_WIN_CLASS, NULL);
	
	if (hWinI != nullptr) {
		int pLength = GetWindowTextLength(hWinI);
		wchar_t* pText = new wchar_t[pLength + 1];
		GetWindowText(hWinI, pText, pLength + 1);
		wcsncpy_s(buffer, MAX_PATH_EX, pText, pLength);
		return; // Success. Clipboard access unnecessary.
	}

	HWND hWin = FindWindowW(EVERYTHING_IPC_WINDOW_CLASS, NULL);

	if (hWin != nullptr) {
		// Everything IPC Clipboard
		SendMessageW(
			hWin,
			WM_COMMAND,
			MAKEWPARAM(EVERYTHING_IPC_COPY_TO_CLIPBOARD, 0),
			0);

		Sleep(100);
		
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
}

bool Everything::MatchClass(PWCHAR classBuffer)
{
	WCHAR sMatchC[256] = { '\0' };
	WCHAR sMatchS[256] = EVERYTHING_IPC_WINDOW_CLASS;
	size_t iLen = wcslen(sMatchS);
	wcsncpy_s(sMatchC, classBuffer, iLen);
	return (0 == wcscmp(sMatchC, sMatchS));
}

void Everything::backupClipboard()
{
	// TODO
}

void Everything::restoreClipboard()
{
	// TODO
}
