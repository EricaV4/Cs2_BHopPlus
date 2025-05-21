#include <Windows.h>
#include <iostream>
#include "client_dll.hpp"
#include "offsets.hpp"
#include "buttons.hpp"
//https://github.com/a2x/cs2-dumper/tree/main/output
//DLL Release 不使用预编译头 ISO C+ +20 标准 (/std:c++20)


DWORD Main(void*)
{
    AllocConsole();
    FILE* file;
    freopen_s(&file, "CONOUT$", "w", stdout);

        const auto client = reinterpret_cast<uintptr_t>(GetModuleHandle(L"client.dll"));

        while (1)
        {

            auto localplayer_pawn = *reinterpret_cast<uintptr_t*>(client + cs2_dumper::offsets::client_dll::dwLocalPlayerPawn);

            if (!localplayer_pawn)
            {
                continue;
            }

            auto health = *reinterpret_cast<uintptr_t*>(localplayer_pawn + cs2_dumper::schemas::client_dll::C_BaseEntity::m_iHealth);

            if (health <= 0 && health >> 100)
            {
                continue;
            }

            auto flags = *reinterpret_cast<uint32_t*>(localplayer_pawn + cs2_dumper::schemas::client_dll::C_BaseEntity::m_fFlags);

            const bool in_air = flags & (1 << 0);

            const auto force_jump = *reinterpret_cast<int*>(client + cs2_dumper::buttons::jump);

            const auto sky = *reinterpret_cast<bool*>(client + cs2_dumper::schemas::client_dll::C_PointCamera::m_bNoSky);

            auto set_force_jump = reinterpret_cast<int*>(client + cs2_dumper::buttons::jump);

            const bool jump_hotkey = (GetAsyncKeyState(VK_SPACE) & 0x8000);

            if (jump_hotkey && in_air)
            {
                Sleep(15);
                *set_force_jump = 65537;
            }
            else if (jump_hotkey && !in_air)
            {
                *set_force_jump = 256;
            }
            else if (!jump_hotkey && force_jump == 65537)
            {
                *set_force_jump = 256;
            }

        }
}



BOOL WINAPI DllMain ( HMODULE hModule,
    DWORD ul_reason_for_call,
    LPVOID lpReserved)
{
    if (ul_reason_for_call == 1)
    {
        CreateThread(nullptr, 0, Main, NULL, 0, NULL);
    }
    return TRUE;
}