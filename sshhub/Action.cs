using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace sshhub
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ConfigRoot))]
    [JsonSerializable(typeof(TargetConfig))]
    [JsonSerializable(typeof(TargetConfig[]))]
    internal partial class ConfigJsonContext : JsonSerializerContext
    {
    }

    public class Action
    {

        public class Ask
        {
            public static string? String(string prompt, bool checkEmpty)
            {
                while (true)
                {
                    Console.Write($"{prompt} >> ");
                    string input = Console.ReadLine() ?? string.Empty;

                    if (input.Equals("!cancel", StringComparison.CurrentCultureIgnoreCase))
                        return null;

                    if (!string.IsNullOrWhiteSpace(input))
                        return input;

                    else if (!checkEmpty)
                        return input;

                    WriteLine.Error("Input cannot be empty. Please try again. (Cancel to \"!cancel\" or \"!CANCEL\")");
                }
            }

            public static int? Int(string prompt, int defaultValue)
            {
                while (true)
                {
                    Console.Write($"{prompt} >> ");

                    string input = Console.ReadLine() ?? string.Empty;

                    if (input.Equals("!cancel", StringComparison.CurrentCultureIgnoreCase))
                        return null;

                    if (int.TryParse(input, out int result) && !string.IsNullOrWhiteSpace(input))
                        return result;

                    else if (defaultValue != -1)
                        return defaultValue;


                    WriteLine.Error("Invalid integer. Please try again. (Cancel to \"!cancel\" or \"!CANCEL\")");
                }
            }

            public static bool? Bool(string prompt, bool? defaultValue)
            {
                while (true)
                {
                    Console.Write($"{prompt} (y/n) >> ");

                    string input = Console.ReadLine() ?? string.Empty;

                    if (input.Equals("!cancel", StringComparison.CurrentCultureIgnoreCase))
                        return null;

                    if (input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    else if (input.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                        return false;

                    else if (input.Trim() == string.Empty && defaultValue != null)
                        return defaultValue;

                    WriteLine.Error("Invalid input. Please enter 'y' or 'n'. (Cancel to \"!cancel\" or \"!CANCEL\")");
                }
            }
        }

        public class WriteLine
        {
            public static void HBar()
            {
                Console.WriteLine(new string('=', Console.WindowWidth - 10));
            }

            public static void HalfHBar()
            {
                Console.WriteLine(new string('-', Console.WindowWidth - 10));
            }

            public static void Info(string message)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            public static void Success(string message)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            public static void Warning(string message)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            public static void Error(string message)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
            }

            /// <summary>
            /// Prompts the user to confirm an action with a yes/no question in the console.
            /// </summary>
            /// <remarks>The console is cleared before displaying the confirmation prompt. Any input
            /// other than 'y' is treated as a cancellation.</remarks>
            /// <param name="msg">The message to display to the user describing the action to be confirmed.</param>
            /// <returns>true if the user enters 'y' (case-insensitive) to confirm; otherwise, false.</returns>
            public static bool Confirm(string msg)
            {
                Console.Clear();
                Warning($"{msg}, Confirm?");
                Console.Write("y=Yes / Other=Cancel >> ");

                var input = Console.ReadLine();
                if (input != null && input.Equals("y", StringComparison.CurrentCultureIgnoreCase))
                    return true;

                return false;
            }

            /// <summary>
            /// Displays a selectable menu in the console and allows the user to choose an option using the keyboard.
            /// </summary>
            /// <remarks>The menu allows navigation using the Up and Down arrow keys. Pressing Enter
            /// selects the currently highlighted option. If numeric shortcuts are enabled, pressing a number key (1–9)
            /// selects the corresponding option directly. The method blocks until the user makes a selection or
            /// cancels.</remarks>
            /// <param name="options">An array of strings representing the menu options to display. Each element corresponds to a selectable
            /// item.</param>
            /// <param name="top">The zero-based row position in the console at which to display the menu.</param>
            /// <param name="enableshotcut">true to enable numeric shortcut keys (1–9) for direct selection of menu options; otherwise, false.</param>
            /// <returns>The zero-based index of the selected option, or -1 if the user presses the Escape key to cancel the
            /// selection.</returns>
            public static int SelectableMenu(string[] options, int top, bool enableshotcut)
            {
                int selectedIndex = 0;

                while (true)
                {
                    Console.SetCursorPosition(0, top);
                    for (int i = 0; i < options.Length; i++)
                    {
                        Console.WriteLine(
                            options[i].Replace("$", i == selectedIndex ? "\e[7m>" : " ") + "\e[0m");
                    }

                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.UpArrow:
                            selectedIndex = (selectedIndex - 1 + options.Length) % options.Length;
                            break;
                        case ConsoleKey.DownArrow:
                            selectedIndex = (selectedIndex + 1) % options.Length;
                            break;
                        case ConsoleKey.Enter:
                            return selectedIndex;

                        case ConsoleKey.Escape:
                            return -1;

                        case ConsoleKey.D1 when enableshotcut: return 0;
                        case ConsoleKey.D2 when enableshotcut: return 1;
                        case ConsoleKey.D3 when enableshotcut: return 2;
                        case ConsoleKey.D4 when enableshotcut: return 3;
                        case ConsoleKey.D5 when enableshotcut: return 4;
                        case ConsoleKey.D6 when enableshotcut: return 5;
                        case ConsoleKey.D7 when enableshotcut: return 6;
                        case ConsoleKey.D8 when enableshotcut: return 7;
                        case ConsoleKey.D9 when enableshotcut: return 8;
                    }
                }
            }
        }

        public class TCP
        {
            public static async Task<TCPState> CanConnectAsync(
                string host,
                int port = 22,
                int timeoutMs = 600)
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(timeoutMs);

                try
                {
                    await client.ConnectAsync(host, port, cts.Token);
                    return TCPState.Online;
                }
                catch (OperationCanceledException)
                {
                    // Timeout
                    return TCPState.Offline;
                }
                catch (SocketException)
                {
                    // Connection refused, host unreachable, etc.
                    return TCPState.Offline;
                }
                catch (Exception)
                {
                    // DNS failures, unexpected errors
                    return TCPState.Error;
                }
            }

            public enum TCPState
            {
                Online,
                Offline,
                Error,
                None
            }
        }

        public class Config
        {
            /// <summary>
            /// Displays an interactive menu of configured targets and lets the user choose one.
            /// </summary>
            /// <param name="config">The configuration containing the available targets.</param>
            /// <param name="infoMsg">Header text shown above the menu to provide context to the user.</param>
            /// <param name="toptext">Prefix text added to each menu entry before the target details.</param>
            /// <param name="scanOnline">If true, the method checks the online status of each target.</param>
            /// <returns>The chosen <see cref="TargetConfig"/>, or <c>null</c> if the user cancels the selection or no targets are available.</returns>
            public static TargetConfig? SelectTarget(ConfigRoot config, string infoMsg, string toptext, bool scanOnline)
            {
                Console.Clear();

                WriteLine.Info($"{infoMsg} (Press ESC to cancel)");
                WriteLine.Info("You can choose Up/Down Allow or Number 1to9");

                if (config.Targets.Length == 0)
                {
                    WriteLine.Error("\e[7m> No targets available.");
                    Console.WriteLine();
                    WriteLine.Info("Press any key to return to the menu...");
                    Console.ReadKey(true);
                    return null;
                }

                if (scanOnline)
                    WriteLine.Info("\e[7m> Scanning online status, please wait...");

                string[] items = [];
                foreach (var t in config.Targets)
                {
                    Array.Resize(ref items, items.Length + 1);

                    bool doScanOnline = scanOnline && t.ScanOnline;
                    TCP.TCPState status = TCP.TCPState.None;
                    string statusColor = string.Empty;
                    if (doScanOnline)
                    {
                        status = TCP.CanConnectAsync(t.IP, t.Port).GetAwaiter().GetResult();
                        statusColor = status switch
                        {
                            TCP.TCPState.Offline => "\e[91m",
                            TCP.TCPState.Error => "\e[31m",
                            _ => ""
                        };
                    }
                    items[^1] = toptext + statusColor + "$ " +
                        $"ID: {t.id}, Name: {t.Name}, {t.Username} @{t.IP} :{t.Port}{(doScanOnline ? $", {status}" : "")}";
                }

                int selected = WriteLine.SelectableMenu(items, 2, true);

                if (selected + 1 > config.Targets.Length)
                {
                    return SelectTarget(config, infoMsg, toptext, scanOnline);
                }
                if (selected == -1)
                {
                    return null;
                }

                return config.Targets[selected];
            }

            public static TargetConfig? EditTargetConfig(TargetConfig? target, TargetConfig[] allTargets, bool isNew)
            {
                target ??= new TargetConfig();

                while (true)
                {
                    int? newId = Ask.Int(isNew ? "Enter Target ID" : $"Current ID ({target.id})", isNew ? -1 : target.id);

                    if (newId == null)
                        return null;

                    bool duplicate = allTargets
                        .Where(t => !ReferenceEquals(t, target))
                        .Any(t => t.id == newId);

                    if (duplicate)
                    {
                        WriteLine.Error("Duplicate ID.");
                        continue;
                    }

                    target.id = (int)newId;
                    break;
                }


                string? newName = Ask.String(
                    isNew ? "Enter Target Name" : $"Current Name ({target.Name})",
                    checkEmpty: isNew
                );
                if (newName == null)
                    return null;
                else if (newName != string.Empty)
                    target.Name = newName;


                string? newIP = Ask.String(
                    isNew ? "Enter Target IP (or HostName)" : $"Current IP ({target.IP})",
                    checkEmpty: isNew
                );
                if (newIP == null)
                    return null;
                else if (newIP != string.Empty)
                    target.IP = newIP;


                int? newPort = Ask.Int(
                    isNew ? "Enter Target Port (default 22)" : $"Current Port ({target.Port})",
                    isNew ? 22 : target.Port
                );
                if (newPort == null)
                    return null;
                target.Port = (int)newPort;


                string? newUsername = Ask.String(
                    isNew ? "Enter Target Username" : $"Current Username ({target.Username})",
                    checkEmpty: isNew
                );
                if (newUsername == null)
                    return null;
                else if (newUsername != string.Empty)
                    target.Username = newUsername;


                bool? newScanOnline = Ask.Bool(
                    isNew ? "Scan Online Status?" : $"Current Scan Online Status ({(target.ScanOnline ? "y" : "n")})",
                    isNew ? null : target.ScanOnline
                );
                if (newScanOnline == null)
                    return null;
                target.ScanOnline = (bool)newScanOnline;


                return target;
            }

            public static ConfigRoot ReLoad()
            {
                if (!File.Exists(Program.CONFIGPATH))
                    return new ConfigRoot();

                string jsonText = File.ReadAllText(Program.CONFIGPATH);

                return JsonSerializer.Deserialize(jsonText, ConfigJsonContext.Default.ConfigRoot) ?? new ConfigRoot();
            }

            static string GetJsonFromConfig(ConfigRoot config)
            {
                config.Targets = [.. config.Targets.OrderBy(t => t.id)];

                return JsonSerializer.Serialize(
                    config,
                    ConfigJsonContext.Default.ConfigRoot
                );
            }

            public static void Show(ConfigRoot config)
            {
                string json = GetJsonFromConfig(config);
                Console.WriteLine("Current Configuration:");
                Console.WriteLine(json);
            }

            public static ConfigRoot Save(ConfigRoot config)
            {
                string json = GetJsonFromConfig(config);

                WriteLine.Success("Done!!");
                Console.WriteLine("Saved Configuration:");
                Console.WriteLine(json);

                File.WriteAllText(Program.CONFIGPATH, json);
                return config;
            }
        }


        public static void WriteTargets(TargetConfig[] targets)
        {
            Console.WriteLine("Targets:");
            foreach (var t in targets)
            {
                Console.WriteLine(
                    $"ID: {t.id}, Name: {t.Name}, IP: {t.IP}, Port: {t.Port}, Username: {t.Username}, ScanOnline: {t.ScanOnline}"
                );
            }
        }
    }
}