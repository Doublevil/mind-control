# Injecting a DLL

A common technique in process hacking is to inject a DLL into the target process. This allows you to run custom code within the context of the target process, which can be useful for various purposes, such as modifying the behavior of the application, hooking functions, or even creating a user interface.

## Creating a DLL for injection

To create a DLL for injection, you can write a simple C++ program with an `APIENTRY` function that will be called when the DLL is loaded. Here's a basic example:

```c++
#include <windows.h>
#include <iostream>

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
            // This code will run when the DLL is injected into the target process
            // In this example, we will just show a messagebox, but you can replace this with your own code
            MessageBoxA(NULL, "Injected library attached", "DLL Injection", MB_OK | MB_ICONINFORMATION);
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }
	
    return TRUE;
}
```

Compile this code into a DLL using your preferred C++ compiler. Make sure to set the output type to "Dynamic Link Library" (DLL), and use the appropriate bitness (x86 or x64) that matches the target process you want to inject into. Then, you can add this DLL to your MindControl project.

> [!NOTE]
> When referencing the DLL in your MindControl project, just add it as a content file, and not a reference. Make sure that the DLL is copied to the output directory of your project.

## Injecting the DLL using MindControl

Once you have your DLL ready, you can inject it into the target process using the `ProcessMemory.InjectLibrary` method.

```csharp
// Pay attention to the result of the method call, as there are many reasons why the injection might fail
processMemory.InjectLibrary("path_to_your_dll.dll").ThrowIfFailure();
```

This method will attempt to inject the specified DLL into the target process. If the injection is successful, the code in the `DllMain` function of your DLL will be executed before the method returns. For example, if you used the code provided above, you should see the message "Injected library attached" in a message box when the DLL is injected successfully.