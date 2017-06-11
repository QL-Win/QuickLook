#include "stdafx.h"

#include "Shell32.h"

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
		else if (wcscmp(classBuffer, L"#32770") == 0)
		{
			if (FindWindowEx(hwndfg, nullptr, L"DUIViewWndClassName", nullptr) != nullptr)
			{
				type = DIALOG;
			}
		}
	}
	delete[] classBuffer;

	return type;
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
		getSelectedFromCommonDialog(buffer);
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
		V_VT(&vi) = VT_I4;
		V_I4(&vi) = i;

		CComPtr<IDispatch> pdisp;
		// ReSharper disable once CppSomeObjectMembersMightNotBeInitialized
		if (FAILED(psw->Item(vi, &pdisp)))
			continue;

		CComQIPtr<IWebBrowserApp> pwba;
		if (FAILED(pdisp->QueryInterface(IID_IWebBrowserApp, reinterpret_cast<void**>(&pwba))))
			continue;

		HWND hwndwba;
		if (FAILED(pwba->get_HWND(reinterpret_cast<LONG_PTR*>(&hwndwba))))
			continue;

		if (hwndwba != hwndfg || isCursorActivated(hwndwba))
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

	if (isCursorActivated(reinterpret_cast<HWND>(LongToHandle(phwnd))))
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

#pragma region DialogHook

#pragma comment(linker, "/SECTION:.shared,RWS")
#pragma data_seg(".shared")
HWND ghMsgWindow = nullptr; // Window handle
HHOOK ghHook = nullptr; // Hook handle
UINT WM_HOOK_NOTIFY = 0;
WCHAR filePathBuffer[MAX_PATH] = {'\0'};
#pragma data_seg()

void Shell32::getSelectedFromCommonDialog(PWCHAR buffer)
{
	auto hwndfg = GetForegroundWindow();
	auto tid = GetWindowThreadProcessId(hwndfg, nullptr);

	if (WM_HOOK_NOTIFY == 0)
		WM_HOOK_NOTIFY = RegisterWindowMessage(L"WM_QUICKLOOK_HOOK_NOTIFY_MSG");

	if (ghHook != nullptr)
		UnhookWindowsHookEx(ghHook);
	ghHook = SetWindowsHookEx(WH_CALLWNDPROC, static_cast<HOOKPROC>(MsgHookProc), ModuleFromAddress(MsgHookProc), tid);
	if (ghHook == nullptr)
		return;

	SendMessage(hwndfg, WM_HOOK_NOTIFY, 0, 0);
	wcscpy_s(buffer, wcslen(buffer) - 1, filePathBuffer);
}

HMODULE Shell32::ModuleFromAddress(PVOID pv)
{
	MEMORY_BASIC_INFORMATION mbi;
	return VirtualQuery(pv, &mbi, sizeof mbi) != 0 ? static_cast<HMODULE>(mbi.AllocationBase) : nullptr;
}

LRESULT Shell32::MsgHookProc(int nCode, WPARAM wParam, LPARAM lParam)
{
	if (nCode < 0)
		goto CONTINUE;

	auto pMSG = reinterpret_cast<CWPSTRUCT*>(lParam);

	// only response to WM_HOOK_NOTIFY
	if (pMSG->message != WM_HOOK_NOTIFY)
		goto CONTINUE;

	UnhookWindowsHookEx(ghHook);
	ghHook = nullptr;

	// get current selected item
	auto psb = reinterpret_cast<IShellBrowser*>(SendMessage(GetForegroundWindow(), WM_USER + 7, 0, 0));
	if (psb == nullptr)
		goto CONTINUE;

	getSelectedInternal2(psb, filePathBuffer);

	return 0;

CONTINUE:
	return CallNextHookEx(ghHook, nCode, wParam, lParam);
}

void Shell32::getSelectedInternal2(CComPtr<IShellBrowser> psb, PWCHAR buffer)
{
	CComPtr<IShellView> psv;
	if (FAILED(psb->QueryActiveShellView(&psv)))
		return;

	CComPtr<IDataObject> dao;
	if (FAILED(psv->GetItemObject(SVGIO_SELECTION, IID_IDataObject, reinterpret_cast<void**>(&dao))))
		return;

	return obtainFirstItem(dao, buffer);
}

#pragma endregion DialogHook
