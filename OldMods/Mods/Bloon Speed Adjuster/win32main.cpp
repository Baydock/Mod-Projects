#define WIN32_LEAN_AND_MEAN
#include "windows.h"

// Handles messages
LRESULT CALLBACK ModProc(HWND hwnd, UINT Message, WPARAM wParam, LPARAM lParam) {
	switch (Message) {
	// Handles general input like buttons
	case WM_COMMAND: {
		break;
	}
	// Unprocessed messages go to the default handler
	default: return DefWindowProc(hwnd, Message, wParam, lParam);
	}
	return 0;
}

// Create Mod Layout
void ModSetup(HWND modContainer, HINSTANCE hInstance) {
	HWND button = CreateWindow(L"BUTTON", L"hello",
		WS_VISIBLE | WS_CHILD,
		10, 10, 100, 100,
		modContainer, NULL, hInstance, NULL);
}
