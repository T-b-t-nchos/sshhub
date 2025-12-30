using System.Diagnostics;
using static sshhub.Action;
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
                SaveConfig(Config);
            }
            else
            {
                Config = ReLoadConfig();
            }

            Console.Clear();
            ShowMenu();
        }

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
                "\e[91m" + "$ " + "0. Exit"
            ];

            int selected = SelectableMenu(menuItems, 4, false);
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

            TargetConfig? target = SelectTarget(Config, "Select Target to Connect", "\e[92m");
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
                    ShowConfig(Config);
                    break;
            }

            Console.WriteLine();

            Info("Press any key to return to the menu...");
            Console.ReadKey(true);
            ShowMenu();
        }

        static void AddTarget()
        {
            Console.Clear();

            var newTarget = EditTargetConfig(
                target: null,
                allTargets: Config.Targets,
                isNew: true
            );

            if (newTarget == null)
            {
                ShowMenu();
                return;
            }

            Config.Targets = [.. Config.Targets, newTarget];

            Config = SaveConfig(Config);

            Console.ReadKey(true);
            ShowMenu();
        }

        static void EditTarget()
        {
            Console.Clear();

            TargetConfig? target = SelectTarget(Config, "Select Target to edit", "\e[93m");
            if (target == null)
            {
                ShowMenu();
                return;
            }

            EditTargetConfig(
                target,
                Config.Targets,
                isNew: false
            );

            if (target == null)
            {
                ShowMenu();
                return;
            }

            Config = SaveConfig(Config);

            Info("Press Any Key to Back Menu...");
            Console.ReadKey(true);
            ShowMenu();
        }

        static void DeleteTarget()
        {
            Console.Clear();

            TargetConfig? target = SelectTarget(Config, "Select Target to Delete", "\e[95m");
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

            Config = SaveConfig(Config);

            Success("Target deleted.");
            Console.ReadKey(true);
            ShowMenu();
        }


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
            Config = SaveConfig(Config);

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
