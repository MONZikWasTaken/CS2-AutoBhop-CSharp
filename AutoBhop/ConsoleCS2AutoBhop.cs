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
            public bool FPSMode { get; set; } = true;
            public string ScrollDirection { get; set; } = "Down";
            public int ScrollDelay { get; set; } = 1;

            public string BhopToggleKey { get; set; } = "F2";
            public string FPSToggleKey { get; set; } = "F3";
            public string JumpActivationKey { get; set; } = "Space";

            public string JumpKey { get; set; } = "mwheeldown";

            public string GameJumpBind { get; set; } = "mwheeldown";
            public string GameFPSLowKey { get; set; } = "F5";
            public string GameFPSHighKey { get; set; } = "F6";
        }

        private Config config = new();
        private System.Threading.Timer? bhopTimer;
        private System.Threading.Timer? monitorTimer;
        private bool isJumping = false;
        private bool fpsOn = false;
        private IntPtr cs2Handle = IntPtr.Zero;
        private bool lastBhopToggleState = false;
        private bool lastFPSToggleState = false;
        private Dictionary<string, uint> keyMap = new Dictionary<string, uint>();
        private readonly object logLock = new();
        private int logCount = 0;
        private string? rtssPath = null;

        public void Run()
        {

            if (!IsRunningAsAdministrator())
            {
                RequestAdministratorRights();
                return;
            }

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

            CheckRTSSSetup();

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

            uint fpsToggleVK = GetVirtualKeyCode(config.FPSToggleKey);
            if (fpsToggleVK != 0)
            {
                bool fpsTogglePressed = (GetAsyncKeyState((int)fpsToggleVK) & 0x8000) != 0;
                if (fpsTogglePressed && !lastFPSToggleState)
                {
                    config.FPSMode = !config.FPSMode;
                    SaveConfig();
                    LogMessage($"[FPS] FPS Control: {(config.FPSMode ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН")}");
                    UpdateStatusInPlace();
                }
                lastFPSToggleState = fpsTogglePressed;
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
            Console.WriteLine($"  {config.FPSToggleKey}     - переключить FPS Control");
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

            Console.Write("  [FPS] FPS Control: ");
            string fpsText;
            ConsoleColor fpsColor;
            if (config.FPSMode && config.BhopEnabled)
            {
                fpsText = "АКТИВЕН";
                fpsColor = ConsoleColor.Green;
            }
            else if (config.FPSMode && !config.BhopEnabled)
            {
                fpsText = "ВКЛЮЧЕН (ждет Bhop)";
                fpsColor = ConsoleColor.Yellow;
            }
            else
            {
                fpsText = "ВЫКЛЮЧЕН";
                fpsColor = ConsoleColor.Red;
            }
            Console.ForegroundColor = fpsColor;
            Console.Write(fpsText);
            Console.Write(new string(' ', Math.Max(0, 50 - fpsText.Length)));
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

            if (config.BhopEnabled && config.FPSMode && !fpsOn)
            {
                uint lowFpsVK = GetVirtualKeyCode(config.GameFPSLowKey);
                if (lowFpsVK != 0)
                {
                    SendKey(lowFpsVK);
                    fpsOn = true;
                    LogMessage($"[FPS] {config.GameFPSLowKey} (низкий FPS для бхопа)");
                }
            }
            else if (config.BhopEnabled && !config.FPSMode)
            {
                uint highFpsVK = GetVirtualKeyCode(config.GameFPSHighKey);
                if (highFpsVK != 0)
                {
                    SendKey(highFpsVK);
                    LogMessage($"[FPS] {config.GameFPSHighKey} (высокий FPS)");
                }
            }

            bhopTimer = new System.Threading.Timer(BhopTick, null, 0, config.ScrollDelay);
        }

        private void HandleJumpRelease()
        {
            isJumping = false;
            LogMessage("[JUMP] Прыжок завершен");

            if (config.BhopEnabled && config.FPSMode && fpsOn)
            {
                uint highFpsVK = GetVirtualKeyCode(config.GameFPSHighKey);
                if (highFpsVK != 0)
                {
                    SendKey(highFpsVK);
                    fpsOn = false;
                    LogMessage($"[FPS] {config.GameFPSHighKey} (высокий FPS после бхопа)");
                }
            }

            bhopTimer?.Dispose();
        }

        private void BhopTick(object? state)
        {
            if (!isJumping || !config.BhopEnabled) return;

            if (config.GameJumpBind == "mwheeldown" || config.GameJumpBind == "mwheelup")
            {

                int delta = config.GameJumpBind == "mwheeldown" ? -WHEEL_DELTA : WHEEL_DELTA;
                SendMouseWheel(delta);
            }
            else
            {

                uint jumpVK = GetVirtualKeyCodeForJump(config.GameJumpBind);
                if (jumpVK != 0)
                {
                    SendKey(jumpVK);
                }
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
                        wScan = (ushort)MapVirtualKey(keyCode, 0),
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
                        wScan = (ushort)MapVirtualKey(keyCode, 0),
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private uint GetVirtualKeyCodeForJump(string jumpKey)
        {

            switch (jumpKey.ToLower())
            {
                case "mouse1": return VK_LBUTTON;
                case "mouse2": return VK_RBUTTON;
                case "mouse3": return VK_MBUTTON;
                case "mouse4": return VK_XBUTTON1;
                case "mouse5": return VK_XBUTTON2;
                case "space": return VK_SPACE;
                default:

                    return GetVirtualKeyCode(jumpKey);
            }
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
            Console.WriteLine($"  2. Переключение FPS Control: {config.FPSToggleKey}");
            Console.WriteLine($"  3. Активация прыжка: {config.JumpActivationKey}");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> ИГРОВЫЕ БИНДЫ:");
            Console.ResetColor();
            Console.WriteLine($"  4. Кнопка для прыжка: {config.GameJumpBind}");
            Console.WriteLine($"  5. Кнопка низкого FPS: {config.GameFPSLowKey}");
            Console.WriteLine($"  6. Кнопка высокого FPS: {config.GameFPSHighKey}");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> НАСТРОЙКИ ПРЫЖКА:");
            Console.ResetColor();
            Console.WriteLine($"  7. Задержка нажатий: {config.ScrollDelay}ms");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(">> ДРУГИЕ:");
            Console.ResetColor();
            Console.WriteLine(" 10. Пересоздать конфиги игры");
            Console.WriteLine(" 11. Сбросить все настройки к дефолтным");
            if (rtssPath == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(" 12. Настроить RTSS (требуется для FPS Control)");
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ВАЖНО] В МЕНЮ НАСТРОЕК BHOP ОТКЛЮЧЕН! Нажмите '0' для выхода");
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARNING] Игровые бинды (4-6) требуют ПЕРЕЗАПУСК CS2!");
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
                    config.FPSToggleKey = ChangeHotkey("Переключение FPS Control", config.FPSToggleKey);
                    break;
                case "3":
                    config.JumpActivationKey = ChangeHotkey("Активация прыжка", config.JumpActivationKey);
                    break;
                case "4":
                    config.GameJumpBind = ChangeGameJumpBind("Кнопка для прыжка", config.GameJumpBind);
                    UpdateGameConfigs();
                    break;
                case "5":
                    config.GameFPSLowKey = ChangeGameHotkey("Кнопка низкого FPS", config.GameFPSLowKey);
                    UpdateGameConfigs();
                    UpdateRTSSHotkeyConfig();
                    RestartRTSS();
                    break;
                case "6":
                    config.GameFPSHighKey = ChangeGameHotkey("Кнопка высокого FPS", config.GameFPSHighKey);
                    UpdateGameConfigs();
                    UpdateRTSSHotkeyConfig();
                    RestartRTSS();
                    break;
                case "7":
                    ChangeScrollDelay();
                    break;
                case "10":
                    CreateCS2Configs();
                    Thread.Sleep(2000);
                    break;
                case "11":
                    ResetToDefaults();
                    break;
                case "12":
                    if (rtssPath == null)
                    {
                        SetupRTSSLater();
                    }
                    else
                    {
                        Console.WriteLine("RTSS уже настроен!");
                        Thread.Sleep(1000);
                    }
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

        private string ChangeGameBind(string description, string currentBind, string[] options)
        {
            Console.WriteLine($"Изменение: {description}");
            Console.WriteLine("Доступные опции:");
            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine($"  {i + 1}. {options[i]}");
            }
            Console.Write("Введите номер: ");

            ConsoleKeyInfo key = Console.ReadKey();
            Console.WriteLine();

            if (char.IsDigit(key.KeyChar))
            {
                int choice = key.KeyChar - '0';
                if (choice > 0 && choice <= options.Length)
                {
                    Console.WriteLine($"Бинд изменен на: {options[choice - 1]}");
                    Thread.Sleep(1500);
                    return options[choice - 1];
                }
                else
                {
                    Console.WriteLine("Неверный номер!");
                }
            }
            else
            {
                Console.WriteLine("Неверный ввод!");
            }
            Thread.Sleep(1500);
            return currentBind;
        }

        private string ChangeGameHotkey(string description, string currentHotkey)
        {
            Console.WriteLine($"Изменение: {description}");
            Console.WriteLine("Доступные клавиши:");
            Console.WriteLine("F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12");
            Console.WriteLine("Space, Enter, Insert, LShift, RShift, LControl, RControl, LAlt, RAlt");
            Console.WriteLine("Mouse1, Mouse2, Mouse3, Mouse4, Mouse5");
            Console.WriteLine("+ любые буквы/цифры (A-Z, 0-9)");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("ВНИМАНИЕ: Убедитесь что клавиша не используется в игре!");
            Console.ResetColor();
            Console.Write("Введите клавишу: ");

            string? input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input))
            {
                string normalizedKey = NormalizeKeyName(input);
                if (keyMap.ContainsKey(normalizedKey))
                {

                    string gameBind = normalizedKey.ToLower();
                    if (normalizedKey == "Space") gameBind = "space";
                    else if (normalizedKey == "Enter") gameBind = "enter";
                    else if (normalizedKey == "LShift") gameBind = "lshift";
                    else if (normalizedKey == "RShift") gameBind = "rshift";
                    else if (normalizedKey == "LControl") gameBind = "lcontrol";
                    else if (normalizedKey == "RControl") gameBind = "rcontrol";
                    else if (normalizedKey == "LAlt") gameBind = "lalt";
                    else if (normalizedKey == "RAlt") gameBind = "ralt";
                    else if (normalizedKey == "Mouse1") gameBind = "mouse1";
                    else if (normalizedKey == "Mouse2") gameBind = "mouse2";
                    else if (normalizedKey == "Mouse3") gameBind = "mouse3";
                    else if (normalizedKey == "Mouse4") gameBind = "mouse4";
                    else if (normalizedKey == "Mouse5") gameBind = "mouse5";

                    Console.WriteLine($"Игровая клавиша изменена на: {gameBind}");
                    Thread.Sleep(1500);
                    return gameBind;
                }
            }

            Console.WriteLine("Неверная клавиша!");
            Thread.Sleep(1500);
            return currentHotkey;
        }

        private string ChangeJumpKey(string description, string currentJumpKey)
        {
            Console.WriteLine($"Изменение: {description}");
            Console.WriteLine("Доступные кнопки для прыжка:");
            Console.WriteLine("mwheeldown, mwheelup - колесико мыши");
            Console.WriteLine("Mouse1, Mouse2, Mouse3, Mouse4, Mouse5 - кнопки мыши");
            Console.WriteLine("Space, F1-F12, A-Z, 0-9 - клавиатура");
            Console.Write("Введите кнопку: ");

            string? input = Console.ReadLine()?.Trim()?.ToLower();
            if (!string.IsNullOrEmpty(input))
            {

                if (input == "mwheeldown" || input == "mwheelup")
                {
                    Console.WriteLine($"Кнопка прыжка изменена на: {input}");

                    config.GameJumpBind = input;
                    UpdateGameConfigs();
                    Thread.Sleep(1500);
                    return input;
                }

                if (input.StartsWith("mouse") && input.Length == 6 && char.IsDigit(input[5]))
                {
                    string jumpKey = char.ToUpper(input[0]) + input.Substring(1);

                    config.GameJumpBind = input.ToLower();
                    UpdateGameConfigs();
                    Console.WriteLine($"Кнопка прыжка изменена на: {jumpKey}");
                    Thread.Sleep(1500);
                    return jumpKey;
                }

                string normalizedKey = NormalizeKeyName(input);
                if (keyMap.ContainsKey(normalizedKey))
                {

                    string gameBind = ConvertToGameBind(normalizedKey);
                    config.GameJumpBind = gameBind;
                    UpdateGameConfigs();
                    Console.WriteLine($"Кнопка прыжка изменена на: {normalizedKey}");
                    Thread.Sleep(1500);
                    return normalizedKey;
                }
            }

            Console.WriteLine("Неверная кнопка!");
            Thread.Sleep(1500);
            return currentJumpKey;
        }

        private string ConvertToGameBind(string normalizedKey)
        {

            switch (normalizedKey)
            {
                case "Space": return "space";
                case "Enter": return "enter";
                case "Insert": return "ins";
                case "LShift": return "shift";
                case "RShift": return "shift";
                case "LControl": return "ctrl";
                case "RControl": return "ctrl";
                case "LAlt": return "alt";
                case "RAlt": return "alt";
                default:

                    return normalizedKey.ToLower();
            }
        }

        private string ChangeGameJumpBind(string description, string currentBind)
        {
            Console.WriteLine($"Изменение: {description}");
            Console.WriteLine("Доступные бинды:");
            Console.WriteLine("mwheeldown, mwheelup, mouse1, mouse2, mouse3, mouse4, mouse5");
            Console.WriteLine("F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12");
            Console.WriteLine("Space, Enter, Insert, LShift, RShift, LControl, RControl, LAlt, RAlt");
            Console.WriteLine("+ любые буквы/цифры (A-Z, 0-9)");
            Console.Write("Введите бинд: ");

            string? input = Console.ReadLine()?.Trim()?.ToLower();
            if (!string.IsNullOrEmpty(input))
            {

                if (input == "mwheeldown" || input == "mwheelup" ||
                    input == "mouse1" || input == "mouse2" || input == "mouse3" ||
                    input == "mouse4" || input == "mouse5")
                {
                    Console.WriteLine($"Игровой бинд изменен на: {input}");
                    Thread.Sleep(1500);
                    return input;
                }

                string normalizedKey = NormalizeKeyName(input);
                if (keyMap.ContainsKey(normalizedKey))
                {
                    string gameBind = normalizedKey.ToLower();

                    if (normalizedKey == "Space") gameBind = "space";
                    else if (normalizedKey == "Enter") gameBind = "enter";
                    else if (normalizedKey == "LShift") gameBind = "lshift";
                    else if (normalizedKey == "RShift") gameBind = "rshift";
                    else if (normalizedKey == "LControl") gameBind = "lcontrol";
                    else if (normalizedKey == "RControl") gameBind = "rcontrol";
                    else if (normalizedKey == "LAlt") gameBind = "lalt";
                    else if (normalizedKey == "RAlt") gameBind = "ralt";
                    else if (normalizedKey == "Mouse1") gameBind = "mouse1";
                    else if (normalizedKey == "Mouse2") gameBind = "mouse2";
                    else if (normalizedKey == "Mouse3") gameBind = "mouse3";
                    else if (normalizedKey == "Mouse4") gameBind = "mouse4";
                    else if (normalizedKey == "Mouse5") gameBind = "mouse5";

                    Console.WriteLine($"Игровой бинд изменен на: {gameBind}");
                    Thread.Sleep(1500);
                    return gameBind;
                }
            }

            Console.WriteLine("Неверный бинд!");
            Thread.Sleep(1500);
            return currentBind;
        }

        private void ChangeScrollDelay()
        {
            Console.Write($"Текущая задержка: {config.ScrollDelay}ms. Введите новое значение (1-100): ");
            string? input = Console.ReadLine()?.Trim();

            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int delay) && delay >= 1 && delay <= 100)
            {
                config.ScrollDelay = delay;
                Console.WriteLine($"Задержка изменена на: {delay}ms");
            }
            else
            {
                Console.WriteLine("Неверное значение! Используйте число от 1 до 100.");
            }
            Thread.Sleep(1500);
        }

        private void ResetToDefaults()
        {
            Console.WriteLine("Сбросить все настройки к дефолтным значениям?");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("ЭТО СБРОСИТ:");
            Console.WriteLine("• Программные хоткеи (F2, F3, Space)");
            Console.WriteLine("• Игровые бинды (mwheeldown, F5, F6)");
            Console.WriteLine("• Задержку скролла (1ms)");
            Console.WriteLine("• Режимы Bhop и FPS (включены)");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Введите 'yes' для подтверждения: ");

            string? confirmation = Console.ReadLine()?.Trim().ToLower();
            if (confirmation == "yes")
            {

                config.BhopEnabled = true;
                config.FPSMode = true;
                config.ScrollDirection = "Down";
                config.ScrollDelay = 1;
                config.BhopToggleKey = "F2";
                config.FPSToggleKey = "F3";
                config.JumpActivationKey = "Space";
                config.JumpKey = "mwheeldown";
                config.GameJumpBind = "mwheeldown";
                config.GameFPSLowKey = "f5";
                config.GameFPSHighKey = "f6";

                SaveConfig();

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[CONFIG] Все настройки сброшены к дефолтным!");
                Console.ResetColor();

                Console.WriteLine("[INFO] Обновление игровых конфигов...");
                UpdateGameConfigs();

                Console.WriteLine("[INFO] Обновление конфигов RTSS...");
                UpdateRTSSHotkeyConfig();
                RestartRTSS();

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

                string autoexecPath = Path.Combine(cfgPath, "autoexec.cfg");
                string autoexecContent = $@"// CS2 AutoBhop Configuration

alias +jump_ ""exec +jump""
alias -jump_ ""exec -jump""
bind {config.GameJumpBind} ""+jump_""
{(config.GameJumpBind != "mwheeldown" && config.GameJumpBind != "mwheelup" ? "" : "bind " + (config.GameJumpBind == "mwheeldown" ? "mwheelup" : "mwheeldown") + " \"+jump_\"")}

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

                string jumpPlusContent = @"setinfo jump 0
toggle jump ""1 0 0""";

                string jumpMinusContent = @"setinfo jump 0
toggle jump ""-999 0 0""";

                string autoexecContent = $@"// CS2 AutoBhop Configuration

alias +jump_ ""exec +jump""
alias -jump_ ""exec -jump""
bind {config.GameJumpBind} ""+jump_""
{(config.GameJumpBind != "mwheeldown" && config.GameJumpBind != "mwheelup" ? "" : "bind " + (config.GameJumpBind == "mwheeldown" ? "mwheelup" : "mwheeldown") + " \"+jump_\"")}

echo ""CS2 AutoBhop configs loaded!""";

                int configsCreated = 0;
                int configsSkipped = 0;

                configsCreated += CreateConfigIfNeeded(Path.Combine(cfgPath, "+jump.cfg"), jumpPlusContent, ref configsSkipped);
                configsCreated += CreateConfigIfNeeded(Path.Combine(cfgPath, "-jump.cfg"), jumpMinusContent, ref configsSkipped);
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

        private bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private void RequestAdministratorRights()
        {
            try
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("╔══════════════════════════════════════╗");
                Console.WriteLine("║         ТРЕБУЮТСЯ ПРАВА АДМИНА       ║");
                Console.WriteLine("╚══════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Для записи конфигов RTSS требуются права администратора.");
                Console.WriteLine("Программа будет перезапущена с повышенными правами...");
                Console.WriteLine();
                Console.WriteLine("Нажмите любую клавишу для продолжения...");
                Console.ReadKey();

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath ?? "AutoBhop.exe",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Normal
                };

                try
                {
                    Process.Start(startInfo);
                    Environment.Exit(0);
                }
                catch (System.ComponentModel.Win32Exception)
                {

                    Console.Clear();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("╔══════════════════════════════════════╗");
                    Console.WriteLine("║      ОТКАЗАНО В ПРАВАХ АДМИНА        ║");
                    Console.WriteLine("╚══════════════════════════════════════╝");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Без прав администратора программа не может:");
                    Console.WriteLine("• Записывать конфиги RTSS");
                    Console.WriteLine("• Останавливать процессы RTSS");
                    Console.WriteLine("• Запускать RTSS");
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Программа будет закрыта.");
                    Console.WriteLine("Для работы требуется запуск от имени администратора!");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.WriteLine("Нажмите любую клавишу для выхода...");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запросе прав администратора: {ex.Message}");
                Console.WriteLine("Нажмите любую клавишу для выхода...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }

        private void CheckRTSSSetup()
        {
            LogMessage("[RTSS] Проверка RivaTuner Statistics Server...");

            string? rtssPath = FindRTSSInstallPath();
            if (rtssPath == null)
            {
                LogMessage("[RTSS] RTSS не найден автоматически");
                HandleRTSSNotFound();
                return;
            }

            LogMessage($"[RTSS] RTSS найден: {rtssPath}");
            this.rtssPath = rtssPath;

            if (IsRTSSConfigUpToDate())
            {
                LogMessage("[RTSS] Конфиги RTSS актуальны, запускаем без изменений");
                StartRTSS();
                return;
            }

            LogMessage("[RTSS] Конфиги RTSS требуют обновления");

            if (!AskForRTSSConfigPermission())
            {
                LogMessage("[RTSS] Пользователь отказался от настройки RTSS");
                HandleRTSSConfigDeclined();
                return;
            }

            LogMessage("[RTSS] Пользователь разрешил настройку RTSS");
            KillRTSSProcess();
            SetupRTSSConfigs();
            StartRTSS();
        }

        private string? FindRTSSInstallPath()
        {
            try
            {

                string[] possiblePaths = {
                    @"C:\Program Files (x86)\RivaTuner Statistics Server",
                    @"C:\Program Files\RivaTuner Statistics Server",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "RivaTuner Statistics Server"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RivaTuner Statistics Server")
                };

                foreach (string path in possiblePaths)
                {
                    if (Directory.Exists(path) && File.Exists(Path.Combine(path, "RTSS.exe")))
                    {
                        return path;
                    }
                }

                string[] registryPaths = {
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Unwinder\RTSS",
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Unwinder\RTSS",
                    @"HKEY_CURRENT_USER\SOFTWARE\Unwinder\RTSS"
                };

                foreach (string regPath in registryPaths)
                {
                    string? installPath = Registry.GetValue(regPath, "InstallDir", null) as string;
                    if (installPath != null && Directory.Exists(installPath) && File.Exists(Path.Combine(installPath, "RTSS.exe")))
                    {
                        return installPath;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка поиска RTSS: {ex.Message}");
                return null;
            }
        }

        private void HandleRTSSNotFound()
        {
            var result = MessageBox.Show(
                "RivaTuner Statistics Server не найден автоматически.\n\n" +
                "RTSS необходим для корректной работы программы.\n\n" +
                "У вас установлен RivaTuner?",
                "CS2 AutoBhop - RTSS не найден",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            if (result == DialogResult.Yes)
            {
                ShowRTSSFolderDialog();
            }
            else
            {
                var downloadResult = MessageBox.Show(
                    "Для работы программы необходим RivaTuner Statistics Server.\n\n" +
                    "Открыть сайт для скачивания?",
                    "CS2 AutoBhop - Требуется RTSS",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (downloadResult == DialogResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html",
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }

                MessageBox.Show(
                    "Программа будет закрыта.\n\n" +
                    "Запустите программу снова после установки RTSS.",
                    "CS2 AutoBhop - Выход",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                Environment.Exit(0);
            }
        }

        private void ShowRTSSFolderDialog()
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Выберите папку установки RivaTuner Statistics Server";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;

                    if (File.Exists(Path.Combine(selectedPath, "RTSS.exe")))
                    {
                        LogMessage($"[RTSS] Пользователь указал путь: {selectedPath}");
                        rtssPath = selectedPath;

                        KillRTSSProcess();
                        SetupRTSSConfigs();
                        StartRTSS();
                    }
                    else
                    {
                        MessageBox.Show(
                            "В выбранной папке не найден файл RTSS.exe.\n\n" +
                            "Это не папка RivaTuner Statistics Server.",
                            "CS2 AutoBhop - Неверная папка",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        ShowRTSSFolderDialog();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Без RTSS программа не может работать корректно.\n\n" +
                        "Программа будет закрыта.",
                        "CS2 AutoBhop - Выход",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    Environment.Exit(0);
                }
            }
        }

        private void KillRTSSProcess()
        {
            try
            {
                Process[] rtssProcesses = Process.GetProcessesByName("RTSS");
                foreach (var process in rtssProcesses)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                        LogMessage("[RTSS] Процесс RTSS остановлен");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"[WARNING] Не удалось остановить процесс RTSS: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[WARNING] Ошибка при остановке RTSS: {ex.Message}");
            }
        }

        private void SetupRTSSConfigs()
        {
            if (rtssPath == null) return;

            try
            {
                LogMessage("[RTSS] Настройка конфигурации...");

                string profilesPath = Path.Combine(rtssPath, "Profiles");
                string pluginsPath = Path.Combine(rtssPath, "Plugins", "Client");

                ForceCreateDirectory(profilesPath);
                ForceCreateDirectory(pluginsPath);

                string cs2ProfileContent = @"[OSD]
EnableOSD=1
EnableBgnd=1
EnableFill=0
EnableStat=0
BaseColor=00FF8000
BgndColor=00000000
FillColor=80000000
PositionX=1
PositionY=1
ZoomRatio=2
CoordinateSpace=0
EnableFrameColorBar=0
FrameColorBarMode=0
RefreshPeriod=500
IntegerFramerate=1
MaximumFrametime=0
EnableFrametimeHistory=0
FrametimeHistoryWidth=-32
FrametimeHistoryHeight=-4
FrametimeHistoryStyle=0
ScaleToFit=0

[Framerate]
Limit=64
LimitDenominator=1
LimitTime=0
LimitTimeDenominator=1
SyncDisplay=0
SyncScanline0=0
SyncScanline1=0
SyncPeriods=0
SyncLimiter=0
PassiveWait=1
ReflexSleep=0
ReflexSetLatencyMarker=1

[Hooking]
EnableHooking=1
HookDirect3D8=1
HookDirect3D9=1
HookDXGI=1
HookDirect3D12=1
HookOpenGL=1
HookVulkan=1
InjectionDelay=15000

[Plugins]
HotkeyHandler.dll=1
";

                string lowFpsHex = GetVirtualKeyHexCode(config.GameFPSLowKey);
                string highFpsHex = GetVirtualKeyHexCode(config.GameFPSHighKey);

                string hotkeyHandlerContent = @"[Settings]
Hotkey0=46
Command0=Limit=999
Hotkey1=45
Command1=Limit=64
OSDOnHotkey=00000000
OSDOffHotkey=00000000
OSDToggleHotkey=00000000
LimiterOnHotkey=" + lowFpsHex + @"
LimiterOffHotkey=" + highFpsHex + @"
";

                string cs2ProfilePath = Path.Combine(profilesPath, "cs2.exe.cfg");
                string hotkeyHandlerPath = Path.Combine(pluginsPath, "HotkeyHandler.cfg");

                ForceWriteConfigFile(cs2ProfilePath, cs2ProfileContent, "cs2.exe.cfg");
                ForceWriteConfigFile(hotkeyHandlerPath, hotkeyHandlerContent, "HotkeyHandler.cfg");

                LogMessage("[RTSS] Конфиги установлены и проверены успешно!");
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Критическая ошибка настройки конфигов RTSS: {ex.Message}");

                MessageBox.Show(
                    $"КРИТИЧЕСКАЯ ОШИБКА!\n\n" +
                    $"Не удалось записать конфиги RTSS:\n{ex.Message}\n\n" +
                    $"Убедитесь что:\n" +
                    $"• Программа запущена от администратора\n" +
                    $"• RTSS не заблокирован антивирусом\n" +
                    $"• Папка RTSS доступна для записи",
                    "CS2 AutoBhop - Ошибка записи конфигов",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Environment.Exit(1);
            }
        }

        private void ForceCreateDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    LogMessage($"[RTSS] Создана папка: {path}");
                }

                if (!Directory.Exists(path))
                {
                    throw new DirectoryNotFoundException($"Не удалось создать папку: {path}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Не удалось создать папку {path}: {ex.Message}");
                throw;
            }
        }

        private void ForceWriteConfigFile(string filePath, string content, string fileName)
        {
            try
            {

                File.WriteAllText(filePath, content);
                LogMessage($"[RTSS] Записан конфиг: {fileName}");

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Файл {fileName} не был создан");
                }

                string writtenContent = File.ReadAllText(filePath);
                if (string.IsNullOrEmpty(writtenContent))
                {
                    throw new IOException($"Файл {fileName} пустой - запись не удалась");
                }

                if (!writtenContent.Contains("[") || writtenContent.Length < content.Length / 2)
                {
                    throw new IOException($"Файл {fileName} записан некорректно");
                }

                LogMessage($"[RTSS] ✓ Конфиг {fileName} проверен и корректен");
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Критическая ошибка записи {fileName}: {ex.Message}");
                throw new IOException($"Не удалось записать конфиг {fileName}: {ex.Message}", ex);
            }
        }

        private void StartRTSS()
        {
            if (rtssPath == null) return;

            try
            {
                string rtssExePath = Path.Combine(rtssPath, "RTSS.exe");

                if (File.Exists(rtssExePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = rtssExePath,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Minimized
                    });

                    LogMessage("[RTSS] RTSS запущен успешно");
                    LogMessage("[INFO] Теперь можно запускать игру!");

                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка запуска RTSS: {ex.Message}");
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

        private string GetVirtualKeyHexCode(string keyName)
        {
            uint vkCode = GetVirtualKeyCode(keyName);
            if (vkCode != 0)
            {
                return vkCode.ToString("X8");
            }
            return "00000000";
        }

        private void UpdateRTSSHotkeyConfig()
        {
            if (rtssPath == null) return;

            try
            {
                string pluginsPath = Path.Combine(rtssPath, "Plugins", "Client");
                string hotkeyHandlerPath = Path.Combine(pluginsPath, "HotkeyHandler.cfg");

                string lowFpsHex = GetVirtualKeyHexCode(config.GameFPSLowKey);
                string highFpsHex = GetVirtualKeyHexCode(config.GameFPSHighKey);

                string updatedContent;
                if (File.Exists(hotkeyHandlerPath))
                {
                    LogMessage("[RTSS] Обновление существующего HotkeyHandler.cfg");
                    string existingContent = File.ReadAllText(hotkeyHandlerPath);
                    updatedContent = UpdateHotkeyHandlerContent(existingContent, lowFpsHex, highFpsHex);
                }
                else
                {
                    LogMessage("[RTSS] Создание нового HotkeyHandler.cfg");
                    updatedContent = @"[Settings]
Hotkey0=46
Command0=Limit=999
Hotkey1=45
Command1=Limit=64
OSDOnHotkey=00000000
OSDOffHotkey=00000000
OSDToggleHotkey=00000000
LimiterOnHotkey=" + lowFpsHex + @"
LimiterOffHotkey=" + highFpsHex + @"
";
                }

                ForceWriteConfigFile(hotkeyHandlerPath, updatedContent, "HotkeyHandler.cfg");
                LogMessage($"[RTSS] Горячие клавиши обновлены: низкий FPS = {config.GameFPSLowKey} ({lowFpsHex}), высокий FPS = {config.GameFPSHighKey} ({highFpsHex})");
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка обновления RTSS конфига: {ex.Message}");
            }
        }

        private string UpdateHotkeyHandlerContent(string existingContent, string lowFpsHex, string highFpsHex)
        {
            string[] lines = existingContent.Split('\n');
            bool foundLimiterOn = false;
            bool foundLimiterOff = false;
            bool inSettingsSection = false;

            List<string> updatedLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine == "[Settings]")
                {
                    inSettingsSection = true;
                    updatedLines.Add(line);
                    continue;
                }

                if (trimmedLine.StartsWith("[") && trimmedLine != "[Settings]")
                {
                    inSettingsSection = false;
                }

                if (inSettingsSection)
                {
                    if (trimmedLine.StartsWith("LimiterOnHotkey="))
                    {
                        updatedLines.Add($"LimiterOnHotkey={lowFpsHex}");
                        foundLimiterOn = true;
                        continue;
                    }
                    else if (trimmedLine.StartsWith("LimiterOffHotkey="))
                    {
                        updatedLines.Add($"LimiterOffHotkey={highFpsHex}");
                        foundLimiterOff = true;
                        continue;
                    }
                }

                updatedLines.Add(line);
            }

            if (!foundLimiterOn || !foundLimiterOff)
            {

                for (int i = 0; i < updatedLines.Count; i++)
                {
                    if (updatedLines[i].Trim() == "[Settings]")
                    {
                        int insertIndex = i + 1;

                        while (insertIndex < updatedLines.Count &&
                               !updatedLines[insertIndex].Trim().StartsWith("["))
                        {
                            insertIndex++;
                        }

                        if (!foundLimiterOn)
                        {
                            updatedLines.Insert(insertIndex, $"LimiterOnHotkey={lowFpsHex}");
                            insertIndex++;
                        }
                        if (!foundLimiterOff)
                        {
                            updatedLines.Insert(insertIndex, $"LimiterOffHotkey={highFpsHex}");
                        }
                        break;
                    }
                }
            }

            return string.Join("\n", updatedLines);
        }

        private void RestartRTSS()
        {
            if (rtssPath == null) return;

            try
            {
                LogMessage("[RTSS] Перезапуск RivaTuner...");

                KillRTSSProcess();

                Thread.Sleep(1000);

                StartRTSS();

                LogMessage("[RTSS] RivaTuner успешно перезапущен с новыми настройками!");
            }
            catch (Exception ex)
            {
                LogMessage($"[ERROR] Ошибка перезапуска RTSS: {ex.Message}");
            }
        }

        private bool IsRTSSConfigUpToDate()
        {
            if (rtssPath == null) return false;

            try
            {
                string pluginsPath = Path.Combine(rtssPath, "Plugins", "Client");
                string hotkeyHandlerPath = Path.Combine(pluginsPath, "HotkeyHandler.cfg");

                if (!File.Exists(hotkeyHandlerPath))
                {
                    LogMessage("[RTSS] HotkeyHandler.cfg не найден");
                    return false;
                }

                string content = File.ReadAllText(hotkeyHandlerPath);

                string expectedLowFpsHex = GetVirtualKeyHexCode(config.GameFPSLowKey);
                string expectedHighFpsHex = GetVirtualKeyHexCode(config.GameFPSHighKey);

                if (!content.Contains("[Settings]"))
                {
                    LogMessage("[RTSS] Секция [Settings] не найдена в HotkeyHandler.cfg");
                    return false;
                }

                bool hasCorrectLowFps = false;
                bool hasCorrectHighFps = false;

                string[] lines = content.Split('\n');
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("LimiterOnHotkey="))
                    {
                        string currentValue = trimmedLine.Substring("LimiterOnHotkey=".Length);
                        hasCorrectLowFps = currentValue.Equals(expectedLowFpsHex, StringComparison.OrdinalIgnoreCase);
                        LogMessage($"[RTSS] LimiterOnHotkey: текущий = {currentValue}, ожидаемый = {expectedLowFpsHex}, совпадает = {hasCorrectLowFps}");
                    }
                    else if (trimmedLine.StartsWith("LimiterOffHotkey="))
                    {
                        string currentValue = trimmedLine.Substring("LimiterOffHotkey=".Length);
                        hasCorrectHighFps = currentValue.Equals(expectedHighFpsHex, StringComparison.OrdinalIgnoreCase);
                        LogMessage($"[RTSS] LimiterOffHotkey: текущий = {currentValue}, ожидаемый = {expectedHighFpsHex}, совпадает = {hasCorrectHighFps}");
                    }
                }

                bool isUpToDate = hasCorrectLowFps && hasCorrectHighFps;
                LogMessage($"[RTSS] Конфиг актуален: {isUpToDate}");

                return isUpToDate;
            }
            catch (Exception ex)
            {
                LogMessage($"[RTSS] Ошибка проверки конфига: {ex.Message}");
                return false;
            }
        }

        private bool AskForRTSSConfigPermission()
        {
            var result = MessageBox.Show(
                "RivaTuner Statistics Server найден!\n\n" +
                "Для корректной работы программы необходимо настроить конфигурации RTSS:\n\n" +
                "• Профиль для CS2 с ограничением FPS\n" +
                "• Глобальные горячие клавиши для переключения FPS\n" +
                "• Плагин HotkeyHandler для обработки нажатий\n\n" +
                "Это может изменить некоторые существующие настройки RTSS.\n\n" +
                "Разрешить программе настроить RTSS?",
                "CS2 AutoBhop - Настройка RivaTuner",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1
            );

            return result == DialogResult.Yes;
        }

        private void HandleRTSSConfigDeclined()
        {
            var result = MessageBox.Show(
                "Без настройки RTSS программа будет работать в ограниченном режиме:\n\n" +
                "✓ AutoBhop будет работать\n" +
                "✗ Автоматическое переключение FPS НЕ будет работать\n" +
                "✗ Горячие клавиши F5/F6 НЕ будут работать\n\n" +
                "Вы сможете настроить RTSS позже через меню программы.\n\n" +
                "Продолжить работу без настройки RTSS?",
                "CS2 AutoBhop - Ограниченный режим",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1
            );

            if (result == DialogResult.Yes)
            {
                LogMessage("[RTSS] Работа в ограниченном режиме без RTSS");
                LogMessage("[WARNING] FPS Control не будет работать без настройки RTSS");

                config.FPSMode = false;
                SaveConfig();

                rtssPath = null;
            }
            else
            {
                LogMessage("[RTSS] Пользователь передумал, показываем диалог настройки заново");

                if (AskForRTSSConfigPermission())
                {
                    LogMessage("[RTSS] Пользователь разрешил настройку RTSS");
                    KillRTSSProcess();
                    SetupRTSSConfigs();
                    StartRTSS();
                }
                else
                {

                    HandleRTSSConfigDeclined();
                }
            }
        }

        private void SetupRTSSLater()
        {
            Console.WriteLine("Настройка RTSS...");
            Console.WriteLine();

            string? foundRtssPath = FindRTSSInstallPath();
            if (foundRtssPath == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("RTSS не найден!");
                Console.ResetColor();
                Console.WriteLine("Убедитесь что RivaTuner Statistics Server установлен.");
                Thread.Sleep(3000);
                return;
            }

            Console.WriteLine($"RTSS найден: {foundRtssPath}");

            if (AskForRTSSConfigPermission())
            {
                rtssPath = foundRtssPath;

                Console.WriteLine("Настройка RTSS конфигов...");
                KillRTSSProcess();
                SetupRTSSConfigs();
                StartRTSS();

                config.FPSMode = true;
                SaveConfig();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("RTSS успешно настроен!");
                Console.WriteLine("FPS Control теперь работает!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Настройка RTSS отменена.");
                Console.ResetColor();
            }

            Thread.Sleep(3000);
        }
    }
}