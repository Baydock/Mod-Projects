// Generated C++ file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty
// Edited by Baydock for the Bloon Speed Adjuster Mod for BTD6

#define WIN32_LEAN_AND_MEAN
#include "windows.h"
#include "dllmain.h"
#include <vector>

#define MOD_CONTAINER_SETUP (WM_APP + 1)
#define GET_MOD_SETUP (WM_APP + 2)
#define GET_MOD_PROC (WM_APP + 3)

typedef void (*initModFunc)(HWND window, HINSTANCE hInstance);

HINSTANCE hInstance;
HWND hwnd;
HWND modMenu;

POINT xy;
POINT wh;
int mods = 0;

void AddModButton(LPCWSTR modName) {
	CreateWindow(L"BUTTON", modName,
		WS_CHILD | WS_VISIBLE,
		0, mods * 25, wh.x - GetSystemMetrics(SM_CXVSCROLL), 25,
		modMenu, NULL, hInstance, NULL);
	mods++;
}

void ModContainerSetup(HWND modHwnd, LPCWSTR modName) {
	WNDCLASSEX wc;
	wc.hInstance = hInstance;
	wc.lpszClassName = modName;
	wc.lpfnWndProc = (WNDPROC)SendMessage(modHwnd, GET_MOD_PROC, NULL, NULL);
	wc.style = CS_DBLCLKS;
	wc.hIcon = NULL;
	wc.hIconSm = NULL;
	wc.hCursor = LoadCursor(NULL, IDC_ARROW);
	wc.lpszMenuName = NULL;
	wc.cbClsExtra = 0;
	wc.cbWndExtra = 0;
	wc.hbrBackground = (HBRUSH)COLOR_BACKGROUND;
	wc.cbSize = sizeof(WNDCLASSEX);
	if (!RegisterClassEx(&wc))
		return;

	HWND modContainer = CreateWindow(modName, NULL,
		WS_CHILD,
		xy.x, xy.y, wh.x, wh.y,
		hwnd, NULL, hInstance, NULL);
	((initModFunc)SendMessage(modHwnd, GET_MOD_SETUP, NULL, NULL))(modContainer, hInstance);

	AddModButton(modName);
}

// Handles and relays messages
LRESULT CALLBACK DLLWindowProc(HWND hwnd, UINT Message, WPARAM wParam, LPARAM lParam) {
	switch (Message) {
	case MOD_CONTAINER_SETUP: {
		ModContainerSetup((HWND)wParam, (LPCWSTR)lParam);
		break;
	}
	case GET_MOD_SETUP: return (LRESULT)ModSetup;
	case GET_MOD_PROC: return (LRESULT)ModProc;
	case WM_COMMAND: {
		if (HIWORD(wParam) == BN_CLICKED) {
			LPWSTR modName = new wchar_t[256];
			GetWindowText((HWND)lParam, modName, 256);
			HWND modContainer = FindWindowEx(hwnd, NULL, modName, NULL);
			if (modContainer != NULL) {
				ShowWindow(modMenu, SW_HIDE);
				ShowWindow(modContainer, SW_SHOW);
			}
		}
		break;
	}
	case WM_CLOSE: {
		// Don't close mod window because that'd be bad
		break;
	}
	case WM_DESTROY: {
		PostQuitMessage(0);
		break;
	}
	default: return DefWindowProc(hwnd, Message, wParam, lParam);
	}
	return 0;
}

// Start window
WPARAM WINAPI ThreadProc(LPVOID lpParam) {
	hInstance = (HINSTANCE)lpParam;
	LPWSTR modName = new wchar_t[256];
	GetModuleFileName(hInstance, modName, 256);
	WNDCLASSEX wc;
	MSG msg;

	HWND base = FindWindow(L"InjectedDLLBtd6Mod", L"Btd6Mods");
	bool alreadyExists = base != NULL;

	wc.hInstance = hInstance;
	wc.lpszClassName = alreadyExists ? L"AdditionalDLLBtd6Mod" : L"InjectedDLLBtd6Mod";
	wc.lpfnWndProc = DLLWindowProc;
	wc.style = CS_DBLCLKS;
	wc.hIcon = NULL;
	wc.hIconSm = NULL;
	wc.hCursor = LoadCursor(hInstance, IDC_ARROW);
	wc.lpszMenuName = NULL;
	wc.cbClsExtra = 0;
	wc.cbWndExtra = 0;
	wc.hbrBackground = (HBRUSH)COLOR_BACKGROUND;
	wc.cbSize = sizeof(WNDCLASSEX);
	if (!RegisterClassEx(&wc))
		return 0;

	hwnd = CreateWindowEx(WS_EX_DLGMODALFRAME,
		alreadyExists ? L"AdditionalDLLBtd6Mod" : L"InjectedDLLBtd6Mod", L"Btd6Mods",
		WS_POPUP | WS_CAPTION,
		CW_USEDEFAULT, CW_USEDEFAULT, 1000, 750,
		NULL, NULL, hInstance, NULL);

	if (!alreadyExists) {
		RECT clientRect;
		GetClientRect(hwnd, &clientRect);
		xy = { clientRect.left, clientRect.top };
		wh = { clientRect.right - clientRect.left, clientRect.bottom - clientRect.top };

		wc.hInstance = hInstance;
		wc.lpszClassName = L"ModMenu";
		wc.lpfnWndProc = DLLWindowProc;
		wc.style = CS_DBLCLKS;
		wc.hIcon = NULL;
		wc.hIconSm = NULL;
		wc.hCursor = LoadCursor(hInstance, IDC_ARROW);
		wc.lpszMenuName = NULL;
		wc.cbClsExtra = 0;
		wc.cbWndExtra = 0;
		wc.hbrBackground = (HBRUSH)COLOR_BACKGROUND;
		wc.cbSize = sizeof(WNDCLASSEX);
		if (!RegisterClassEx(&wc))
			return 0;

		modMenu = CreateWindow(L"ModMenu", NULL,
			WS_CHILD | WS_VISIBLE | WS_VSCROLL,
			xy.x, xy.y, wh.x, wh.y,
			hwnd, NULL, hInstance, NULL);

		SCROLLINFO si;
		si.fMask = SIF_DISABLENOSCROLL | SIF_PAGE | SIF_POS;
		si.nPage = wh.y;
		si.nPos = 0;
		si.cbSize = sizeof(SCROLLINFO);
		SetScrollInfo(modMenu, SB_VERT, &si, true);

		ModContainerSetup(hwnd, modName);

		ShowWindow(hwnd, SW_SHOWNORMAL);
	}
	else SendMessage(base, MOD_CONTAINER_SETUP, (WPARAM)hwnd, (LPARAM)modName);

	while (GetMessage(&msg, NULL, 0, 0) > 0) {
		TranslateMessage(&msg);
		DispatchMessage(&msg);
	}
	return msg.wParam;
}

// DLL entry point
BOOL APIENTRY DllMain( HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH:
        init_il2cpp();
        CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE) ThreadProc, (LPVOID)hModule, 0, NULL);
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}