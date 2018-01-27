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
#include "DOpus.h"
#include "rapidxml.hpp"

#include <iostream>

#define DOPUS_IPC_LP_INFO 0x00000015
#define DOPUS_IPC_LP_DATA L"listsel"
#define DOPUS_CLASS L"DOpus.ParentWindow"
#define DOPUS_NAME L"Directory Opus"
#define MSGWINDOW_CLASS L"QuickLook.Native.DOpus.MsgWindow"

HWND hMsgWnd;
HANDLE hGetResultEvent;

PCHAR pXmlBuffer;

void DOpus::GetSelected(PWCHAR buffer)
{
	/*
	 * CPU Disasm
	 * 6A44B588  |.  68 A839526A   PUSH OFFSET 6A5239A8                     ; /WindowName = "Directory Opus"
	 * 6A44B58D  |.  68 6459526A   PUSH OFFSET 6A525964                     ; |ClassName = "DOpus.ParentWindow"
	 * 6A44B592  |.  894424 40     MOV DWORD PTR SS:[ESP+40],EAX            ; |
	 * 6A44B596  |.  C74424 30 140 MOV DWORD PTR SS:[ESP+30],14             ; |
	 * 6A44B59E  |.  FFD7          CALL EDI                                 ; \USER32.FindWindowW
	 * ...
	 * 00BC2E9B  |.  50            PUSH EAX                                 ; /lParam = 12FE80C -> 15     ;
	 * 00BC2E9C  |.  57            PUSH EDI                                 ; |wParam                     ; class = dopusrt.getinfo
	 * 00BC2E9D  |.  6A 4A         PUSH 4A                                  ; |Msg = WM_COPYDATA
	 * 00BC2E9F  |.  FF75 FC       PUSH DWORD PTR SS:[LOCAL.1]              ; |hWnd => [LOCAL.1]          ; class = DOpus.ParentWindow, text = Directory Opus
	 * 00BC2EA2  |.  FF15 0802C000 CALL DWORD PTR DS:[<&USER32.SendMessageW ; \USER32.SendMessageW
	 *
	 * CPU Stack
	 * 012FE80C  |00000015  
	 * 012FE810  |00000010
	 * 012FE814  |013A26C0 ; UNICODE "listsel"
	 */

	if (hMsgWnd == nullptr)
		PrepareMessageWindow();
	if (hMsgWnd == nullptr)
		return;

	PWCHAR data = DOPUS_IPC_LP_DATA;
	COPYDATASTRUCT cds;
	cds.dwData = DOPUS_IPC_LP_INFO;
	cds.cbData = static_cast<DWORD>(wcslen(data) + 1) * sizeof WCHAR;
	cds.lpData = data;

	auto ret = SendMessage(FindWindow(DOPUS_CLASS, DOPUS_NAME), WM_COPYDATA, reinterpret_cast<WPARAM>(hMsgWnd),
	                       reinterpret_cast<LPARAM>(&cds));
	if (!ret)
		return;

	WaitForSingleObject(hGetResultEvent, 2000);

	ParseXmlBuffer(buffer);

	delete[] pXmlBuffer;
}

void DOpus::ParseXmlBuffer(PWCHAR buffer)
{
	/*
	 * <?xml version="1.0" encoding="UTF-8"?>
	 * <results command="listsel" result="1">
	 *     <items display_path="C:\folder" lister="0x707f6" path="C:\folder" tab="0xb0844">
	 *         <item id="11" name="1.jpg" path="C:\folder\1.jpg" type="0" />
	 *         <item id="12" name="2.zip" path="C:\folder\2.zip" type="1" />
	 *         ...
	 */

	using namespace rapidxml;

	xml_document<> doc;
	doc.parse<0>(pXmlBuffer);

	auto results = doc.first_node("results");
	auto items = results->first_node("items");
	for (auto item = items->first_node("item"); item; item = item->next_sibling("item"))
	{
		auto path = item->first_attribute("path")->value();

		auto size = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, path, -1, nullptr, 0);
		auto b = new WCHAR[size];
		MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, path, -1, b, size);

		wcscpy_s(buffer, MAX_PATH, b);

		delete[] b;
		return; // we now cares only the first result
	}
}

void DOpus::PrepareMessageWindow()
{
	WNDCLASSEX wx = {sizeof wx};
	wx.cbSize = sizeof(WNDCLASSEX);
	wx.lpfnWndProc = msgWindowProc;
	wx.lpszClassName = MSGWINDOW_CLASS;

	if (RegisterClassEx(&wx))
		hMsgWnd = CreateWindowEx(0, MSGWINDOW_CLASS, L"", 0, 0, 0, 0, 0, HWND_MESSAGE, nullptr, nullptr, nullptr);

	hGetResultEvent = CreateEvent(nullptr, FALSE, FALSE, nullptr);
}

LRESULT CALLBACK DOpus::msgWindowProc(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	switch (uMsg)
	{
	case WM_COPYDATA:
		{
			auto cds = reinterpret_cast<PCOPYDATASTRUCT>(lParam);
			auto buf = static_cast<PCHAR>(cds->lpData);

			pXmlBuffer = new CHAR[cds->cbData + 1]{'\0'};
			memcpy(pXmlBuffer, buf, cds->cbData);

			SetEvent(hGetResultEvent);
			return 0;
		}
	default:
		return DefWindowProc(hWnd, uMsg, wParam, lParam);
	}
}
