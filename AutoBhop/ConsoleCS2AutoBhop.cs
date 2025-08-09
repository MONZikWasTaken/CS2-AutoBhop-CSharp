using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace CS2AutoBhop
{
    public class ConsoleCS2AutoBhop
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

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
            public MOUSEINPUT mi;
        }

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

        // Virtual Key Codes
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
        const uint VK_LMENU = 0xA4; // Alt
        const uint VK_RMENU = 0xA5; // Alt
        // Буквы A-Z
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
        // Цифры 0-9
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
            
            // Программные хоткеи
            public string BhopToggleKey { get; set; } = "F2";
            public string FPSToggleKey { get; set; } = "F3";
            public string JumpActivationKey { get; set; } = "Space";
            
            // Игровые бинды
            public string GameJumpBind { get; set; } = "mwheeldown"; // mwheeldown, mwheelup, space и т.д.
            public string GameFPSLowKey { get; set; } = "F5"; // F5 для fps_max 64
            public string GameFPSHighKey { get; set; } = "F6"; // F6 для fps_max 0
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

        public void Run()
        {
            // Устанавливаем кодировку UTF-8 для эмодзи
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.CursorVisible = false;
            
            InitializeKeyMap();
            LoadConfig();
            
            // Инициализируем отображение
            ShowInitialDisplay();
            
            
            LogMessage("🎮 CS2 AutoBhop Console запущен!");
            LogMessage("🔍 Поиск процесса CS2...");
            LogMessage("📁 Создание конфигов CS2...");

            CreateCS2Configs();
            StartMonitoring();

            // Основной цикл - проверяем hotkeys и bhop
            while (true)
            {
                CheckHotkeys();
                CheckBhop();
                Thread.Sleep(1); // Минимальная задержка
            }
        }

        private void CheckHotkeys()
        {
            // Bhop toggle hotkey
            uint bhopToggleVK = GetVirtualKeyCode(config.BhopToggleKey);
            if (bhopToggleVK != 0)
            {
                bool bhopTogglePressed = (GetAsyncKeyState((int)bhopToggleVK) & 0x8000) != 0;
                if (bhopTogglePressed && !lastBhopToggleState)
                {
                    config.BhopEnabled = !config.BhopEnabled;
                    SaveConfig();
                    LogMessage($"🦘 Bhop: {(config.BhopEnabled ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН")}");
                    UpdateStatusInPlace();
                }
                lastBhopToggleState = bhopTogglePressed;
            }

            // FPS toggle hotkey
            uint fpsToggleVK = GetVirtualKeyCode(config.FPSToggleKey);
            if (fpsToggleVK != 0)
            {
                bool fpsTogglePressed = (GetAsyncKeyState((int)fpsToggleVK) & 0x8000) != 0;
                if (fpsTogglePressed && !lastFPSToggleState)
                {
                    config.FPSMode = !config.FPSMode;
                    SaveConfig();
                    LogMessage($"🎯 FPS Control: {(config.FPSMode ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН")}");
                    UpdateStatusInPlace();
                }
                lastFPSToggleState = fpsTogglePressed;
            }

            // Insert для настроек
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
            Console.WriteLine("▶ СТАТУС:");
            Console.ResetColor();
            
            ShowStatus();
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("▶ УПРАВЛЕНИЕ:");
            Console.ResetColor();
            Console.WriteLine($"  Insert - настройки");
            Console.WriteLine($"  {config.BhopToggleKey}     - переключить Bhop");
            Console.WriteLine($"  {config.FPSToggleKey}     - переключить FPS Control");
            Console.WriteLine($"  {config.JumpActivationKey}  - (в игре) активировать бхоп");
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("════════════════════════════════════════");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("▶ ЛОГИ:");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("════════════════════════════════════════");
            Console.ResetColor();
        }
        
        private void ShowStatus()
        {
            // Очищаем и выводим строку Auto Bhop
            Console.Write("  🦘 Auto Bhop:  ");
            Console.ForegroundColor = config.BhopEnabled ? ConsoleColor.Green : ConsoleColor.Red;
            string bhopText = config.BhopEnabled ? "ВКЛЮЧЕН" : "ВЫКЛЮЧЕН";
            Console.Write(bhopText);
            Console.Write(new string(' ', Math.Max(0, 50 - bhopText.Length))); // Очищаем остаток строки
            Console.WriteLine();
            Console.ResetColor();
            
            // Очищаем и выводим строку FPS Control
            Console.Write("  🎯 FPS Control: ");
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
            Console.Write(new string(' ', Math.Max(0, 50 - fpsText.Length))); // Очищаем остаток строки
            Console.WriteLine();
            Console.ResetColor();
            
            // Очищаем и выводим строку CS2 Process
            Console.Write("  🎮 CS2 Process: ");
            Console.ForegroundColor = cs2Handle != IntPtr.Zero ? ConsoleColor.Green : ConsoleColor.Red;
            string cs2Text = cs2Handle != IntPtr.Zero ? "НАЙДЕН" : "НЕ НАЙДЕН";
            Console.Write(cs2Text);
            Console.Write(new string(' ', Math.Max(0, 50 - cs2Text.Length))); // Очищаем остаток строки
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
                    
                    // Обновляем только строки статуса (строки 5-7)
                    Console.SetCursorPosition(0, 5);
                    ShowStatus();
                    
                    // Возвращаем курсор
                    Console.SetCursorPosition(currentLeft, currentTop);
                }
                catch
                {
                    // Игнорируем ошибки
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
            LogMessage("⬆️ Прыжок начат");

            // FPS control работает только если Bhop включен
            if (config.BhopEnabled && config.FPSMode && !fpsOn)
            {
                uint lowFpsVK = GetVirtualKeyCode(config.GameFPSLowKey);
                if (lowFpsVK != 0)
                {
                    SendKey(lowFpsVK);
                    fpsOn = true;
                    LogMessage($"🔽 {config.GameFPSLowKey} (низкий FPS для бхопа)");
                }
            }
            else if (config.BhopEnabled && !config.FPSMode)
            {
                uint highFpsVK = GetVirtualKeyCode(config.GameFPSHighKey);
                if (highFpsVK != 0)
                {
                    SendKey(highFpsVK);
                    LogMessage($"🔼 {config.GameFPSHighKey} (высокий FPS)");
                }
            }

            bhopTimer = new System.Threading.Timer(BhopTick, null, 0, config.ScrollDelay);
        }

        private void HandleJumpRelease()
        {
            isJumping = false;
            LogMessage("⬇️ Прыжок завершен");

            // FPS control работает только если Bhop включен
            if (config.BhopEnabled && config.FPSMode && fpsOn)
            {
                uint highFpsVK = GetVirtualKeyCode(config.GameFPSHighKey);
                if (highFpsVK != 0)
                {
                    SendKey(highFpsVK);
                    fpsOn = false;
                    LogMessage($"🔼 {config.GameFPSHighKey} (высокий FPS после бхопа)");
                }
            }

            bhopTimer?.Dispose();
        }

        private void BhopTick(object? state)
        {
            if (!isJumping || !config.BhopEnabled) return;

            int delta = config.ScrollDirection == "Down" ? -WHEEL_DELTA : WHEEL_DELTA;
            SendMouseWheel(delta);
        }

        private void SendKey(uint keyCode)
        {
            keybd_event((byte)keyCode, 0, 0, UIntPtr.Zero);
            Thread.Sleep(10);
            keybd_event((byte)keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
        
        private void InitializeKeyMap()
        {
            // F-клавиши
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
            
            // Специальные клавиши
            keyMap["Space"] = VK_SPACE;
            keyMap["Enter"] = VK_ENTER;
            keyMap["Insert"] = VK_INSERT;
            keyMap["LShift"] = VK_LSHIFT;
            keyMap["RShift"] = VK_RSHIFT;
            keyMap["LControl"] = VK_LCONTROL;
            keyMap["RControl"] = VK_RCONTROL;
            keyMap["LAlt"] = VK_LMENU;
            keyMap["RAlt"] = VK_RMENU;
            
            // Буквы A-Z
            keyMap["A"] = VK_A; keyMap["B"] = VK_B; keyMap["C"] = VK_C; keyMap["D"] = VK_D;
            keyMap["E"] = VK_E; keyMap["F"] = VK_F; keyMap["G"] = VK_G; keyMap["H"] = VK_H;
            keyMap["I"] = VK_I; keyMap["J"] = VK_J; keyMap["K"] = VK_K; keyMap["L"] = VK_L;
            keyMap["M"] = VK_M; keyMap["N"] = VK_N; keyMap["O"] = VK_O; keyMap["P"] = VK_P;
            keyMap["Q"] = VK_Q; keyMap["R"] = VK_R; keyMap["S"] = VK_S; keyMap["T"] = VK_T;
            keyMap["U"] = VK_U; keyMap["V"] = VK_V; keyMap["W"] = VK_W; keyMap["X"] = VK_X;
            keyMap["Y"] = VK_Y; keyMap["Z"] = VK_Z;
            
            // Цифры 0-9
            keyMap["0"] = VK_0; keyMap["1"] = VK_1; keyMap["2"] = VK_2; keyMap["3"] = VK_3;
            keyMap["4"] = VK_4; keyMap["5"] = VK_5; keyMap["6"] = VK_6; keyMap["7"] = VK_7;
            keyMap["8"] = VK_8; keyMap["9"] = VK_9;
        }
        
        private uint GetVirtualKeyCode(string keyName)
        {
            string normalizedKey = NormalizeKeyName(keyName);
            if (keyMap.ContainsKey(normalizedKey))
            {
                return keyMap[normalizedKey];
            }
            return 0; // Неизвестная клавиша
        }
        
        private string NormalizeKeyName(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return keyName;
                
            // Приводим к верхнему регистру для букв и цифр
            string normalized = keyName.Trim().ToUpper();
            
            // Специальная обработка для некоторых клавиш
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
                default:
                    // Для F-клавиш, букв и цифр оставляем как есть (уже в верхнем регистре)
                    if (normalized.StartsWith("F") || (normalized.Length == 1 && (char.IsLetter(normalized[0]) || char.IsDigit(normalized[0]))))
                    {
                        return normalized;
                    }
                    return keyName; // Возвращаем оригинал если не подходит
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
            Console.WriteLine("▶ ПРОГРАММНЫЕ ХОТКЕИ:");
            Console.ResetColor();
            Console.WriteLine($"  1. Переключение Bhop: {config.BhopToggleKey}");
            Console.WriteLine($"  2. Переключение FPS Control: {config.FPSToggleKey}");
            Console.WriteLine($"  3. Активация прыжка: {config.JumpActivationKey}");
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("▶ ИГРОВЫЕ БИНДЫ:");
            Console.ResetColor();
            Console.WriteLine($"  4. Кнопка прыжка в игре: {config.GameJumpBind}");
            Console.WriteLine($"  5. Кнопка низкого FPS: {config.GameFPSLowKey}");
            Console.WriteLine($"  6. Кнопка высокого FPS: {config.GameFPSHighKey}");
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("▶ ДРУГИЕ НАСТРОЙКИ:");
            Console.ResetColor();
            Console.WriteLine($"  7. Направление скролла: {config.ScrollDirection}");
            Console.WriteLine($"  8. Задержка скролла: {config.ScrollDelay}ms");
            Console.WriteLine("  9. Пересоздать конфиги игры");
            Console.WriteLine(" 10. Сбросить все настройки к дефолтным");
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  ВНИМАНИЕ: При изменении игровых биндов (4-6)");
            Console.WriteLine("   требуется ПЕРЕЗАПУСК CS2 для применения!");
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
                    config.GameJumpBind = ChangeGameJumpBind("Кнопка прыжка в игре", config.GameJumpBind);
                    UpdateGameConfigs();
                    break;
                case "5":
                    config.GameFPSLowKey = ChangeGameHotkey("Кнопка низкого FPS", config.GameFPSLowKey);
                    UpdateGameConfigs();
                    break;
                case "6":
                    config.GameFPSHighKey = ChangeGameHotkey("Кнопка высокого FPS", config.GameFPSHighKey);
                    UpdateGameConfigs();
                    break;
                case "7":
                    config.ScrollDirection = config.ScrollDirection == "Down" ? "Up" : "Down";
                    Console.WriteLine($"Направление скролла изменено на: {config.ScrollDirection}");
                    Thread.Sleep(1000);
                    break;
                case "8":
                    ChangeScrollDelay();
                    break;
                case "9":
                    CreateCS2Configs();
                    Thread.Sleep(2000);
                    break;
                case "10":
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
            ShowConfigMenu(); // Показываем меню снова
        }
        
        private string ChangeHotkey(string description, string currentHotkey)
        {
            Console.WriteLine($"Изменение: {description}");
            Console.WriteLine("Доступные клавиши:");
            Console.WriteLine("F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12");
            Console.WriteLine("Space, Enter, Insert, LShift, RShift, LControl, RControl, LAlt, RAlt");
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
                    // Конвертируем в игровой формат (нижний регистр для CS2)
                    string gameBind = normalizedKey.ToLower();
                    if (normalizedKey == "Space") gameBind = "space";
                    else if (normalizedKey == "Enter") gameBind = "enter";
                    else if (normalizedKey == "LShift") gameBind = "lshift";
                    else if (normalizedKey == "RShift") gameBind = "rshift";
                    else if (normalizedKey == "LControl") gameBind = "lcontrol";
                    else if (normalizedKey == "RControl") gameBind = "rcontrol";
                    else if (normalizedKey == "LAlt") gameBind = "lalt";
                    else if (normalizedKey == "RAlt") gameBind = "ralt";
                    
                    Console.WriteLine($"Игровая клавиша изменена на: {gameBind}");
                    Thread.Sleep(1500);
                    return gameBind;
                }
            }
            
            Console.WriteLine("Неверная клавиша!");
            Thread.Sleep(1500);
            return currentHotkey;
        }
        
        private string ChangeGameJumpBind(string description, string currentBind)
        {
            Console.WriteLine($"Изменение: {description}");
            Console.WriteLine("Доступные бинды:");
            Console.WriteLine("mwheeldown, mwheelup, mouse1, mouse2");
            Console.WriteLine("F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12");
            Console.WriteLine("Space, Enter, Insert, LShift, RShift, LControl, RControl, LAlt, RAlt");
            Console.WriteLine("+ любые буквы/цифры (A-Z, 0-9)");
            Console.Write("Введите бинд: ");
            
            string? input = Console.ReadLine()?.Trim()?.ToLower();
            if (!string.IsNullOrEmpty(input))
            {
                // Специальные игровые бинды
                if (input == "mwheeldown" || input == "mwheelup" || 
                    input == "mouse1" || input == "mouse2")
                {
                    Console.WriteLine($"Игровой бинд изменен на: {input}");
                    Thread.Sleep(1500);
                    return input;
                }
                
                // Обычные клавиши
                string normalizedKey = NormalizeKeyName(input);
                if (keyMap.ContainsKey(normalizedKey))
                {
                    string gameBind = normalizedKey.ToLower();
                    // Для игры специальные названия
                    if (normalizedKey == "Space") gameBind = "space";
                    else if (normalizedKey == "Enter") gameBind = "enter";
                    else if (normalizedKey == "LShift") gameBind = "lshift";
                    else if (normalizedKey == "RShift") gameBind = "rshift";
                    else if (normalizedKey == "LControl") gameBind = "lcontrol";
                    else if (normalizedKey == "RControl") gameBind = "rcontrol";
                    else if (normalizedKey == "LAlt") gameBind = "lalt";
                    else if (normalizedKey == "RAlt") gameBind = "ralt";
                    
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
            Console.WriteLine("• Направление скролла (Down)");
            Console.WriteLine("• Задержку скролла (1ms)");
            Console.WriteLine("• Режимы Bhop и FPS (включены)");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Введите 'yes' для подтверждения: ");
            
            string? confirmation = Console.ReadLine()?.Trim().ToLower();
            if (confirmation == "yes")
            {
                // Сбрасываем все настройки к дефолтным
                config.BhopEnabled = true;
                config.FPSMode = true;
                config.ScrollDirection = "Down";
                config.ScrollDelay = 1;
                config.BhopToggleKey = "F2";
                config.FPSToggleKey = "F3";
                config.JumpActivationKey = "Space";
                config.GameJumpBind = "mwheeldown";
                config.GameFPSLowKey = "f5";
                config.GameFPSHighKey = "f6";
                
                SaveConfig();
                
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Все настройки сброшены к дефолтным!");
                Console.ResetColor();
                
                // Обновляем игровые конфиги
                Console.WriteLine("🔄 Обновление игровых конфигов...");
                UpdateGameConfigs();
                
                Console.WriteLine("✨ Сброс завершен!");
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
                    Console.WriteLine("❌ CS2 не найден в реестре, конфиги не обновлены");
                    Thread.Sleep(2000);
                    return;
                }

                string cfgPath = Path.Combine(cs2Path, "game", "csgo", "cfg");
                
                if (!Directory.Exists(cfgPath))
                {
                    Console.WriteLine($"❌ Папка cfg не найдена: {cfgPath}");
                    Thread.Sleep(2000);
                    return;
                }

                // Создаем обновленный autoexec.cfg
                string autoexecPath = Path.Combine(cfgPath, "autoexec.cfg");
                string autoexecContent = $@"// CS2 AutoBhop Configuration
// Прыжки на настраиваемую кнопку
alias +jump_ ""exec +jump""
alias -jump_ ""exec -jump""
bind {config.GameJumpBind} ""+jump_""
{(config.GameJumpBind != "mwheeldown" && config.GameJumpBind != "mwheelup" ? "" : "bind " + (config.GameJumpBind == "mwheeldown" ? "mwheelup" : "mwheeldown") + " \"+jump_\"")}

// fps_max 64 по настраиваемой кнопке (для бхопа)
alias fps_set_64 ""fps_max 64""
bind {config.GameFPSLowKey.ToLower()} ""fps_set_64""

// fps_max 0 по настраиваемой кнопке (для игры)
alias fps_set_0 ""fps_max 0""
bind {config.GameFPSHighKey.ToLower()} ""fps_set_0""

echo ""CS2 AutoBhop configs loaded!""";

                File.WriteAllText(autoexecPath, autoexecContent);
                Console.WriteLine("✅ Конфиг autoexec.cfg обновлен!");
                
                // Проверяем запущена ли игра
                if (IsCS2Running())
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️ ВНИМАНИЕ: CS2 запущен!");
                    Console.WriteLine("🔄 ПЕРЕЗАПУСТИТЕ ИГРУ для применения изменений!");
                    Console.WriteLine("💡 Или выполните в консоли игры: exec autoexec");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("🎮 CS2 не запущен - изменения будут применены при запуске игры");
                }
                
                Thread.Sleep(3000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обновления конфигов: {ex.Message}");
                Thread.Sleep(2000);
            }
        }

        private void SendMouseWheel(int delta)
        {
            try
            {
                INPUT[] inputs = new INPUT[1];
                inputs[0].type = INPUT_MOUSE;
                inputs[0].mi.mouseData = (uint)delta;
                inputs[0].mi.dwFlags = MOUSEEVENTF_WHEEL;
                
                SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Ошибка колеса: {ex.Message}");
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
                        LogMessage("✅ CS2 процесс найден!");
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
                    LogMessage("❌ CS2 не найден в реестре, конфиги не созданы");
                    return;
                }

                string cfgPath = Path.Combine(cs2Path, "game", "csgo", "cfg");
                
                if (!Directory.Exists(cfgPath))
                {
                    LogMessage($"❌ Папка cfg не найдена: {cfgPath}");
                    return;
                }

                // Проверяем запущена ли игра CS2
                bool gameWasRunning = IsCS2Running();
                
                // Подготавливаем содержимое конфигов
                string jumpPlusContent = @"setinfo jump 0
toggle jump ""1 0 0""";

                string jumpMinusContent = @"setinfo jump 0
toggle jump ""-999 0 0""";

                string autoexecContent = $@"// CS2 AutoBhop Configuration
// Прыжки на настраиваемую кнопку
alias +jump_ ""exec +jump""
alias -jump_ ""exec -jump""
bind {config.GameJumpBind} ""+jump_""
{(config.GameJumpBind != "mwheeldown" && config.GameJumpBind != "mwheelup" ? "" : "bind " + (config.GameJumpBind == "mwheeldown" ? "mwheelup" : "mwheeldown") + " \"+jump_\"")}

// fps_max 64 по настраиваемой кнопке (для бхопа)
alias fps_set_64 ""fps_max 64""
bind {config.GameFPSLowKey.ToLower()} ""fps_set_64""

// fps_max 0 по настраиваемой кнопке (для игры)
alias fps_set_0 ""fps_max 0""
bind {config.GameFPSHighKey.ToLower()} ""fps_set_0""

echo ""CS2 AutoBhop configs loaded!""";

                // Проверяем и создаем конфиги
                int configsCreated = 0;
                int configsSkipped = 0;

                configsCreated += CreateConfigIfNeeded(Path.Combine(cfgPath, "+jump.cfg"), jumpPlusContent, ref configsSkipped);
                configsCreated += CreateConfigIfNeeded(Path.Combine(cfgPath, "-jump.cfg"), jumpMinusContent, ref configsSkipped);
                configsCreated += CreateConfigIfNeeded(Path.Combine(cfgPath, "autoexec.cfg"), autoexecContent, ref configsSkipped);

                // Выводим результат
                if (configsCreated > 0)
                {
                    LogMessage($"✅ Создано конфигов: {configsCreated}");
                    
                    if (gameWasRunning)
                    {
                        LogMessage("⚠️ CS2 был запущен - ОБЯЗАТЕЛЬНО ПЕРЕЗАПУСТИ ИГРУ!");
                        LogMessage("🔄 Конфиги применятся только после ПОЛНОГО ПЕРЕЗАПУСКА игры!");
                        LogMessage("💡 Альтернатива: выполни в консоли игры команду: exec autoexec");
                    }
                    else
                    {
                        LogMessage("🎮 CS2 не запущен - можешь запускать игру!");
                    }
                }
                
                if (configsSkipped > 0)
                {
                    LogMessage($"ℹ️ Пропущено конфигов: {configsSkipped} (уже актуальные)");
                    
                    if (configsCreated == 0)
                    {
                        if (gameWasRunning)
                        {
                            LogMessage("✅ Конфиги актуальны и игра запущена - можешь играть!");
                        }
                        else
                        {
                            LogMessage("✅ Конфиги актуальны - можешь запускать игру!");
                        }
                    }
                }

                if (configsCreated == 0 && configsSkipped == 0)
                {
                    LogMessage("❌ Не удалось создать конфиги");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Ошибка создания конфигов: {ex.Message}");
            }
        }

        private int CreateConfigIfNeeded(string configPath, string expectedContent, ref int skipped)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string existingContent = File.ReadAllText(configPath);
                    
                    // Если содержимое такое же - пропускаем
                    if (existingContent.Trim() == expectedContent.Trim())
                    {
                        skipped++;
                        return 0;
                    }
                    
                    // Если содержимое разное - спрашиваем разрешение
                    string fileName = Path.GetFileName(configPath);
                    LogMessage($"⚠️ Найден конфиг {fileName} с другим содержимым");
                    
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
                        LogMessage($"❌ Пользователь отказался перезаписывать {fileName}");
                        LogMessage("❌ Программа не может работать без нужных конфигов");
                        
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
                    
                    LogMessage($"✅ Пользователь разрешил перезаписать {fileName}");
                }

                File.WriteAllText(configPath, expectedContent);
                return 1;
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Ошибка создания {Path.GetFileName(configPath)}: {ex.Message}");
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
                
                // Проверяем и старое название
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
                // Ищем Steam в реестре
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
                        LogMessage($"🎮 Steam найден: {installPath}");
                        
                        // Проверяем стандартную папку CS2
                        string cs2Path = Path.Combine(installPath, "steamapps", "common", "Counter-Strike Global Offensive");
                        if (Directory.Exists(cs2Path))
                        {
                            LogMessage($"✅ CS2 найден: {cs2Path}");
                            return cs2Path;
                        }

                        // Ищем в других библиотеках Steam
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
                LogMessage($"❌ Ошибка поиска CS2: {ex.Message}");
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
                                LogMessage($"✅ CS2 найден в библиотеке: {cs2Path}");
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
            lock (logLock)
            {
                // Проверяем, не достигли ли мы лимита высоты консоли
                if (Console.CursorTop >= Console.WindowHeight - 2)
                {
                    // Очищаем консоль и показываем шапку заново
                    logCount = 0;
                    ShowInitialDisplay();
                }
                
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                logCount++;
                
                // Выводим сообщение в консоль
                // Время серым цветом
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{timestamp}] ");
                Console.ResetColor();
                
                // Сообщение с цветом в зависимости от типа
                if (message.Contains("✅"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (message.Contains("❌"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (message.Contains("⚠️"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else if (message.Contains("🎮") || message.Contains("🔍") || message.Contains("📁"))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else if (message.Contains("🦘") || message.Contains("🎯"))
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                }
                else if (message.Contains("⬆️") || message.Contains("⬇️") || message.Contains("🖱️"))
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                }
                else if (message.Contains("🧪"))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
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
                    LogMessage("⚙️ Конфигурация загружена");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Ошибка загрузки конфига: {ex.Message}");
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
                LogMessage($"❌ Ошибка сохранения: {ex.Message}");
            }
        }
    }
}