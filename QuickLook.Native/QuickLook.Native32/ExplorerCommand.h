// Copyright © 2017-2026 QL-Win Contributors
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

#pragma once

#include <ocidl.h>
#include <shobjidl_core.h>
#include <wrl/client.h>

extern HMODULE g_module;
extern long g_moduleRefs;
extern long g_moduleLocks;

// Windows 11 top-level File Explorer command. One verb, no submenu.
class ExplorerCommand final : public IExplorerCommand, public IObjectWithSite
{
public:
    ExplorerCommand();
    ~ExplorerCommand();

    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;

    IFACEMETHODIMP GetTitle(IShellItemArray* items, PWSTR* title) override;
    IFACEMETHODIMP GetIcon(IShellItemArray* items, PWSTR* icon) override;
    IFACEMETHODIMP GetToolTip(IShellItemArray* items, PWSTR* tip) override;
    IFACEMETHODIMP GetCanonicalName(GUID* name) override;
    IFACEMETHODIMP GetState(IShellItemArray* items, BOOL okToBeSlow, EXPCMDSTATE* state) override;
    IFACEMETHODIMP Invoke(IShellItemArray* items, IBindCtx* bindCtx) override;
    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) override;
    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** commands) override;

    IFACEMETHODIMP SetSite(IUnknown* site) override;
    IFACEMETHODIMP GetSite(REFIID riid, void** ppv) override;

private:
    long m_refs;
    Microsoft::WRL::ComPtr<IUnknown> m_site;
};

class ClassFactory final : public IClassFactory
{
public:
    ClassFactory();
    ~ClassFactory();

    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;
    IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID riid, void** ppv) override;
    IFACEMETHODIMP LockServer(BOOL lock) override;

private:
    long m_refs;
};
