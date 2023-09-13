#define _CRT_SECURE_NO_WARNINGS
#pragma warning(disable : 4244)
#pragma warning(disable : 4005)
#pragma warning(disable : 4477)
#pragma warning(disable : 4311)
#pragma warning(disable : 4302)
#pragma warning(disable : 4313)
#pragma warning(disable : 4267)

#include "main.hh"

using namespace std;

bool initialized = false;
bool init(HMODULE h_module)
{
    if (h_module && !initialized)
    {
        initialized = true;
        if (Mono::Initialize())
        {
            const auto assembly_load = reinterpret_cast<UINT_PTR>(GetProcAddress(mono_handle, "mono_assembly_load_from_full"));
            while (true)
            {
                if (assembly_load != assembly_load + 13) ExitProcess(0);
                else Sleep(150);
            }
        }
    }
    return true;
}

bool APIENTRY DllMain(HMODULE hModule, DWORD dwReason, LPVOID lpReserved)
{
    if (dwReason == DLL_THREAD_ATTACH) return init(hModule);
    return true;
}
