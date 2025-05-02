#include "stdafx.h"

#pragma warning(disable : 4706)

#include "task.h"
#include "internal.h"
#include "list_entry.h"

RTL_STATIC_LIST_HEAD(gwndList);
HWINEVENTHOOK hWinEventHook = 0;

LONG dwListRef;

struct FOCUS_INFO : LIST_ENTRY
{
	HWND hwndChild;
	HWND hwndParent;

	FOCUS_INFO(HWND hwndParent, HWND hwndChild) : hwndParent(hwndParent), hwndChild(hwndChild)
	{
		InsertHeadList(&gwndList, this);
		DbgPrint("%s<%p>[%p / %p]\n", __FUNCTION__, this, hwndParent, hwndChild);
	}

	~FOCUS_INFO()
	{
		RemoveEntryList(this);
		DbgPrint("%s<%p>\n", __FUNCTION__, this);
	}
};

void RefList()
{
	++dwListRef;
}

void ReleaseList()
{
	if (!--dwListRef)
	{
		PLIST_ENTRY entry = gwndList.Flink;

		while (entry != &gwndList)
		{
			FOCUS_INFO* pi = static_cast<FOCUS_INFO*>(entry);

			entry = entry->Flink;

			delete pi;
		}
	}
}

void WINAPI DoneShl()
{
	if (hWinEventHook)
	{
		UnhookWinEvent(hWinEventHook);
	}

	ReleaseList();
}

ATOM ShellWnd::Register()
{
	WNDCLASS wndcls = {
		0, StartWindowProc, 0, 0, (HINSTANCE)&__ImageBase, 0, LoadCursor(0, IDC_ARROW), 0, 0, szwndcls
	};

	return RegisterClass(&wndcls);
}

void ShellWnd::Unregister()
{
	if (!UnregisterClassW(szwndcls, (HINSTANCE)&__ImageBase))
	{
		__debugbreak();
	}
}

BOOL ShellWnd::RedirectParent(_In_ HWND hwnd, _In_ HWND hwndForEmbed, _In_ int cx, _In_ int cy)
{
	RECT rc;
	GetWindowRect(hwnd, &rc);
	MoveWindow(hwndForEmbed, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top, TRUE);

	if (SetParent(hwndForEmbed, hwnd))
	{
		rc.left = 0, rc.top = 0, rc.right = cx, rc.bottom = cy;

		if (AdjustWindowRectEx(&rc, GetWindowLongW(hwndForEmbed, GWL_STYLE), FALSE, GetWindowLongW(hwndForEmbed, GWL_EXSTYLE)))
		{
			_pt.x = rc.left, _pt.y = rc.top, _s.cx = (rc.right -= rc.left) - cx, _s.cy = (rc.bottom -= rc.top) - cy;

			DbgPrint("!! %p:(%d, %d) [%u * %u]\n", hwndForEmbed, rc.left, rc.top, rc.right, rc.bottom);

			ShowWindow(hwndForEmbed, SW_SHOW);
			return PostMessageW(hwnd, WM_MOVE_EXT, rc.right, rc.bottom);
		}
	}
	return FALSE;
}

LRESULT ShellWnd::OnPaint(HWND hwnd)
{
	PAINTSTRUCT ps;
	if (BeginPaint(hwnd, &ps))
	{
		if (!IsRectEmpty(&ps.rcPaint))
		{
			FillRect(ps.hdc, &ps.rcPaint, (HBRUSH)(1 + COLOR_INFOBK));
		}
		EndPaint(hwnd, &ps);
	}
	return 0;
}

void ShellWnd::Cleanup()
{
	if (FOCUS_INFO* pfi = _pfi)
	{
		_pfi = 0;
		delete pfi;
	}
}

HWND ShellWnd::Unparent(HWND hwnd)
{
	if (hwnd)
	{
		_hwnd = 0;
		Cleanup();
		RECT rc;
		GetWindowRect(hwnd, &rc);
		SetParent(hwnd, 0);
		MoveWindow(hwnd, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top, TRUE);
	}
	return hwnd;
}

LRESULT ShellWnd::StartWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	if (uMsg == WM_NCCREATE)
	{
		ShellWnd* This = reinterpret_cast<ShellWnd*>(reinterpret_cast<CREATESTRUCTW*>(lParam)->lpCreateParams);
		SetWindowLongPtrW(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(This));
		SetWindowLongPtrW(hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(StaticWindowProc));

		This->AddRef();
		This->_dwCallCount = 1 << 31;
		return This->WrapperWindowProc(hwnd, uMsg, wParam, lParam);
	}

	return DefWindowProcW(hwnd, uMsg, wParam, lParam);
}

LRESULT ShellWnd::StaticWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	return reinterpret_cast<ShellWnd*>(GetWindowLongPtrW(hwnd, GWLP_USERDATA))->WrapperWindowProc(hwnd, uMsg, wParam, lParam);
}

LRESULT ShellWnd::WrapperWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	_dwCallCount++;
	lParam = reinterpret_cast<ShellWnd*>(GetWindowLongPtrW(hwnd, GWLP_USERDATA))->WindowProc(hwnd, uMsg, wParam, lParam);
	if (!--_dwCallCount)
	{
		Release();
	}
	return lParam;
}

LRESULT ShellWnd::WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	switch (uMsg)
	{
	case WM_WINDOWPOSCHANGED:
		if (_hwnd)
		{
			PostMessageW(hwnd, WM_MOVE_EXT, reinterpret_cast<WINDOWPOS*>(lParam)->cx, reinterpret_cast<WINDOWPOS*>(lParam)->cy);
		}
		return 0;

	case WM_MOVE_EXT:
		MoveWindow(_hwnd, _pt.x, _pt.y, _s.cx + (int)wParam, _s.cy + (int)lParam, TRUE);
		return 0;

	case WM_NCDESTROY:
		Cleanup();
		ReleaseList();
		_bittestandreset(&_dwCallCount, 31);
		break;

	case WM_PARENTNOTIFY:
		if (wParam == WM_NCDESTROY)
		{
			return (LRESULT)Unparent(_hwnd);
		}
		break;

	case WM_ERASEBKGND:
		return TRUE;

	case WM_PAINT:
		return OnPaint(hwnd);

	case WM_NCCREATE:
		RefList();
		if (RedirectParent(hwnd, _hwnd, reinterpret_cast<CREATESTRUCTW*>(lParam)->cx, reinterpret_cast<CREATESTRUCTW*>(lParam)->cy))
		{
			if (_pfi = new FOCUS_INFO(reinterpret_cast<CREATESTRUCTW*>(lParam)->hwndParent, hwnd))
			{
				break;
			}
		}
		return 0;
	}

	return DefWindowProcW(hwnd, uMsg, wParam, lParam);
}

HWND WINAPI CreateShlWnd(_In_ HWND hWndParent, _In_ HWND hwndExternal, _In_ int nWidth, _In_ int nHeight)
{
	if (ShellWnd* p = new ShellWnd(hwndExternal))
	{
		hWndParent = CreateWindowExW(0, ShellWnd::szwndcls, 0,
			WS_CHILD|WS_VISIBLE|WS_CLIPCHILDREN|WS_CLIPSIBLINGS,
			0, 0, nWidth, nHeight, hWndParent, 0, (HINSTANCE)&__ImageBase, p);

		p->Release();

		return hWndParent;
	}

	return 0;
}

HWND GetCrossParent(HWND hwnd, _Out_opt_ LPDWORD lpdwProcessId)
{
	if (ULONG tid = GetWindowThreadProcessId(hwnd, lpdwProcessId))
	{
		do
		{
			if (tid != GetWindowThreadProcessId(hwnd, 0))
			{
				return hwnd;
			}
		} while (hwnd = (HWND)GetWindowLongPtrW(hwnd, GWLP_HWNDPARENT));
	}

	return 0;
}

VOID CALLBACK WinEventProc(
						   HWINEVENTHOOK /*hWinEventHook*/,
						   DWORD event,
						   HWND hwnd,
						   LONG /*idObject*/,
						   LONG /*idChild*/,
						   DWORD /*dwEventThread*/,
						   DWORD /*dwmsEventTime*/
						   )
{
	if (hwnd && event == EVENT_OBJECT_FOCUS)
	{
		ULONG dwProcessId;

		if (hwnd = GetCrossParent(hwnd, &dwProcessId))
		{
			static ULONG s_dwLastActive;

			PLIST_ENTRY entry = &gwndList;

			while ((entry = entry->Flink) != &gwndList)
			{
				if (static_cast<FOCUS_INFO*>(entry)->hwndChild == hwnd)
				{
					BringWindowToTop(GetAncestor(hwnd = static_cast<FOCUS_INFO*>(entry)->hwndParent, GA_ROOT));
					SetFocus(hwnd);
					SendMessageW(hwnd, WM_APP, WM_SETFOCUS, 0);
					do
					{
						SendMessageW(hwnd, WM_NCACTIVATE, TRUE, 0);
					} while (hwnd = (HWND)GetWindowLongPtrW(hwnd, GWLP_HWNDPARENT));

					s_dwLastActive = dwProcessId;
					return;
				}
			}

			if (dwProcessId != GetCurrentProcessId() && dwProcessId != s_dwLastActive)
			{
				GUITHREADINFO gui = { sizeof(gui) };
				if (GetGUIThreadInfo(GetCurrentThreadId(), &gui))
				{
					SendMessageW(gui.hwndActive, WM_NCACTIVATE, FALSE, 0);
				}
			}
		}
	}
}

HRESULT WINAPI InitShl()
{
	dwListRef = 1;

	if (hWinEventHook = SetWinEventHook(EVENT_OBJECT_FOCUS, EVENT_OBJECT_FOCUS,
		0, WinEventProc, 0, 0, WINEVENT_OUTOFCONTEXT| WINEVENT_SKIPOWNPROCESS  ))
	{
		return S_OK;
	}

	return HRESULT_FROM_WIN32(GetLastError());
}
