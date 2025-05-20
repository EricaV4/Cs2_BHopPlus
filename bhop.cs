using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace Bhop
{
    class Bhop
    {

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private const int VK_SPACE = 0x20;

        private static bool jump = false;

        private static Memory cs2;

        private const string VERSION = "1.0.0";

        static void Main(string[] args)
        {


            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("CS2 BHopPlus V{0} ", VERSION);
            Console.ResetColor();


            CheckForUpdates();

            Console.WriteLine("\n正在获取跳跃偏移");

            if (Offsets.dwForceJump == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("获取dwForceJump偏移失败");
                Console.ResetColor();
                // Wait for user to press enter before exiting.
                Console.WriteLine("按ENTER退出...");
                Console.ReadLine();
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"dwForceJump: 0x{Offsets.dwForceJump:X}");
            Console.ResetColor();

            // Loop until the cs2.exe process is found or user decides to exit.
            while (true)
            {
                Console.WriteLine("正在搜索cs2.exe进程");
                cs2 = new Memory("cs2.exe");
                if (cs2.IsValid)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("发现cs2.exe进程");
                    Console.ResetColor();

                    // Retrieve the base address of the client.dll module.
                    cs2.ClientBase = cs2.GetModuleBaseAddress("client.dll");
                    if (cs2.ClientBase == IntPtr.Zero)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("找不到client.dll模块");
                        Console.ResetColor();
                        // Exit or retry here if needed. For now, we exit after a key press.
                        Console.WriteLine("按ENTER退出...");
                        Console.ReadLine();
                        return;
                    }
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"client.dll base address: 0x{cs2.ClientBase.ToInt64():X}");
                    Console.ResetColor();

                    // If everything is set, break out of the loop.
                    break;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("找不到cs2.exe进程");
                    Console.ResetColor();
                    Console.WriteLine("按空格退出或者按R重新尝试");
                    ConsoleKeyInfo keyInfo = Console.ReadKey(true); // true prevents the key from being shown.
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        return;  // Exit the application.
                    }
                    else if (keyInfo.Key == ConsoleKey.R)
                    {
                        // Optionally, you could add a short delay or clear the screen.
                        Console.Clear();
                        // Re-display header and update messages if desired.
                        Console.WriteLine("正在定位cs2.exe进程");
                    }
                }
            }

            Console.WriteLine("按住空格开始连跳");

            // Infinite loop checking for the SPACE key press.
            while (true)
            {

                 if ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0)
                 {
                    PerformBhop();
                    PerformBhop();
                    if ((GetAsyncKeyState(VK_SPACE) & 0x8000) == 0)
                    {
                        cs2.WriteMemory(cs2.ClientBase + Offsets.dwForceJump, 256);
                        jump = false;
                    }
                }
            }
        }

        // Method implementing bhop logic: toggles jump state by writing memory values with short delays.
        private static void PerformBhop()
        {
            if (!jump)
            {
                if ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0)
                {
                    Thread.Sleep(6);
                    // Write value 65537 at address: client.dll base + dwForceJump.
                    cs2.WriteMemory(cs2.ClientBase + Offsets.dwForceJump, 65537);
                    jump = true;
                    cs2.WriteMemory(cs2.ClientBase + Offsets.dwForceJump, 256);
                    jump = false;
                    cs2.WriteMemory(cs2.ClientBase + Offsets.dwForceJump, 65537);
                    jump = true;
                }
            }
            else
            {
                {
                    if ((GetAsyncKeyState(VK_SPACE) & 0x8000) != 0)
                    {
                        Thread.Sleep(8);
                        // Write value 256 to reset the jump state.
                        cs2.WriteMemory(cs2.ClientBase + Offsets.dwForceJump, 256);
                        jump = false;
                    }
                }
            }
        }

        // Checks for updates using the GitHub API.
        private static void CheckForUpdates()
        {
            try
            {
                Console.WriteLine("检查更新");

                using (var wc = new WebClient())
                {
                    // GitHub requires a User-Agent header.
                    wc.Headers.Add("User-Agent", "CS2-Bhop-Utility");
                    string json = wc.DownloadString("https://api.github.com/repos/EricaV4/Cs2_BHopPlus/tags");

                    // Parse JSON to get the latest version.
                    var tags = JsonSerializer.Deserialize<Tag[]>(json);
                    if (tags != null && tags.Length > 0)
                    {
                        // Assume the first tag is the latest.
                        string latestVersion = tags[0].name;
                        if (latestVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                        {
                            latestVersion = latestVersion.Substring(1);
                        }

                        if (latestVersion != VERSION)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"发现新版本: {VERSION}, 最新版本: {latestVersion}");
                            Console.ResetColor();
                            Process.Start(new ProcessStartInfo
                                {
                                  FileName = "https://github.com/EricaV4/Cs2_BHopPlus/releases/",
                                   UseShellExecute = true
                               }
                            );
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("您使用的是最新版本!");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.WriteLine("在储存库中找不到标签");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Update check failed: " + ex.Message);
                Console.ResetColor();
            }
        }

        // Class mapping JSON response for GitHub tags.
        private class Tag
        {
            public string name { get; set; }
        }
    }

    /// <summary>
    /// Static class Offsets.a
    /// The jump offset is loaded automatically from a remote file.
    /// </summary>
    static class Offsets
    {

        public static int dwForceJump { get; private set; }

        // Static constructor runs once upon first access to the class.
        static Offsets()
        {
            try
            {
                using (var wc = new WebClient())
                {
                    string url = "https://raw.githubusercontent.com/a2x/cs2-dumper/refs/heads/main/output/buttons.hpp";
                    string content = wc.DownloadString(url);
                    Match match = Regex.Match(content, @"constexpr\s+std::ptrdiff_t\s+jump\s*=\s*(0x[0-9A-Fa-f]+);");
                    if (match.Success)
                    {
                         string hexValue = match.Groups[1].Value;
                         dwForceJump = Convert.ToInt32(hexValue, 16);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("无法连接至Github仓库，请使用Watt Toolkit加速Github");
                        Console.ResetColor();
                        Console.WriteLine("使用离线偏移量0x1844E00");
                        string hexValue = "0x1844E00";
                        dwForceJump = Convert.ToInt32(hexValue, 16);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error retrieving offset: " + ex.Message);
                Console.ResetColor();
                dwForceJump = 0;
            }
        }
    }

    /// <summary>
    /// Memory class for process memory operations.
    /// </summary>
    class Memory
    {
        public IntPtr ProcessHandle { get; private set; }
        public IntPtr ClientBase { get; set; }
        private Process process;

        public bool IsValid { get; private set; }

        // Constructor attempts to find the process by name (e.g., "cs2.exe").
        public Memory(string processName)
        {
            Process[] processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(processName));
            if (processes.Length > 0)
            {
                process = processes[0];
                ProcessHandle = process.Handle;
                IsValid = true;
            }
            else
            {
                IsValid = false;
            }
        }

        // Retrieves the base address of the specified module.
        public IntPtr GetModuleBaseAddress(string moduleName)
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return module.BaseAddress;
                }
            }
            return IntPtr.Zero;
        }

        // Generic method to write a value of type T into process memory at a given address.
        public void WriteMemory<T>(IntPtr address, T value)
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] buffer = new byte[size];

            // Convert the value to a byte array.
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(value, ptr, false);
            Marshal.Copy(ptr, buffer, 0, size);
            Marshal.FreeHGlobal(ptr);

            int bytesWritten;
            WriteProcessMemory(ProcessHandle, address, buffer, buffer.Length, out bytesWritten);
        }

        // Import WriteProcessMemory from kernel32.dll to write into another process's memory.
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesWritten);
    }
}