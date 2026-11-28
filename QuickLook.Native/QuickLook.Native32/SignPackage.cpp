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

#include <windows.h>
#include <wincrypt.h>

// SignerSignEx2 has no SDK header. Layout matches the Appx signing sample.
typedef struct _SIGNER_FILE_INFO
{
    DWORD cbSize;
    LPCWSTR pwszFileName;
    HANDLE hFile;
} SIGNER_FILE_INFO;

typedef struct _SIGNER_SUBJECT_INFO
{
    DWORD cbSize;
    DWORD* pdwIndex;
    DWORD dwSubjectChoice;
    union
    {
        SIGNER_FILE_INFO* pSignerFileInfo;
        void* pSignerBlobInfo;
    };
} SIGNER_SUBJECT_INFO;

typedef struct _SIGNER_CERT_STORE_INFO
{
    DWORD cbSize;
    PCCERT_CONTEXT pSigningCert;
    DWORD dwCertPolicy;
    HCERTSTORE hCertStore;
} SIGNER_CERT_STORE_INFO;

typedef struct _SIGNER_CERT
{
    DWORD cbSize;
    DWORD dwCertChoice;
    union
    {
        LPCWSTR pwszSpcFile;
        SIGNER_CERT_STORE_INFO* pCertStoreInfo;
        void* pSpcChainInfo;
    };
    HWND hwnd;
} SIGNER_CERT;

typedef struct _SIGNER_SIGNATURE_INFO
{
    DWORD cbSize;
    ALG_ID algidHash;
    DWORD dwAttrChoice;
    union
    {
        void* pAttrAuthcode;
    };
    void* psAuthenticated;
    void* psUnauthenticated;
} SIGNER_SIGNATURE_INFO;

typedef struct _SIGNER_CONTEXT
{
    DWORD cbSize;
    DWORD cbBlob;
    BYTE* pbBlob;
} SIGNER_CONTEXT;

typedef struct _SIGNER_SIGN_EX2_PARAMS
{
    DWORD dwFlags;
    SIGNER_SUBJECT_INFO* pSubjectInfo;
    SIGNER_CERT* pSigningCert;
    SIGNER_SIGNATURE_INFO* pSignatureInfo;
    void* pProviderInfo;
    DWORD dwTimestampFlags;
    PCSTR pszAlgorithmOid;
    PCWSTR pwszTimestampURL;
    void* pCryptAttrs;
    PVOID pSipData;
    SIGNER_CONTEXT** pSignerContext;
    PVOID pCryptoPolicy;
    PVOID pReserved;
} SIGNER_SIGN_EX2_PARAMS;

typedef struct _APPX_SIP_CLIENT_DATA
{
    SIGNER_SIGN_EX2_PARAMS* pSignerParams;
    IUnknown* pAppxSipState;
} APPX_SIP_CLIENT_DATA;

typedef HRESULT(WINAPI* SignerSignEx2Function)(
    DWORD,
    SIGNER_SUBJECT_INFO*,
    SIGNER_CERT*,
    SIGNER_SIGNATURE_INFO*,
    void*,
    DWORD,
    PCSTR,
    PCWSTR,
    void*,
    PVOID,
    SIGNER_CONTEXT**,
    PVOID,
    PVOID);

extern "C" __declspec(dllexport) HRESULT STDAPICALLTYPE SignAppxPackage(PCCERT_CONTEXT cert, LPCWSTR path)
{
    if (cert == nullptr || path == nullptr)
        return E_INVALIDARG;

    DWORD signerIndex = 0;

    SIGNER_FILE_INFO fileInfo = {};
    fileInfo.cbSize = sizeof(fileInfo);
    fileInfo.pwszFileName = path;

    SIGNER_SUBJECT_INFO subjectInfo = {};
    subjectInfo.cbSize = sizeof(subjectInfo);
    subjectInfo.pdwIndex = &signerIndex;
    subjectInfo.dwSubjectChoice = 0x01;
    subjectInfo.pSignerFileInfo = &fileInfo;

    SIGNER_CERT_STORE_INFO certStoreInfo = {};
    certStoreInfo.cbSize = sizeof(certStoreInfo);
    certStoreInfo.dwCertPolicy = 0x02;
    certStoreInfo.pSigningCert = cert;

    SIGNER_CERT signerCert = {};
    signerCert.cbSize = sizeof(signerCert);
    signerCert.dwCertChoice = 0x02;
    signerCert.pCertStoreInfo = &certStoreInfo;

    SIGNER_SIGNATURE_INFO signatureInfo = {};
    signatureInfo.cbSize = sizeof(signatureInfo);
    signatureInfo.algidHash = CALG_SHA_256;
    signatureInfo.dwAttrChoice = 0;

    SIGNER_SIGN_EX2_PARAMS signerParams = {};
    signerParams.pSubjectInfo = &subjectInfo;
    signerParams.pSigningCert = &signerCert;
    signerParams.pSignatureInfo = &signatureInfo;

    APPX_SIP_CLIENT_DATA sipClientData = {};
    sipClientData.pSignerParams = &signerParams;
    signerParams.pSipData = &sipClientData;

    HMODULE module = LoadLibraryExW(L"mssign32.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (module == nullptr)
        return HRESULT_FROM_WIN32(GetLastError());

    auto sign = reinterpret_cast<SignerSignEx2Function>(GetProcAddress(module, "SignerSignEx2"));
    HRESULT hr = sign == nullptr ? HRESULT_FROM_WIN32(GetLastError()) : sign(
        signerParams.dwFlags,
        signerParams.pSubjectInfo,
        signerParams.pSigningCert,
        signerParams.pSignatureInfo,
        signerParams.pProviderInfo,
        signerParams.dwTimestampFlags,
        signerParams.pszAlgorithmOid,
        signerParams.pwszTimestampURL,
        signerParams.pCryptAttrs,
        signerParams.pSipData,
        signerParams.pSignerContext,
        signerParams.pCryptoPolicy,
        signerParams.pReserved);

    if (sipClientData.pAppxSipState != nullptr)
        sipClientData.pAppxSipState->Release();

    FreeLibrary(module);
    return hr;
}
