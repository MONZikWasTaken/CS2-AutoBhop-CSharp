using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Security.Principal;

namespace CS2AutoBhop
{
    public class ConsoleCS2AutoBhop
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const uint INPUT_KEYBOARD = 1;

        const uint VK_F1 = 0x70;
        const uint VK_F2 = 0x71;
        const uint VK_F3 = 0x72;
        const uint VK_F4 = 0x73;
        const uint VK_F5 = 0x74;
        const uint VK_F6 = 0x75;
        const uint VK_F7 = 0x76;
        const uint VK_F8 = 0x77;
        const uint VK_F9 = 0x78;
        const uint VK_F10 = 0x79;
        const uint VK_F11 = 0x7A;
        const uint VK_F12 = 0x7B;
        const uint VK_SPACE = 0x20;
        const uint VK_ENTER = 0x0D;
        const uint VK_INSERT = 0x2D;
        const uint VK_LSHIFT = 0xA0;
        const uint VK_RSHIFT = 0xA1;
        const uint VK_LCONTROL = 0xA2;
        const uint VK_RCONTROL = 0xA3;
        const uint VK_LMENU = 0xA4;
        const uint VK_RMENU = 0xA5;

        const uint VK_A = 0x41;
        const uint VK_B = 0x42;
        const uint VK_C = 0x43;
        const uint VK_D = 0x44;
        const uint VK_E = 0x45;
        const uint VK_F = 0x46;
        const uint VK_G = 0x47;
        const uint VK_H = 0x48;
        const uint VK_I = 0x49;
        const uint VK_J = 0x4A;
        const uint VK_K = 0x4B;
        const uint VK_L = 0x4C;
        const uint VK_M = 0x4D;
        const uint VK_N = 0x4E;
        const uint VK_O = 0x4F;
        const uint VK_P = 0x50;
        const uint VK_Q = 0x51;
        const uint VK_R = 0x52;
        const uint VK_S = 0x53;
        const uint VK_T = 0x54;
        const uint VK_U = 0x55;
        const uint VK_V = 0x56;
        const uint VK_W = 0x57;
        const uint VK_X = 0x58;
        const uint VK_Y = 0x59;
        const uint VK_Z = 0x5A;

        const uint VK_0 = 0x30;
        const uint VK_1 = 0x31;
        const uint VK_2 = 0x32;
        const uint VK_3 = 0x33;
        const uint VK_4 = 0x34;
        const uint VK_5 = 0x35;
        const uint VK_6 = 0x36;
        const uint VK_7 = 0x37;
        const uint VK_8 = 0x38;
        const uint VK_9 = 0x39;

        const uint VK_LBUTTON = 0x01;
        const uint VK_RBUTTON = 0x02;
        const uint VK_MBUTTON = 0x04;
        const uint VK_XBUTTON1 = 0x05;
        const uint VK_XBUTTON2 = 0x06;
        const byte KEYEVENTF_KEYUP = 0x02;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint INPUT_MOUSE = 0;
        const int WHEEL_DELTA = 120;

        public class Config
        {
            public bool BhopEnabled { get; set; } = true;

            public string BhopToggleKey { get; set; } = "F2";
            public string JumpActivationKey { get; set; } = "Space";
            public string RadioKey { get; set; } = "6";
        }

        private Config config = new();
        private System.Threading.Timer? bhopTimer;
        private System.Threading.Timer? monitorTimer;
        private bool isJumping = false;
        private IntPtr cs2Handle = IntPtr.Zero;
        private bool lastBhopToggleState = false;
        private Dictionary<string, uint> keyMap = new Dictionary<string, uint>();
        private readonly object logLock = new();
        private int logCount = 0;

        public void Run()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.InputEncoding = System.Text.Encoding.UTF8;
            }
            catch
            {
                Console.OutputEncoding = System.Text.Encoding.Default;
                Console.InputEncoding = System.Text.Encoding.Default;
            }
            Console.CursorVisible = false;

            InitializeKeyMap();
            LoadConfig();

            ShowInitialDisplay();

            LogMessage("[INFO] CS2 AutoBhop Console запущен!");
            LogMessage("[INFO] Поиск процесса CS2...");
            LogMessage("[INFO] Создание конфигов CS2...");

            CreateCS2Configs();
            StartMonitoring();

            while (true)
            {
                CheckHotkeys();
                CheckBhop();
                Thread.Sleep(1);
            }
        }

        private void CheckHotkeys()
        {
            uint bhopToggleVK = GetVirtualKeyCode(config.BhopToggleKey);
            if (bhopToggleVK != 0)
            {
                bool bhopTogglePressed = (GetAsyncKeyState((int)bhopToggleVK) & 0x8000) != 0;
                if (bhopTogglePressed && !lastBhopToggleState)
                {
                    config.BhopEnabled = !config.BhopEnabled;
                    SaveConfig();
                    LogMessage($"[BHOP] Bhop: {(config.BhopEnabled ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН")}");
                    UpdateStatusInPlace();
                }
                lastBhopToggleState = bhopTogglePressed;
            }

            bool insertPressed = (GetAsyncKeyState((int)VK_INSERT) & 0x8000) != 0;
            if (insertPressed)
            {
                ShowConfigMenu();
            }
        }

        private void ShowInitialDisplay()
        {
            Console.Clear();
            ShowHeader();
        }

        private void ShowHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║             CS2 AutoBhop             ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> СТАТУС:");
            Console.ResetColor();

            ShowStatus();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> УПРАВЛЕНИЕ:");
            Console.ResetColor();
            Console.WriteLine($"  Insert - настройки");
            Console.WriteLine($"  {config.BhopToggleKey}     - переключить Bhop");
            Console.WriteLine($"  {config.JumpActivationKey}  - (в игре) активировать бхоп");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(">> ЛОГИ:");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("════════════════════════════════════════");
            Console.ResetColor();
        }

        private void ShowStatus()
        {
            Console.Write("  [BHOP] Auto Bhop:  ");
            Console.ForegroundColor = config.BhopEnabled ? ConsoleColor.Green : ConsoleColor.Red;
            string bhopText = config.BhopEnabled ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН";
            Console.Write(bhopText);
            Console.Write(new string(' ', Math.Max(0, 50 - bhopText.Length)));
            Console.WriteLine();
            Console.ResetColor();

            Console.Write("  [GAME] CS2 Process: ");
            Console.ForegroundColor = cs2Handle != IntPtr.Zero ? ConsoleColor.Green : ConsoleColor.Red;
            string cs2Text = cs2Handle != IntPtr.Zero ? "НАЙДЕН" : "НЕ НАЙДЕН";
            Console.Write(cs2Text);
            Console.Write(new string(' ', Math.Max(0, 50 - cs2Text.Length)));
            Console.WriteLine();
            Console.ResetColor();
        }

        private void UpdateStatusInPlace()
        {
            lock (logLock)
            {
                try
                {
                    int currentTop = Console.CursorTop;
                    int currentLeft = Console.CursorLeft;

                    Console.SetCursorPosition(0, 5);
                    ShowStatus();

                    Console.SetCursorPosition(currentLeft, currentTop);
                }
                catch
                {

                }
            }
        }

        private void CheckBhop()
        {
            uint jumpActivationVK = GetVirtualKeyCode(config.JumpActivationKey);
            if (jumpActivationVK != 0)
            {
                bool jumpPressed = (GetAsyncKeyState((int)jumpActivationVK) & 0x8000) != 0;

                if (jumpPressed && !isJumping && config.BhopEnabled)
                {
                    HandleJumpPress();
                }
                else if (!jumpPressed && isJumping)
                {
                    HandleJumpRelease();
                }
            }
        }

        private void HandleJumpPress()
        {
            isJumping = true;
            LogMessage("[JUMP] Прыжок начат");

            bhopTimer = new System.Threading.Timer(BhopTick, null, 0, 1);
        }

        private void HandleJumpRelease()
        {
            isJumping = false;
            LogMessage("[JUMP] Прыжок завершен");

            bhopTimer?.Dispose();
        }

        private void BhopTick(object? state)
        {
            if (!isJumping || !config.BhopEnabled) return;

            uint radioKeyVK = GetVirtualKeyCode(config.RadioKey);
            if (radioKeyVK != 0)
            {
                SendKey(radioKeyVK);
            }
        }

        private void SendKey(uint keyCode)
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)keyCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            };

            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = (ushort)keyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void InitializeKeyMap()
        {

            keyMap["F1"] = VK_F1;
            keyMap["F2"] = VK_F2;
            keyMap["F3"] = VK_F3;
            keyMap["F4"] = VK_F4;
            keyMap["F5"] = VK_F5;
            keyMap["F6"] = VK_F6;
            keyMap["F7"] = VK_F7;
            keyMap["F8"] = VK_F8;
            keyMap["F9"] = VK_F9;
            keyMap["F10"] = VK_F10;
            keyMap["F11"] = VK_F11;
            keyMap["F12"] = VK_F12;

            keyMap["Space"] = VK_SPACE;
            keyMap["Enter"] = VK_ENTER;
            keyMap["Insert"] = VK_INSERT;
            keyMap["LShift"] = VK_LSHIFT;
            keyMap["RShift"] = VK_RSHIFT;
            keyMap["LControl"] = VK_LCONTROL;
            keyMap["RControl"] = VK_RCONTROL;
            keyMap["LAlt"] = VK_LMENU;
            keyMap["RAlt"] = VK_RMENU;

            keyMap["A"] = VK_A; keyMap["B"] = VK_B; keyMap["C"] = VK_C; keyMap["D"] = VK_D;
            keyMap["E"] = VK_E; keyMap["F"] = VK_F; keyMap["G"] = VK_G; keyMap["H"] = VK_H;
            keyMap["I"] = VK_I; keyMap["J"] = VK_J; keyMap["K"] = VK_K; keyMap["L"] = VK_L;
            keyMap["M"] = VK_M; keyMap["N"] = VK_N; keyMap["O"] = VK_O; keyMap["P"] = VK_P;
            keyMap["Q"] = VK_Q; keyMap["R"] = VK_R; keyMap["S"] = VK_S; keyMap["T"] = VK_T;
            keyMap["U"] = VK_U; keyMap["V"] = VK_V; keyMap["W"] = VK_W; keyMap["X"] = VK_X;
            keyMap["Y"] = VK_Y; keyMap["Z"] = VK_Z;

            keyMap["0"] = VK_0; keyMap["1"] = VK_1; keyMap["2"] = VK_2; keyMap["3"] = VK_3;
            keyMap["4"] = VK_4; keyMap["5"] = VK_5; keyMap["6"] = VK_6; keyMap["7"] = VK_7;
            keyMap["8"] = VK_8; keyMap["9"] = VK_9;

            keyMap["Mouse1"] = VK_LBUTTON;
            keyMap["Mouse2"] = VK_RBUTTON;
            keyMap["Mouse3"] = VK_MBUTTON;
            keyMap["Mouse4"] = VK_XBUTTON1;
            keyMap["Mouse5"] = VK_XBUTTON2;
        }

        private uint GetVirtualKeyCode(string keyName)
        {
            string normalizedKey = NormalizeKeyName(keyName);
            if (keyMap.ContainsKey(normalizedKey))
            {
                return keyMap[normalizedKey];
            }
            return 0;
        }

        private string NormalizeKeyName(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return keyName;

            string normalized = keyName.Trim().ToUpper();

            switch (normalized)
            {
                case "SPACE":
                case " ":
                    return "Space";
                case "ENTER":
                case "RETURN":
                    return "Enter";
                case "INSERT":
                case "INS":
                    return "Insert";
                case "LSHIFT":
                case "LEFT SHIFT":
                    return "LShift";
                case "RSHIFT":
                case "RIGHT SHIFT":
                    return "RShift";
                case "LCONTROL":
                case "LCTRL":
                case "LEFT CONTROL":
                    return "LControl";
                case "RCONTROL":
                case "RCTRL":
                case "RIGHT CONTROL":
                    return "RControl";
                case "LALT":
                case "LEFT ALT":
                    return "LAlt";
                case "RALT":
                case "RIGHT ALT":
                    return "RAlt";
                case "MOUSE1":
                case "LEFT MOUSE":
                case "LEFT CLICK":
                    return "Mouse1";
                case "MOUSE2":
                case "RIGHT MOUSE":
                case "RIGHT CLICK":
                    return "Mouse2";
                case "MOUSE3":
                case "MIDDLE MOUSE":
                case "MIDDLE CLICK":
                    return "Mouse3";
                case "MOUSE4":
                case "X1":
                case "X1 MOUSE":
                    return "Mouse4";
                case "MOUSE5":
                case "X2":
                case "X2 MOUSE":
                    return "Mouse5";
                default:

                    if (normalized.StartsWith("F") || (normalized.Length == 1 && (char.IsLetter(normalized[0]) || char.IsDigit(normalized[0]))))
                    {
                        return normalized;
                    }
                    return keyName;
            }
        }

        private void ShowConfigMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║          НАСТРОЙКИ КОНФИГОВ          ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> ПРОГРАММНЫЕ ХОТКЕИ:");
            Console.ResetColor();
            Console.WriteLine($"  1. Переключение Bhop: {config.BhopToggleKey}");
            Console.WriteLine($"  2. Активация прыжка: {config.JumpActivationKey}");
            Console.WriteLine($"  3. Кнопка прыжка в игре: {config.RadioKey}");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> ДРУГИЕ:");
            Console.ResetColor();
            Console.WriteLine("  4. Пересоздать конфиги игры");
            Console.WriteLine("  5. Сбросить все настройки к дефолтным");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ВАЖНО] В МЕНЮ НАСТРОЕК BHOP ОТКЛЮЧЕН! Нажмите '0' для выхода");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[HELP] Если не работает: exec autoexec в консоли игры");
            Console.WriteLine("[HELP] Или добавь -exec autoexec в параметры запуска Steam");
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("  0. Вернуться назад");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Введите номер настройки и нажмите Enter: ");
            Console.ResetColor();

            string? input = Console.ReadLine();

            switch (input?.Trim())
            {
                case "1":
                    config.BhopToggleKey = ChangeHotkey("Переключение Bhop", config.BhopToggleKey);
                    break;
                case "2":
                    config.JumpActivationKey = ChangeHotkey("Активация прыжка", config.JumpActivationKey);
                    break;
                case "3":
                    config.RadioKey = ChangeHotkey("Кнопка прыжка в игре", config.RadioKey);
                    UpdateGameConfigs();
                    break;
                case "4":
                    CreateCS2Configs();
                    Thread.Sleep(2000);
                    break;
                case "5":
                    ResetToDefaults();
                    break;
                case "0":
                    ShowInitialDisplay();
                    return;
                default:
                    Console.WriteLine("Неверный выбор!");
                    Thread.Sleep(1000);
                    break;
            }

            SaveConfig();
            ShowConfigMenu();
        }

        private string ChangeHotkey(string description, string currentHotkey)
        {
            Console.WriteLine($"Изменение: {description}");
            Console.WriteLine("Доступные клавиши:");
            Console.WriteLine("F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12");
            Console.WriteLine("Space, Enter, Insert, LShift, RShift, LControl, RControl, LAlt, RAlt");
            Console.WriteLine("Mouse1, Mouse2, Mouse3, Mouse4, Mouse5");
            Console.WriteLine("+ любые буквы/цифры (A-Z, 0-9)");
            Console.Write("Введите клавишу: ");

            string? input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                string normalizedKey = NormalizeKeyName(input);
                if (keyMap.ContainsKey(normalizedKey))
                {
                    Console.WriteLine($"Клавиша изменена на: {normalizedKey}");
                    Thread.Sleep(1500);
                    return normalizedKey;
                }
            }

            Console.WriteLine("Неверная клавиша!");
            Thread.Sleep(1500);
            return currentHotkey;
        }

        private void ResetToDefaults()
        {
            Console.WriteLine("Сбросить все настройки к дефолтным значениям?");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("ЭТО СБРОСИТ:");
            Console.WriteLine("• Программные хоткеи (F2, Space)");
            Console.WriteLine("• Кнопку прыжка в игре (6)");
            Console.WriteLine("• Режим Bhop (включен)");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Введите 'yes' для подтверждения: ");

            string? confirmation = Console.ReadLine()?.Trim().ToLower();
            if (confirmation == "yes")
            {
                config.BhopEnabled = true;
                config.BhopToggleKey = "F2";
                config.JumpActivationKey = "Space";
                config.RadioKey = "6";

                SaveConfig();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[CONFIG] Все настройки сброшены к дефолтным!");
                Console.ResetColor();

                Console.WriteLine("[INFO] Обновление игровых конфигов...");
                UpdateGameConfigs();

                Console.WriteLine("[INFO] Сброс завершен!");
            }
            else
            {
                Console.WriteLine("Сброс отменен.");
            }
            Thread.Sleep(2000);
        }

        private void UpdateGameConfigs()
        {
            try
            {
                string? cs2Path = FindCS2InstallPath();
                if (cs2Path == null)
                {
                    Console.WriteLine("[ERROR] CS2 не найден в реестре, конфиги не обновлены");
                    Thread.Sleep(2000);
                    return;
                }

                string cfgPath = Path.Combine(cs2Path, "game", "csgo", "cfg");

                if (!Directory.Exists(cfgPath))
                {
                    Console.WriteLine($"[ERROR] Папка cfg не найдена: {cfgPath}");
                    Thread.Sleep(2000);
                    return;
                }

                string radioKey = config.RadioKey.ToLower();
                string autoexecPath = Path.Combine(cfgPath, "autoexec.cfg");
                string autoexecContent = $@"alias roger ""slot1;radio""
alias negative ""slot2;radio""
alias cheer ""slot3;radio""
alias holdpos ""slot4;radio""
alias followme ""slot5;radio""

alias thanks ""jump 1 0 0;jump -999 0 0;radio""
bind ""{radioKey}"" ""radio;jump 1 0 0""

echo ""CS2 AutoBhop configs loaded!""";

                File.WriteAllText(autoexecPath, autoexecContent);
                Console.WriteLine("[CONFIG] Конфиг autoexec.cfg обновлен!");

                if (IsCS2Running())
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[WARNING] ВНИМАНИЕ: CS2 запущен!");
                    Console.WriteLine("[INFO] ПЕРЕЗАПУСТИТЕ ИГРУ для применения изменений!");
                    Console.WriteLine("[HELP] Или выполните в консоли игры: exec autoexec");
                    Console.WriteLine("[HELP] Альтернатива: добавьте -exec autoexec в параметры запуска Steam");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("[INFO] CS2 не запущен - изменения будут применены при запуске игры");
                }

                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Ошибка обновления конфигов: {ex.Message}");
                Thread.Sleep(2000);
            }
        }

        private void SendMouseWheel(int delta)
        {
            try
            {
                INPUT[] inputs = new INPUT[1];
                inputs[0].type = INPUT_MOUSE;
                inputs[0].u.mi.mouseData = (uint)delta;
                inputs[0].u.mi.dwFlags = MOUSEEVENTF_WHEEL;

                SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка колеса: {ex.Message}");
            }
        }

        private void StartMonitoring()
        {
            monitorTimer = new System.Threading.Timer(_ =>
            {
                if (cs2Handle == IntPtr.Zero)
                {
                    cs2Handle = FindCS2Window();
                    if (cs2Handle != IntPtr.Zero)
                    {
                        LogMessage("[GAME] CS2 процесс найден!");
                        UpdateStatusInPlace();
                    }
                }
            }, null, 1000, 5000);
        }

        private IntPtr FindCS2Window()
        {
            IntPtr cs2WindowHandle = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                try
                {
                    var process = Process.GetProcessById((int)processId);
                    if (process.ProcessName.ToLower() == "cs2" || process.ProcessName.ToLower() == "csgo")
                    {
                        cs2WindowHandle = hWnd;
                        return false;
                    }
                }
                catch { }

                return true;
            }, IntPtr.Zero);

            return cs2WindowHandle;
        }

        private void CreateCS2Configs()
        {
            try
            {
                string? cs2Path = FindCS2InstallPath();
                if (cs2Path == null)
                {
                    LogMessage("[ERROR] CS2 не найден в реестре, конфиги не созданы");
                    return;
                }

                string cfgPath = Path.Combine(cs2Path, "game", "csgo", "cfg");

                if (!Directory.Exists(cfgPath))
                {
                    LogMessage($"[ERROR] Папка cfg не найдена: {cfgPath}");
                    return;
                }

                bool gameWasRunning = IsCS2Running();

                string radioKey = config.RadioKey.ToLower();
                string autoexecContent = $@"alias roger ""slot1;radio""
alias negative ""slot2;radio""
alias cheer ""slot3;radio""
alias holdpos ""slot4;radio""
alias followme ""slot5;radio""

alias thanks ""jump 1 0 0;jump -999 0 0;radio""
bind ""{radioKey}"" ""radio;jump 1 0 0""

echo ""CS2 AutoBhop configs loaded!""";

                int configsCreated = 0;
                int configsSkipped = 0;

                configsCreated += CreateConfigIfNeeded(Path.Combine(cfgPath, "autoexec.cfg"), autoexecContent, ref configsSkipped);

                if (configsCreated > 0)
                {
                    LogMessage($"[CONFIG] Создано конфигов: {configsCreated}");

                    if (gameWasRunning)
                    {
                        LogMessage("[WARNING] CS2 был запущен - ОБЯЗАТЕЛЬНО ПЕРЕЗАПУСТИ ИГРУ!");
                        LogMessage("[WARNING] Конфиги применятся только после ПОЛНОГО ПЕРЕЗАПУСКА игры!");
                        LogMessage("[HELP] Альтернатива: выполни в консоли игры команду: exec autoexec", ConsoleColor.Red);
                        LogMessage("[HELP] Или добавь -exec autoexec в параметры запуска Steam", ConsoleColor.Red);
                    }
                    else
                    {
                        LogMessage("[GAME] CS2 не запущен - можешь запускать игру!");
                    }
                }

                if (configsSkipped > 0)
                {
                    LogMessage($"[INFO] Пропущено конфигов: {configsSkipped} (уже актуальные)");

                    if (configsCreated == 0)
                    {
                        if (gameWasRunning)
                        {
                            LogMessage("[CONFIG] Конфиги актуальны и игра запущена - можешь играть!");
                            LogMessage("[HELP] Если конфиги не работают, выполни в консоли: exec autoexec", ConsoleColor.Red);
                            LogMessage("[HELP] Или добавь -exec autoexec в параметры запуска Steam", ConsoleColor.Red);
                        }
                        else
                        {
                            LogMessage("[CONFIG] Конфиги актуальны - можешь запускать игру!");
                        }
                    }
                }

                if (configsCreated == 0 && configsSkipped == 0)
                {
                    LogMessage("[ERROR] Не удалось создать конфиги");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка создания конфигов: {ex.Message}");
            }
        }

        private int CreateConfigIfNeeded(string configPath, string expectedContent, ref int skipped)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string existingContent = File.ReadAllText(configPath);

                    if (existingContent.Trim() == expectedContent.Trim())
                    {
                        skipped++;
                        return 0;
                    }

                    string fileName = Path.GetFileName(configPath);
                    LogMessage($"[WARNING] Найден конфиг {fileName} с другим содержимым");

                    var result = MessageBox.Show(
                        $"Найден конфиг {fileName} с отличающимся содержимым.\n\n" +
                        "Для работы программы нужно его перезаписать.\n\n" +
                        "Перезаписать конфиг?",
                        "CS2 AutoBhop - Подтверждение",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1
                    );

                    if (result == DialogResult.No)
                    {
                        LogMessage($"[ERROR] Пользователь отказался перезаписывать {fileName}");
                        LogMessage("[ERROR] Программа не может работать без нужных конфигов");

                        MessageBox.Show(
                            "Программа не может работать без правильных конфигов CS2.\n\n" +
                            "Программа будет закрыта.",
                            "CS2 AutoBhop - Выход",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        Environment.Exit(0);
                        return 0;
                    }

                    LogMessage($"[CONFIG] Пользователь разрешил перезаписать {fileName}");
                }

                File.WriteAllText(configPath, expectedContent);
                return 1;
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка создания {Path.GetFileName(configPath)}: {ex.Message}");
                return 0;
            }
        }

        private bool IsCS2Running()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("cs2");
                if (processes.Length > 0)
                {
                    return true;
                }

                processes = Process.GetProcessesByName("csgo");
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private string? FindCS2InstallPath()
        {
            try
            {

                string[] steamPaths = {
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam",
                    @"HKEY_CURRENT_USER\Software\Valve\Steam"
                };

                foreach (string steamPath in steamPaths)
                {
                    string? installPath = Registry.GetValue(steamPath, "InstallPath", null) as string;
                    if (installPath != null && Directory.Exists(installPath))
                    {
                        LogMessage($"[GAME] Steam найден: {installPath}");

                        string cs2Path = Path.Combine(installPath, "steamapps", "common", "Counter-Strike Global Offensive");
                        if (Directory.Exists(cs2Path))
                        {
                            LogMessage($"[GAME] CS2 найден: {cs2Path}");
                            return cs2Path;
                        }

                        string libraryfoldersPath = Path.Combine(installPath, "steamapps", "libraryfolders.vdf");
                        if (File.Exists(libraryfoldersPath))
                        {
                            string? altPath = FindCS2InLibraries(libraryfoldersPath);
                            if (altPath != null) return altPath;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка поиска CS2: {ex.Message}");
                return null;
            }
        }

        private string? FindCS2InLibraries(string libraryFoldersPath)
        {
            try
            {
                string content = File.ReadAllText(libraryFoldersPath);
                string[] lines = content.Split('\n');

                foreach (string line in lines)
                {
                    if (line.Contains("\"path\""))
                    {
                        int startIndex = line.IndexOf("\"", line.IndexOf("\"path\"") + 6) + 1;
                        int endIndex = line.LastIndexOf("\"");
                        if (startIndex < endIndex)
                        {
                            string libraryPath = line.Substring(startIndex, endIndex - startIndex);
                            libraryPath = libraryPath.Replace("\\\\", "\\");

                            string cs2Path = Path.Combine(libraryPath, "steamapps", "common", "Counter-Strike Global Offensive");
                            if (Directory.Exists(cs2Path))
                            {
                                LogMessage($"[GAME] CS2 найден в библиотеке: {cs2Path}");
                                return cs2Path;
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private void LogMessage(string message)
        {
            LogMessage(message, null);
        }

        private void LogMessage(string message, ConsoleColor? forceColor)
        {
            lock (logLock)
            {

                if (Console.CursorTop >= Console.WindowHeight - 2)
                {

                    logCount = 0;
                    ShowInitialDisplay();
                }

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                logCount++;

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{timestamp}] ");
                Console.ResetColor();

                if (forceColor.HasValue)
                {
                    Console.ForegroundColor = forceColor.Value;
                }
                else
                {

                    if (message.StartsWith("[ERROR]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else if (message.StartsWith("[WARNING]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                    else if (message.StartsWith("[INFO]") || message.StartsWith("[CONFIG]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    else if (message.StartsWith("[BHOP]") || message.StartsWith("[FPS]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    }
                    else if (message.StartsWith("[JUMP]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                    }
                    else if (message.StartsWith("[GAME]"))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (message.StartsWith("[HELP]"))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }

                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        private void LoadConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
                    LogMessage("[CONFIG] Конфигурация загружена");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка загрузки конфига: {ex.Message}");
                config = new Config();
            }
        }

        private void SaveConfig()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка сохранения: {ex.Message}");
            }
        }

    }
}