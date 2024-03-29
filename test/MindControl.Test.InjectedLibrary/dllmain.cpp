#include <windows.h>
#include <iostream>

// This code builds a DLL that will be injected into the testing target app.
// All it does is print a message to the standard output when it is loaded.
// The unit tests can then check that the message was printed to confirm that the DLL was loaded.

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        std::cout << "Injected library attached" << std::endl;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
	
    return TRUE;
}

