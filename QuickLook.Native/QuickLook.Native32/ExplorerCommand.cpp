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

#include "ExplorerCommand.h"

#include "ContextMenuGuid.h"
#include "resource.h"

#include <shlguid.h>
#include <shlobj.h>
#include <shlwapi.h>
#include <winnls.h>

#include <new>
#include <string>

#pragma comment(lib, "shell32.lib")
#pragma comment(lib, "ole32.lib")
#pragma comment(lib, "shlwapi.lib")
#pragma comment(lib, "uuid.lib")

namespace
{
    std::wstring ModuleFileName()
    {
        std::wstring path(32768, L'\0');
        const DWORD length = GetModuleFileNameW(g_module, &path[0], static_cast<DWORD>(path.size()));
        path.resize(length);
        return path;
    }

    std::wstring ModuleDirectory()
    {
        std::wstring path = ModuleFileName();
        const size_t slash = path.find_last_of(L"\\/");
        if (slash != std::wstring::npos)
            path.resize(slash);
        return path;
    }

    void FreeText(PWSTR text)
    {
        CoTaskMemFree(text);
    }

    bool TryGetFileSystemPath(IShellItem* item, std::wstring& path)
    {
        if (!item)
            return false;

        PWSTR name = nullptr;
        if (FAILED(item->GetDisplayName(SIGDN_FILESYSPATH, &name)) || name == nullptr || name[0] == L'\0')
        {
            FreeText(name);
            return false;
        }

        path.assign(name);
        FreeText(name);
        return true;
    }

    bool TryGetFirstFileSystemPath(IShellItemArray* items, std::wstring& path)
    {
        if (!items)
            return false;

        DWORD count = 0;
        if (FAILED(items->GetCount(&count)))
            return false;

        for (DWORD index = 0; index < count; ++index)
        {
            Microsoft::WRL::ComPtr<IShellItem> item;
            if (FAILED(items->GetItemAt(index, &item)))
                continue;
            if (TryGetFileSystemPath(item.Get(), path))
                return true;
        }

        return false;
    }

    bool TryGetFolderFromSite(IUnknown* site, std::wstring& path)
    {
        if (!site)
            return false;

        Microsoft::WRL::ComPtr<IShellBrowser> browser;
        if (FAILED(IUnknown_QueryService(site, SID_STopLevelBrowser, IID_PPV_ARGS(&browser))))
            return false;

        Microsoft::WRL::ComPtr<IShellView> view;
        if (FAILED(browser->QueryActiveShellView(&view)))
            return false;

        Microsoft::WRL::ComPtr<IFolderView> folderView;
        if (FAILED(view.As(&folderView)))
            return false;

        Microsoft::WRL::ComPtr<IPersistFolder2> folder;
        if (FAILED(folderView->GetFolder(IID_PPV_ARGS(&folder))))
            return false;

        PIDLIST_ABSOLUTE pidl = nullptr;
        if (FAILED(folder->GetCurFolder(&pidl)) || pidl == nullptr)
            return false;

        PWSTR name = nullptr;
        const HRESULT hr = SHGetNameFromIDList(pidl, SIGDN_FILESYSPATH, &name);
        CoTaskMemFree(pidl);
        if (FAILED(hr) || name == nullptr || name[0] == L'\0')
        {
            FreeText(name);
            return false;
        }

        path.assign(name);
        FreeText(name);
        return true;
    }

    HRESULT LaunchQuickLook(const std::wstring& target)
    {
        if (target.find(L'"') != std::wstring::npos)
            return E_INVALIDARG;

        const std::wstring directory = ModuleDirectory();
        const std::wstring executable = directory + L"\\QuickLook.exe";
        if (GetFileAttributesW(executable.c_str()) == INVALID_FILE_ATTRIBUTES)
            return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);

        std::wstring command = L"\"";
        command += executable;
        command += L"\" \"";
        command += target;
        command += L"\"";

        STARTUPINFOW startup{};
        startup.cb = sizeof(startup);
        startup.dwFlags = STARTF_USESHOWWINDOW;
        startup.wShowWindow = SW_SHOWNORMAL;

        PROCESS_INFORMATION process{};
        if (!CreateProcessW(executable.c_str(), &command[0], nullptr, nullptr, FALSE, 0, nullptr, directory.c_str(), &startup, &process))
            return HRESULT_FROM_WIN32(GetLastError());

        CloseHandle(process.hThread);
        CloseHandle(process.hProcess);
        return S_OK;
    }

    const wchar_t* TitleForLanguage(const wchar_t* language, bool allowParent)
    {
        struct Entry
        {
            const wchar_t* language;
            const wchar_t* title;
        };

        // Same shape as VS Code's OpenWithCodeContextMenu: a verb around the product name.
        static const Entry entries[] = {
            { L"zh-CN", L"使用 QuickLook 预览" },
            { L"zh-Hans", L"使用 QuickLook 预览" },
            { L"zh-SG", L"使用 QuickLook 预览" },
            { L"zh-TW", L"使用 QuickLook 預覽" },
            { L"zh-HK", L"使用 QuickLook 預覽" },
            { L"zh-MO", L"使用 QuickLook 預覽" },
            { L"zh-Hant", L"使用 QuickLook 預覽" },
            { L"zh", L"使用 QuickLook 预览" },
            { L"ja", L"QuickLook でプレビュー" },
            { L"ko", L"QuickLook로 미리 보기" },
            { L"de", L"Mit QuickLook ansehen" },
            { L"fr", L"Aperçu avec QuickLook" },
            { L"es", L"Vista previa con QuickLook" },
            { L"it", L"Anteprima con QuickLook" },
            { L"pt-BR", L"Visualizar com o QuickLook" },
            { L"pt-PT", L"Pré-visualizar com o QuickLook" },
            { L"pt", L"Visualizar com o QuickLook" },
            { L"ru", L"Просмотр в QuickLook" },
            { L"uk", L"Переглянути в QuickLook" },
            { L"pl", L"Podgląd w QuickLook" },
            { L"nl", L"Bekijken met QuickLook" },
            { L"tr", L"QuickLook ile önizle" },
            { L"vi", L"Xem trước bằng QuickLook" },
            { L"ar", L"معاينة باستخدام QuickLook" },
            { L"hu", L"Előnézet a QuickLookkal" },
            { L"sk", L"Náhľad v QuickLook" },
            { L"id", L"Pratinjau dengan QuickLook" },
            { L"ca", L"Previsualitza amb el QuickLook" },
            { L"nb", L"Forhåndsvis med QuickLook" },
            { L"no", L"Forhåndsvis med QuickLook" },
            { L"mr", L"QuickLook सह पूर्वावलोकन" },
            { L"hi", L"QuickLook से पूर्वावलोकन" },
            { L"he", L"תצוגה מקדימה עם QuickLook" },
            { L"el", L"Προεπισκόπηση με το QuickLook" },
            { L"ro", L"Previzualizare cu QuickLook" },
            { L"en", L"QuickLook" },
        };

        for (const auto& entry : entries)
        {
            if (_wcsicmp(language, entry.language) == 0)
                return entry.title;
        }

        if (!allowParent)
            return nullptr;

        const wchar_t* dash = wcschr(language, L'-');
        if (dash == nullptr || dash == language)
            return nullptr;

        const std::wstring parent(language, dash);
        return TitleForLanguage(parent.c_str(), false);
    }

    const wchar_t* MenuTitle()
    {
        ULONG count = 0;
        ULONG length = 0;
        if (GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, &count, nullptr, &length) && length > 1)
        {
            std::wstring languages(length, L'\0');
            if (GetUserPreferredUILanguages(MUI_LANGUAGE_NAME, &count, &languages[0], &length))
            {
                for (const wchar_t* language = languages.c_str(); *language != L'\0'; language += wcslen(language) + 1)
                {
                    if (const wchar_t* title = TitleForLanguage(language, true))
                        return title;
                }
            }
        }

        wchar_t locale[LOCALE_NAME_MAX_LENGTH] = {};
        if (LCIDToLocaleName(GetUserDefaultUILanguage(), locale, ARRAYSIZE(locale), 0) > 0)
        {
            if (const wchar_t* title = TitleForLanguage(locale, true))
                return title;
        }

        return L"QuickLook";
    }
}

ExplorerCommand::ExplorerCommand()
    : m_refs(1)
{
    InterlockedIncrement(&g_moduleRefs);
}

ExplorerCommand::~ExplorerCommand()
{
    InterlockedDecrement(&g_moduleRefs);
}

IFACEMETHODIMP ExplorerCommand::QueryInterface(REFIID riid, void** ppv)
{
    if (ppv == nullptr)
        return E_POINTER;

    *ppv = nullptr;
    if (riid == IID_IUnknown || riid == IID_IExplorerCommand)
        *ppv = static_cast<IExplorerCommand*>(this);
    else if (riid == IID_IObjectWithSite)
        *ppv = static_cast<IObjectWithSite*>(this);
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
}

IFACEMETHODIMP_(ULONG) ExplorerCommand::AddRef()
{
    return static_cast<ULONG>(InterlockedIncrement(&m_refs));
}

IFACEMETHODIMP_(ULONG) ExplorerCommand::Release()
{
    const long refs = InterlockedDecrement(&m_refs);
    if (refs == 0)
        delete this;
    return static_cast<ULONG>(refs);
}

IFACEMETHODIMP ExplorerCommand::GetTitle(IShellItemArray*, PWSTR* title)
{
    if (title == nullptr)
        return E_POINTER;

    return SHStrDupW(MenuTitle(), title);
}

IFACEMETHODIMP ExplorerCommand::GetIcon(IShellItemArray*, PWSTR* icon)
{
    if (icon == nullptr)
        return E_POINTER;

    std::wstring resource = ModuleFileName();
    resource += L",-";
    resource += std::to_wstring(IDI_APP_ICON);
    return SHStrDupW(resource.c_str(), icon);
}

IFACEMETHODIMP ExplorerCommand::GetToolTip(IShellItemArray*, PWSTR* tip)
{
    if (tip != nullptr)
        *tip = nullptr;
    return E_NOTIMPL;
}

IFACEMETHODIMP ExplorerCommand::GetCanonicalName(GUID* name)
{
    if (name == nullptr)
        return E_POINTER;

    *name = CLSID_QuickLookCommand;
    return S_OK;
}

IFACEMETHODIMP ExplorerCommand::GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state)
{
    if (state == nullptr)
        return E_POINTER;

    std::wstring path;
    if (TryGetFirstFileSystemPath(items, path) || TryGetFolderFromSite(m_site.Get(), path))
        *state = ECS_ENABLED;
    else if (items == nullptr)
        *state = ECS_ENABLED;
    else
    {
        DWORD count = 0;
        if (FAILED(items->GetCount(&count)) || count == 0)
            *state = ECS_ENABLED;
        else
            *state = ECS_HIDDEN;
    }

    return S_OK;
}

IFACEMETHODIMP ExplorerCommand::Invoke(IShellItemArray* items, IBindCtx*)
{
    std::wstring path;
    if (!TryGetFirstFileSystemPath(items, path) && !TryGetFolderFromSite(m_site.Get(), path))
        return E_FAIL;

    return LaunchQuickLook(path);
}

IFACEMETHODIMP ExplorerCommand::GetFlags(EXPCMDFLAGS* flags)
{
    if (flags == nullptr)
        return E_POINTER;

    *flags = ECF_DEFAULT;
    return S_OK;
}

IFACEMETHODIMP ExplorerCommand::EnumSubCommands(IEnumExplorerCommand** commands)
{
    if (commands != nullptr)
        *commands = nullptr;
    return E_NOTIMPL;
}

IFACEMETHODIMP ExplorerCommand::SetSite(IUnknown* site)
{
    m_site = site;
    return S_OK;
}

IFACEMETHODIMP ExplorerCommand::GetSite(REFIID riid, void** ppv)
{
    if (ppv == nullptr)
        return E_POINTER;
    if (m_site == nullptr)
        return E_FAIL;
    return m_site->QueryInterface(riid, ppv);
}

ClassFactory::ClassFactory()
    : m_refs(1)
{
    InterlockedIncrement(&g_moduleRefs);
}

ClassFactory::~ClassFactory()
{
    InterlockedDecrement(&g_moduleRefs);
}

IFACEMETHODIMP ClassFactory::QueryInterface(REFIID riid, void** ppv)
{
    if (ppv == nullptr)
        return E_POINTER;

    *ppv = nullptr;
    if (riid == IID_IUnknown || riid == IID_IClassFactory)
        *ppv = static_cast<IClassFactory*>(this);
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
}

IFACEMETHODIMP_(ULONG) ClassFactory::AddRef()
{
    return static_cast<ULONG>(InterlockedIncrement(&m_refs));
}

IFACEMETHODIMP_(ULONG) ClassFactory::Release()
{
    const long refs = InterlockedDecrement(&m_refs);
    if (refs == 0)
        delete this;
    return static_cast<ULONG>(refs);
}

IFACEMETHODIMP ClassFactory::CreateInstance(IUnknown* outer, REFIID riid, void** ppv)
{
    if (ppv == nullptr)
        return E_POINTER;
    *ppv = nullptr;
    if (outer != nullptr)
        return CLASS_E_NOAGGREGATION;

    auto* command = new (std::nothrow) ExplorerCommand();
    if (command == nullptr)
        return E_OUTOFMEMORY;

    const HRESULT hr = command->QueryInterface(riid, ppv);
    command->Release();
    return hr;
}

IFACEMETHODIMP ClassFactory::LockServer(BOOL lock)
{
    if (lock)
        InterlockedIncrement(&g_moduleLocks);
    else
        InterlockedDecrement(&g_moduleLocks);
    return S_OK;
}
