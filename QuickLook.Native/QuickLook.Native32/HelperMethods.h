#pragma once
class HelperMethods
{
public:
	static void GetSelectedInternal(CComQIPtr<IWebBrowserApp> pWebBrowserApp, PWCHAR buffer);
	static void ObtainFirstItem(CComPtr<IDataObject> dao, PWCHAR buffer);
	static bool IsCursorActivated(HWND hwndfg);
};
