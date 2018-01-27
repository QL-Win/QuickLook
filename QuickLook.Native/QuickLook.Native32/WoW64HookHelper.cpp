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
#include "WoW64HookHelper.h"
#include "HelperMethods.h"

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
#ifndef WIN64
	return true;
#endif

	if (HelperMethods::IsUWP())
		return true;

	if (CheckStatus())
		return true;

	createJob();

	WCHAR fullPath[MAX_PATH] = {'\0'};
	GetModuleFileName(nullptr, fullPath, MAX_PATH - 1);
	auto p = wcsrchr(fullPath, L'\\');
	memcpy(p, HELPER_FILE, wcslen(HELPER_FILE) * sizeof WCHAR);

	STARTUPINFO si = {sizeof si};
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

	JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation = {sizeof BasicLimitInformation};
	BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
	JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo = {sizeof lpJobObjectInfo};
	lpJobObjectInfo.BasicLimitInformation = BasicLimitInformation;

	SetInformationJobObject(hJob, JobObjectExtendedLimitInformation, &lpJobObjectInfo,
	                        sizeof JOBOBJECT_EXTENDED_LIMIT_INFORMATION);
}
