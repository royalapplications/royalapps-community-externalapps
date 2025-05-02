#include "stdafx.h"

#include "task.h"
#include "internal.h"

BOOLEAN DllMain(_In_ HMODULE DllHandle, _In_ ULONG Reason, _In_ PVOID /*Reserved*/)
{
	switch (Reason)
	{
	case DLL_PROCESS_ATTACH:
		DisableThreadLibraryCalls(DllHandle);
		return ShellWnd::Register() != 0;

	case DLL_PROCESS_DETACH:
		ShellWnd::Unregister();
		break;
	}

	return TRUE;
}
