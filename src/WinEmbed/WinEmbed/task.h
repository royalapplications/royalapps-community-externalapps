#pragma once

HWND WINAPI CreateShlWnd(_In_ HWND hWndParent, _In_ HWND hwndExternal, _In_ int nWidth, _In_ int nHeight);

HRESULT WINAPI InitShl();

void WINAPI DoneShl();