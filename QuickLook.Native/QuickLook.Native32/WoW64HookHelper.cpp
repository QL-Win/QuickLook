#include "stdafx.h"
#include "WoW64HookHelper.h"

#define HELPER_FILE L"\\QuickLook.WoW64HookHelper.exe"
#define RUN_ARG L"033A853A-E4B2-4552-9A91-E88789761C48"

HANDLE hHelper = nullptr;
HANDLE hJob = nullptr;

bool WoW64HookHelper::CheckStatus()
{
	DWORD running = -1;
	GetExitCodeProcess(hHelper, &running);
	return running == STILL_ACTIVE;
}

bool WoW64HookHelper::Launch()
{
	if (CheckStatus())
		return true;

	createJob();

	WCHAR fullPath[MAX_PATH] = {'\0'};
	GetModuleFileName(nullptr, fullPath, MAX_PATH - 1);
	auto p = wcsrchr(fullPath, L'\\');
	memcpy(p, HELPER_FILE, wcslen(HELPER_FILE) * sizeof WCHAR);

	STARTUPINFO si = {'\0'};
	PROCESS_INFORMATION pi = {nullptr};
	si.cb = sizeof si;

	CreateProcess(fullPath, RUN_ARG, nullptr, nullptr, false, 0, nullptr, nullptr, &si, &pi);
	hHelper = pi.hProcess;

	AssignProcessToJobObject(hJob, hHelper);

	return CheckStatus();
}

void WoW64HookHelper::createJob()
{
	if (hJob != nullptr)
		return;

	hJob = CreateJobObject(nullptr, nullptr);

	JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation = {'\0'};
	BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
	JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo = {'\0'};
	lpJobObjectInfo.BasicLimitInformation = BasicLimitInformation;

	SetInformationJobObject(hJob, JobObjectExtendedLimitInformation, &lpJobObjectInfo, sizeof JOBOBJECT_EXTENDED_LIMIT_INFORMATION);
}
