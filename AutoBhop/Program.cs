namespace CS2AutoBhop
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Показываем консоль для отладки
            AllocConsole();
            Console.Title = "CS2 AutoBhop Debug Console";

            try
            {
                var bhop = new ConsoleCS2AutoBhop();    
                bhop.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка: {ex.Message}");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey();
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}