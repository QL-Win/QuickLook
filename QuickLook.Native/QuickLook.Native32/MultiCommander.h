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

#pragma once

#define MULTICMD_CPF_GETCURITEMFULL 0x00000010L // Get full path of current item (file or folder) in focus
#define MULTICMD_CPF_SOURCE         0x00000400L // Go to the new path in the source panel side

#define MULTICMD_CLASS              L"MultiCommander MainWnd"
#define MULTICMD_MSGWINDOW_CLASS    L"QuickLook.Native.MultiCmd.MsgWindow"

class MultiCommander
{
public:
	static void GetSelected(PWCHAR buffer);
    static bool PrepareMessageWindow();
    MultiCommander() = delete;
private:
    static HWND     hMsgWnd;
	static HANDLE   hGetResultEvent;
	static PCHAR    pCurrentItemPath;

    static LRESULT CALLBACK msgWindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
};

