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

#include "Shell32.h"
#include "HelperMethods.h"
#include "DialogHook.h"
#include "Everything.h"
#include "DOpus.h"

using namespace std;

Shell32::FocusedWindowType Shell32::GetFocusedWindowType()
{
	auto hwndfg = GetForegroundWindow();

	if (HelperMethods::IsCursorActivated(hwndfg))
		return INVALID;

	WCHAR classBuffer[MAX_PATH] = { '\0' };
	if (FAILED(GetClassName(hwndfg, classBuffer, MAX_PATH)))
		return INVALID;

	if (wcscmp(classBuffer, L"dopus.lister") == 0)
	{
		return DOPUS;
	}
	if (wcscmp(classBuffer, L"EVERYTHING") == 0 || wcscmp(classBuffer, L"EVERYTHING_SHELL_EXECUTE") == 0)
	{
		return EVERYTHING;
	}
	if (wcscmp(classBuffer, L"WorkerW") == 0 || wcscmp(classBuffer, L"Progman") == 0)
	{
		if (FindWindowEx(hwndfg, nullptr, L"SHELLDLL_DefView", nullptr) != nullptr)
		{
			return DESKTOP;
		}
	}
	if (wcscmp(classBuffer, L"ExploreWClass") == 0 || wcscmp(classBuffer, L"CabinetWClass") == 0)
	{
		if (!HelperMethods::IsExplorerSearchBoxFocused())
		{
			return EXPLORER;
		}
	}
	if (wcscmp(classBuffer, L"#32770") == 0)
	{
		if (FindWindowEx(hwndfg, nullptr, L"DUIViewWndClassName", nullptr) != nullptr)
		{
			if (!HelperMethods::IsExplorerSearchBoxFocused())
			{
				return DIALOG;
			}
		}
	}

	return INVALID;
}

void Shell32::GetCurrentSelection(PWCHAR buffer)
{
	switch (GetFocusedWindowType())
	{
	case DESKTOP:
		getSelectedFromDesktop(buffer);
		break;
	case EXPLORER:
		getSelectedFromExplorer(buffer);
		break;
	case DIALOG:
		DialogHook::GetSelected(buffer);
		break;
	case EVERYTHING:
		Everything::GetSelected(buffer);
		break;
	case DOPUS:
		DOpus::GetSelected(buffer);
		break;
	default:
		break;
	}
}

void Shell32::getSelectedFromExplorer(PWCHAR buffer)
{
	CoInitialize(nullptr);

	CComPtr<IShellWindows> psw;
	if (FAILED(psw.CoCreateInstance(CLSID_ShellWindows)))
		return;

	auto hwndfg = GetForegroundWindow();

	auto count = 0L;
	psw->get_Count(&count);

	for (auto i = 0; i < count; i++)
	{
		VARIANT vi;
		VariantInit(&vi);
		V_VT(&vi) = VT_I4;
		V_I4(&vi) = i;

		CComPtr<IDispatch> pdisp;
		if (S_OK != psw->Item(vi, &pdisp))
			continue;

		CComQIPtr<IWebBrowserApp> pwba;
		if (FAILED(pdisp->QueryInterface(IID_IWebBrowserApp, reinterpret_cast<void**>(&pwba))))
			continue;

		HWND hwndwba;
		if (FAILED(pwba->get_HWND(reinterpret_cast<LONG_PTR*>(&hwndwba))))
			continue;

		if (hwndwba != hwndfg || HelperMethods::IsCursorActivated(hwndwba))
			continue;

		HelperMethods::GetSelectedInternal(pwba, buffer);
	}
}

void Shell32::getSelectedFromDesktop(PWCHAR buffer)
{
	CoInitialize(nullptr);

	CComPtr<IShellWindows> psw;
	CComQIPtr<IWebBrowserApp> pwba;

	if (FAILED(psw.CoCreateInstance(CLSID_ShellWindows)))
		return;

	VARIANT pvarLoc;
	VariantInit(&pvarLoc);
	long phwnd;
	if (FAILED(psw->FindWindowSW(&pvarLoc, &pvarLoc, SWC_DESKTOP, &phwnd, SWFO_NEEDDISPATCH, reinterpret_cast<IDispatch**>(
		&pwba))))
		return;

	if (HelperMethods::IsCursorActivated(reinterpret_cast<HWND>(LongToHandle(phwnd))))
		return;

	HelperMethods::GetSelectedInternal(pwba, buffer);
}
