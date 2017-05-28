#include "stdafx.h"

#include "Shell32.h"
#include <atlcomcli.h>

using namespace std;

Shell32::FocusedWindowType Shell32::GetFocusedWindowType()
{
	auto type = INVALID;

	auto hwndfg = GetForegroundWindow();

	if (isCursorActivated(hwndfg))
		return INVALID;

	auto classBuffer = new WCHAR[MAX_PATH];
	if (SUCCEEDED(GetClassName(hwndfg, classBuffer, MAX_PATH)))
	{
		if (wcscmp(classBuffer, L"WorkerW") == 0 || wcscmp(classBuffer, L"Progman") == 0)
		{
			if (FindWindowEx(hwndfg, nullptr, L"SHELLDLL_DefView", nullptr) != nullptr)
			{
				type = DESKTOP;
			}
		}
		else if (wcscmp(classBuffer, L"ExploreWClass") == 0 || wcscmp(classBuffer, L"CabinetWClass") == 0)
		{
			type = EXPLORER;
		}
	}
	delete[] classBuffer;

	return type;
}

void Shell32::GetCurrentSelection(PWCHAR buffer)
{
	switch (GetFocusedWindowType())
	{
	case EXPLORER:
		getSelectedFromExplorer(buffer);
		break;
	case DESKTOP:
		getSelectedFromDesktop(buffer);
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

	auto hwndFGW = GetForegroundWindow();

	auto count = 0L;
	psw->get_Count(&count);

	for (auto i = 0; i < count; i++)
	{
		VARIANT vi;
		V_VT(&vi) = VT_I4;
		V_I4(&vi) = i;

		CComPtr<IDispatch> pdisp;
		// ReSharper disable once CppSomeObjectMembersMightNotBeInitialized
		if (FAILED(psw->Item(vi, &pdisp)))
			continue;

		CComQIPtr<IWebBrowserApp> pwba;
		if (FAILED(pdisp->QueryInterface(IID_IWebBrowserApp, reinterpret_cast<void**>(&pwba))))
			continue;

		HWND hwndWBA;
		if (FAILED(pwba->get_HWND(reinterpret_cast<LONG_PTR*>(&hwndWBA))))
			continue;

		if (hwndWBA != hwndFGW || isCursorActivated(hwndWBA))
			continue;

		getSelectedInternal(pwba, buffer);
	}
}

void Shell32::getSelectedFromDesktop(PWCHAR buffer)
{
	CoInitialize(nullptr);

	CComPtr<IShellWindows> psw;
	CComQIPtr<IWebBrowserApp> pwba;

	if (FAILED(psw.CoCreateInstance(CLSID_ShellWindows)))
		return;

	VARIANT pvarLoc = {VT_EMPTY};
	long phwnd;
	if (FAILED(psw->FindWindowSW(&pvarLoc, &pvarLoc, SWC_DESKTOP, &phwnd, SWFO_NEEDDISPATCH, reinterpret_cast<IDispatch**>(&pwba))))
		return;

	if (isCursorActivated(reinterpret_cast<HWND>(phwnd)))
		return;

	getSelectedInternal(pwba, buffer);
}

void Shell32::getSelectedInternal(CComQIPtr<IWebBrowserApp> pwba, PWCHAR buffer)
{
	CComQIPtr<IServiceProvider> psp;
	if (FAILED(pwba->QueryInterface(IID_IServiceProvider, reinterpret_cast<void**>(&psp))))
		return;

	CComPtr<IShellBrowser> psb;
	if (FAILED(psp->QueryService(SID_STopLevelBrowser, IID_IShellBrowser, reinterpret_cast<LPVOID*>(&psb))))
		return;

	CComPtr<IShellView> psv;
	if (FAILED(psb->QueryActiveShellView(&psv)))
		return;

	CComPtr<IDataObject> dao;
	if (FAILED(psv->GetItemObject(SVGIO_SELECTION, IID_IDataObject, reinterpret_cast<void**>(&dao))))
		return;

	return obtainFirstItem(dao, buffer);
}

void Shell32::obtainFirstItem(CComPtr<IDataObject> dao, PWCHAR buffer)
{
	FORMATETC formatetc;
	STGMEDIUM medium;

	formatetc.cfFormat = CF_HDROP;
	formatetc.ptd = nullptr;
	formatetc.dwAspect = DVASPECT_CONTENT;
	formatetc.lindex = -1;
	formatetc.tymed = TYMED_HGLOBAL;

	medium.tymed = TYMED_HGLOBAL;

	if (FAILED(dao->GetData(&formatetc, &medium)))
		return;

	int n = DragQueryFile(HDROP(medium.hGlobal), 0xFFFFFFFF, nullptr, 0);

	if (n < 1)
		return;

	DragQueryFile(HDROP(medium.hGlobal), 0, buffer, MAX_PATH - 1);
}

bool Shell32::isCursorActivated(HWND hwnd)
{
	auto tId = GetWindowThreadProcessId(hwnd, nullptr);

	GUITHREADINFO gui = {sizeof gui};
	GetGUIThreadInfo(tId, &gui);
	return gui.flags || gui.hwndCaret;
}
