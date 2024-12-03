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

#define EVERYTHING_IPC_HIDDEN_WIN_CLASS			L"EVERYTHING_RESULT_LIST_FOCUS"
#define EVERYTHING_IPC_WINDOW_CLASS				L"EVERYTHING"
#define EVERYTHING_IPC_COPY_TO_CLIPBOARD		41007

#pragma once
class Everything
{
public:
	static void GetSelected(PWCHAR buffer);
	static bool MatchClass(PWCHAR classBuffer);

private:
	static void backupClipboard();
	static void restoreClipboard();
};
