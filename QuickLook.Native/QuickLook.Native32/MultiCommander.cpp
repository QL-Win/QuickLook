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
#include "MultiCommander.h"

HWND     MultiCommander::hMsgWnd          = nullptr;
HANDLE   MultiCommander::hGetResultEvent  = nullptr;
PCHAR    MultiCommander::pCurrentItemPath = nullptr;

void MultiCommander::GetSelected(PWCHAR buffer)
{
    if (false == PrepareMessageWindow()) { 
        return;
    }

    COPYDATASTRUCT cds;
	cds.dwData = MULTICMD_CPF_GETCURITEMFULL | MULTICMD_CPF_SOURCE;
	cds.cbData = 0;
	cds.lpData = nullptr;

    ResetEvent(hGetResultEvent);

    auto ret = SendMessage(
                   FindWindow(MULTICMD_CLASS, nullptr),
                   WM_COPYDATA,
                   reinterpret_cast<WPARAM>(hMsgWnd),
		           reinterpret_cast<LPARAM>(&cds)
               );

	if (!ret || WAIT_OBJECT_0 != WaitForSingleObject(hGetResultEvent, 2000)) {
        return;
	}

    auto path = reinterpret_cast<PWCHAR>(pCurrentItemPath);
    wcscpy_s(buffer, wcslen(path) + 1, path);

    delete[] pCurrentItemPath;
    pCurrentItemPath = nullptr;
}

bool MultiCommander::PrepareMessageWindow()
{
    if (nullptr == hMsgWnd) {
        WNDCLASSEX wx = {};

        wx.cbSize        = sizeof(WNDCLASSEX);
        wx.lpfnWndProc   = msgWindowProc;
        wx.lpszClassName = MULTICMD_MSGWINDOW_CLASS;

        if (RegisterClassEx(&wx))
            hMsgWnd = CreateWindowEx(0, MULTICMD_MSGWINDOW_CLASS, L"", 0, 0, 0, 0, 0, HWND_MESSAGE, nullptr, nullptr, nullptr);

        if (nullptr == hMsgWnd) {
            return false;
        }
    }

    if (nullptr == hGetResultEvent) {
        hGetResultEvent = CreateEvent(nullptr, TRUE, FALSE, nullptr);
    }

    return (nullptr != hGetResultEvent);
}

LRESULT CALLBACK MultiCommander::msgWindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
    switch (uMsg)
	{
		case WM_COPYDATA:
		{
            delete[] pCurrentItemPath;

			auto cds = reinterpret_cast<PCOPYDATASTRUCT>(lParam);
			auto buf = static_cast<PCHAR>(cds->lpData);

			pCurrentItemPath = new CHAR[cds->cbData + 1]{ '\0' };
			memcpy(pCurrentItemPath, buf, cds->cbData);

            SetEvent(hGetResultEvent);
            return 0;
		}
		default:
            return DefWindowProc(hWnd, uMsg, wParam, lParam);
	}
}
