#pragma once

struct FOCUS_INFO;

EXTERN_C {
	extern IMAGE_DOS_HEADER __ImageBase;

#ifndef DbgPrint

	DECLSPEC_IMPORT
		ULONG
		__cdecl
		DbgPrint (
		_In_z_ _Printf_format_string_ PCSTR Format,
		...
		);
#endif // !DbgPrint
};

class ShellWnd
{
	enum { WM_MOVE_EXT = WM_SIZE + WM_USER };

	HWND _hwnd = 0;
	FOCUS_INFO* _pfi = 0;
	POINT _pt;
	SIZE _s;
	LONG _dwRef = 1;
	LONG _dwCallCount = NULL;

	BOOL RedirectParent(_In_ HWND hwnd, _In_ HWND hwndForEmbed, _In_ int cx, _In_ int cy);

	LRESULT OnPaint(HWND hwnd);

	void Cleanup();

	HWND Unparent(HWND hwnd);

	static LRESULT StartWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

	static LRESULT StaticWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

	LRESULT WrapperWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

	LRESULT WindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

	~ShellWnd()
	{
		DbgPrint("%s<%p>\n", __FUNCTION__, this);
	}

public:
	
	ShellWnd(HWND hwnd) : _hwnd(hwnd)
	{
		DbgPrint("%s<%p>\n", __FUNCTION__, this);
	}

	void AddRef()
	{
		InterlockedIncrement(&_dwRef);
	}

	void Release()
	{
		if (!InterlockedDecrement(&_dwRef))
		{
			delete this;
		}
	}

	inline static const wchar_t szwndcls[] = L"{30DE1EAA-E536-44b9-948D-2CE9EF8E981D}";

	static ATOM Register();

	static void Unregister();
};
