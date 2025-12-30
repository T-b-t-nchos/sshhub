using System.Diagnostics;
using static sshhub.Action;
using static sshhub.Action.Config;
using static sshhub.Action.WriteLine;

namespace sshhub
{
    class Program
    {
        public static readonly string CONFIGPATH =
            Path.Combine(Directory.GetParent(Environment.ProcessPath!)!.ToString(), "config.json");

        public static ConfigRoot Config = new();

        static void Main(string[] _)
        {
            if (!File.Exists(CONFIGPATH))
            {
                Config = new ConfigRoot();
                Save(Config);
            }
            else
            {
                Config = ReLoad();
            }

            Console.Clear();
            ShowMenu();
        }

        /// <summary>
        /// Displays the application's main menu, accepts a selection, and dispatches to the corresponding action.
        /// </summary>
        /// <remarks>
        /// Clears the console, renders the menu, and invokes one of: Connect, ListTargets, AddTarget, EditTarget, DeleteTarget, EditExec, or ConfirmExit. Selecting the exit option or cancelling the menu will invoke ConfirmExit, which may terminate the process.
        /// </remarks>
        static void ShowMenu()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            HBar();
            Console.WriteLine("  SSH Hub (sshhub)");
            HBar();
            Console.ResetColor();
            Console.WriteLine();

            string[] menuItems =
            [
                "\e[92m" + "$ " + "1. Connect",
                "\e[96m" + "$ " + "2. List Targets / Settings",
                "\e[93m" + "$ " + "3. Add Target",
                "\e[93m" + "$ " + "4. Edit Target",
                "\e[95m" + "$ " + "5. Delete Target",
                "\e[95m" + "$ " + "6. Edit Execution Option",
                "\e[91m" + "$ " + "7. Exit"
            ];

            int selected = SelectableMenu(menuItems, 4, true);
            if (selected == -1)
            {
                ConfirmExit();
            }

            switch (selected)
            {
                case 0: Connect(); break;
                case 1: ListTargets(); break;
                case 2: AddTarget(); break;
                case 3: EditTarget(); break;
                case 4: DeleteTarget(); break;
                case 5: EditExec(); break;
                case 6: ConfirmExit(); break;
            }
        }

        static void Connect()
        {
            Console.Clear();

            TargetConfig? target = SelectTarget(Config, "Select Target to Connect", "\e[92m", true);
            if (target == null)
            {
                ShowMenu();
                return;
            }

            string exec = Config.Exec
                .Replace("{$IP}", target.IP)
                .Replace("{$Port}", target.Port.ToString())
                .Replace("{$Username}", target.Username);

            var parts = exec.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            HalfHBar();

            Success($"Running ssh to {target.Username}@{target.IP}:{target.Port}");
            Info($"({exec})");

            Console.WriteLine();

            HBar();

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = parts[0],
                Arguments = parts.Length > 1 ? parts[1] : "",
                UseShellExecute = false
            });

            process?.WaitForExit();

            HBar();

            Console.WriteLine();

            Warning("SSH session ended");
            Info("Press any key to return to menu...");
            Console.ReadKey(true);

            ShowMenu();
            return;
        }

        /// <summary>
        /// Display a two-option menu that lets the user view configured targets or view settings, handle the chosen action, and return to the main menu.
        /// </summary>
        /// <remarks>
        /// Presents options to list targets or list settings, allows navigation with Up/Down or by pressing 1 or 2, and cancels back to the main menu when ESC is pressed. After performing the selected action, waits for a key press and then returns to the main menu.
        /// </remarks>
        static void ListTargets()
        {
            Console.Clear();

            Info("Select option (Press ESC to cancel)");
            Info("You can choose Up/Down Allow or Number 1or2");

            string[] menuItems =
            [
                "\e[92m" + "$ " + "1. List Targets",
                "\e[93m" + "$ " + "2. List Settings",
            ];

            int selected = SelectableMenu(menuItems, 2, true);
            if (selected == -1)
            {
                ShowMenu();
                return;
            }

            Console.WriteLine();

            switch (selected)
            {
                case 0:
                    WriteTargets(Config.Targets);
                    break;
                case 1:
                    Show(Config);
                    break;
            }

            Console.WriteLine();

            Info("Press any key to return to the menu...");
            Console.ReadKey(true);
            ShowMenu();
        }

        /// <summary>
        /// Prompts the user to create a new target, adds it to the current configuration, persists the configuration, and returns to the main menu.
        /// </summary>
        static void AddTarget()
        {
            Console.Clear();

            var newTarget = AddTargetConfig(
                target: null,
                allTargets: Config.Targets
            );

            if (newTarget == null)
            {
                ShowMenu();
                return;
            }

            Config.Targets = [.. Config.Targets, newTarget];

            Config = Save(Config);

            Console.ReadKey(true);
            ShowMenu();
        }

        /// <summary>
        /// Prompts the user to choose an existing target, opens the target editor for modification, saves changes to the configuration, and returns to the main menu.
        /// </summary>
        /// <remarks>
        /// If the user cancels selection or no target is chosen, control returns immediately to the main menu without saving.
        /// </remarks>
        static void EditTarget()
        {
            Console.Clear();

            TargetConfig? target = SelectTarget(Config, "Select Target to edit", "\e[93m", false);
            if (target == null)
            {
                ShowMenu();
                return;
            }

            TargetConfig? newTarget = EditTargetConfig(
                target,
                Config.Targets
            );

            if (newTarget == null)
            {
                ShowMenu();
                return;
            }

            Config.Targets = [.. Config.Targets.Where(t => t != target), newTarget];

            Config = Save(Config);

            Info("Press Any Key to Back Menu...");
            Console.ReadKey(true);
            ShowMenu();
        }

        static void DeleteTarget()
        {
            Console.Clear();

            TargetConfig? target = SelectTarget(Config, "Select Target to Delete", "\e[95m", false);
            if (target == null)
            {
                ShowMenu();
                return;
            }



            if (!Confirm($"Delete target '{target.Username}@{target.IP}:{target.Port}'"))
            {
                ShowMenu();
                return;
            }

            Config.Targets = [.. Config.Targets.Where(t => t != target)];

            Config = Save(Config);

            Success("Target deleted.");
            Console.ReadKey(true);
            ShowMenu();
        }


        /// <summary>
        /// Prompt the user to edit the command template used to launch SSH and persist the updated template to the application configuration.
        /// </summary>
        /// <remarks>
        /// Supported placeholders: {@code {$IP}}, {@code {$Port}}, {@code {$Username}}. If the user cancels the prompt (provides no input), the configuration is not changed and the method returns to the main menu.
        /// </remarks>
        static void EditExec()
        {
            Console.Clear();
            Console.WriteLine("Parameter-List");
            Console.WriteLine("{$IP}\t\tConfigurated IP");
            Console.WriteLine("{$Port}\t\tConfigurated Port");
            Console.WriteLine("{$Username}\tConfigurated Username");

            string? input = Ask.String($"Current Exec ({Config.Exec})", checkEmpty: true);

            if (input == null)
            {
                ShowMenu();
                return;
            }

            Config.Exec = input.Trim();
            Config = Save(Config);

            Info("Press Any Key to Back Menu...");
            Console.ReadKey(true);
            ShowMenu();
        }

        static void ConfirmExit()
        {
            Console.Clear();

            if (Confirm("Exit"))
                Environment.Exit(0);

            ShowMenu();
        }
    }
}