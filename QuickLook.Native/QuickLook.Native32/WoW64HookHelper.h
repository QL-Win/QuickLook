#pragma once

#include "stdafx.h"

class WoW64HookHelper
{
public:
	static PWCHAR GetMsgWindowClassName()
	{
		return L"QUICKLOOK_WOW64HOOKHELPER_MSG_CLASS";
	}

	static bool CheckStatus();
	static bool Launch();

private:
	static void createJob();
};
