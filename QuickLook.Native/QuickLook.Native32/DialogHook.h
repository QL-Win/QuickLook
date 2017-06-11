#pragma once
class DialogHook
{
public:
	static void GetSelectedFromCommonDialog(PWCHAR buffer);

private:
	static void getSelectedInternal(CComPtr<IShellBrowser> psb, PWCHAR buffer);
	static void GetSelectedFromWoW64HookHelper(PWCHAR buffer);
	static HMODULE ModuleFromAddress(PVOID pv);
	static LRESULT CALLBACK MsgHookProc(int nCode, WPARAM wParam, LPARAM lParam);
};
