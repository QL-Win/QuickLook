#include "stdafx.h"

#include "Shell32.h"

using namespace std;

vector<wstring> Shell32::vector_items;

void Shell32::SaveCurrentSelection()
{
	vector_items.clear();

	switch (GetFocusedWindowType())
	{
	case EXPLORER:
		SaveSelectedFromExplorer();
		break;
	case DESKTOP:
		SaveSelectedFromDesktop();
		break;
	default:
		break;
	}
}

UINT Shell32::GetCurrentSelectionCount()
{
	return vector_items.size();
}

void Shell32::GetCurrentSelectionBuffer(PWCHAR buffer)
{
	PWCHAR pos = buffer;

	for (vector<wstring>::iterator it = vector_items.begin(); it < vector_items.end(); ++it)
	{
		int l = it->length();
		wcscpy_s(pos, l + 1, it->c_str());

		pos += l;

		// overwrite NULL
		wcscpy_s(pos++, 2, L"|");
	}

	// remove last "|"
	wcscpy_s(pos - 1, 1, L"");
}

void Shell32::SaveSelectedFromExplorer()
{
	CoInitialize(nullptr);

	CComPtr<IShellWindows> psw;
	HRESULT ret = psw.CoCreateInstance(CLSID_ShellWindows);

	auto hwndFGW = GetForegroundWindow();

	auto fFound = FALSE;

	for (int i = 0; !fFound; i++)
	{
		VARIANT vi;
		V_VT(&vi) = VT_I4;
		V_I4(&vi) = i;

		CComPtr<IDispatch> pdisp;
		// ReSharper disable once CppSomeObjectMembersMightNotBeInitialized
		if (SUCCEEDED(psw->Item(vi, &pdisp)))
		{
			CComPtr<IWebBrowserApp> pwba;
			if (SUCCEEDED(pdisp->QueryInterface(IID_IWebBrowserApp, reinterpret_cast<void**>(&pwba))))
			{
				HWND hwndWBA;
				if (SUCCEEDED(pwba->get_HWND(reinterpret_cast<LONG_PTR*>(&hwndWBA))) && hwndWBA == hwndFGW)
				{
					fFound = TRUE;

					CComPtr<IDispatch> ppdisp;
					if (SUCCEEDED(pwba->get_Document(&ppdisp)))
					{
						CComPtr<IShellFolderViewDual2> pshvd;
						if (SUCCEEDED(ppdisp->QueryInterface(IID_IShellFolderViewDual2, reinterpret_cast<void**>(&pshvd))))
						{
							CComPtr<FolderItems> pfis;
							if (SUCCEEDED(pshvd->SelectedItems(&pfis)))
							{
								LONG pCount = 0L;
								pfis->get_Count(&pCount);

								for (int ii = 0; ii < pCount; ii++)
								{
									VARIANT vii;
									V_VT(&vii) = VT_I4;
									V_I4(&vii) = ii;

									CComPtr<FolderItem> pfi;
									// ReSharper disable once CppSomeObjectMembersMightNotBeInitialized
									if (SUCCEEDED(pfis->Item(vii, &pfi)))
									{
										CComBSTR pbs = SysAllocStringLen(L"", MAX_PATH);
										pfi->get_Path(&pbs);

										wstring ws = wstring(pbs);
										ws.shrink_to_fit();

										vector_items.push_back(ws);
									}
								}
							}
						}
					}
				}
			}
		}
	}
}

CComQIPtr<IWebBrowser2> Shell32::AttachDesktopShellWindow()
{
	CoInitialize(nullptr);

	CComPtr<IShellWindows> psw;
	CComQIPtr<IWebBrowser2> pdispOut;

	if (SUCCEEDED(psw.CoCreateInstance(CLSID_ShellWindows)))
	{
		VARIANT pvarLoc = {VT_EMPTY};
		long phwnd;
		psw->FindWindowSW(&pvarLoc, &pvarLoc, SWC_DESKTOP, &phwnd, SWFO_NEEDDISPATCH, reinterpret_cast<IDispatch**>(&pdispOut));
	}
	return pdispOut;
}

void Shell32::SaveSelectedFromDesktop()
{
	auto pWebBrowser2 = AttachDesktopShellWindow();

	if (!pWebBrowser2)
		return;

	CComQIPtr<IServiceProvider> psp(pWebBrowser2);
	CComPtr<IShellBrowser> psb;
	CComPtr<IShellView> psv;
	CComPtr<IFolderView> pfv;
	CComPtr<IPersistFolder2> ppf2;

	if (!psp) return;

	if (SUCCEEDED(psp->QueryService(SID_STopLevelBrowser, IID_IShellBrowser, reinterpret_cast<LPVOID*>(&psb))))
	{
		if (SUCCEEDED(psb->QueryActiveShellView(&psv)))
		{
			if (SUCCEEDED(psv->QueryInterface(IID_IFolderView, reinterpret_cast<void**>(&pfv))))
			{
				if (SUCCEEDED(pfv->GetFolder(IID_IPersistFolder2, reinterpret_cast<void**>(&ppf2))))
				{
					LPITEMIDLIST pidlFolder;
					if (SUCCEEDED(ppf2->GetCurFolder(&pidlFolder)))
					{
						CComPtr<IDataObject> dao;
						if (SUCCEEDED(psv->GetItemObject(SVGIO_SELECTION, IID_IDataObject, reinterpret_cast<void**>(&dao))))
							vectorFromDataObject(dao);
					}
					CoTaskMemFree(pidlFolder);
				}
			}
		}
	}
}

void Shell32::vectorFromDataObject(CComPtr<IDataObject> dao)
{
	FORMATETC formatetc;
	STGMEDIUM medium;

	formatetc.cfFormat = CF_HDROP;
	formatetc.ptd = nullptr;
	formatetc.dwAspect = DVASPECT_CONTENT;
	formatetc.lindex = -1;
	formatetc.tymed = TYMED_HGLOBAL;

	medium.tymed = TYMED_HGLOBAL;

	if (SUCCEEDED(dao->GetData(&formatetc, &medium)))
	{
		int n = DragQueryFile(HDROP(medium.hGlobal), 0xFFFFFFFF, nullptr, 0);

		for (int i = 0; i < n; i++)
		{
			WCHAR buffer[MAX_PATH];
			DragQueryFile(HDROP(medium.hGlobal), i, buffer, MAX_PATH - 1);

			wstring ws = wstring(buffer);
			ws.shrink_to_fit();

			vector_items.push_back(ws);
		}
	}
}

Shell32::FocusedWindowType Shell32::GetFocusedWindowType()
{
	auto type = INVALID;

	auto hwndfg = GetForegroundWindow();

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
