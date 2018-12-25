using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Dezhnev.ConsoleClient.Edsm;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dezhnev.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = LoadConfig();
            var commandFactory = new CommandFactory(config);
            while (true)
            {
                Console.Write("dzhnv>");
                var commandText = Console.ReadLine();
                var command = commandFactory.GetCommand(commandText);
                var exit = command.Proceed();
                if (exit)
                    return;
            }
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        private static DezhnevConfig LoadConfig()
        {
            var defaultConfigPath = $"{AssemblyDirectory}\\default-config.json";
            var userConfigPath = $"{AssemblyDirectory}\\user-config.json";

            if (!File.Exists(defaultConfigPath))
            {
                File.WriteAllText(defaultConfigPath, JsonConvert.SerializeObject(DezhnevConfig.Default));
            }

            if (!File.Exists(userConfigPath))
            {
                File.WriteAllText(userConfigPath, "{}");
            }

            var defaultConfigObject = JObject.Parse(File.ReadAllText(defaultConfigPath));
            var userConfigObject = JObject.Parse(File.ReadAllText(userConfigPath));
            var resultConfig = new JObject();

            foreach (var defaultProperty in defaultConfigObject.Properties())
            {
                var userProperty = userConfigObject.Property(defaultProperty.Name);
                if (userProperty == null)
                {
                    resultConfig.Add(defaultProperty.Name, defaultProperty.Value);
                }
                else
                {
                    resultConfig.Add(userProperty.Name, userProperty.Value);
                }
            }

            return resultConfig.ToObject<DezhnevConfig>();
        }
    }

    internal class DezhnevConfig
    {
        public static DezhnevConfig Default => new DezhnevConfig
        {
            OutputDir = ""
        };

        public string OutputDir { get; set; }
    }

    internal class CommandFactory
    {
        private readonly DezhnevConfig config;

        public CommandFactory(DezhnevConfig config)
        {
            this.config = config;
        }

        public ICommand GetCommand(string commandText)
        {
            var commandName = commandText.Split(' ').FirstOrDefault();
            switch (commandName)
            {
                case "sphere":
                    return new SphereCommand(config, commandText);
                case "help":
                    return new HelpCommand();
                case "exit":
                    return new ExitCommand();
                default:
                    return new UnknownCommand(commandName);
            }
        }

        public static List<ICommand> GetAllCommands()
        {
            return new List<ICommand>
            {
                new ExitCommand(),
                new HelpCommand()
            };
        }
    }

    internal class SphereCommand : ICommand
    {
        private bool wrongInput;
        private readonly DezhnevConfig config;
        private string commandText;
        private Point center;
        private int radius = 100;
        private EdsmPort edsm;

        public SphereCommand(DezhnevConfig config, string commandText)
        {
            this.config = config;
            this.commandText = commandText;
            var regex = new Regex(@"^(?<command>\w+) (?<x>-?\d+) (?<y>-?\d+) (?<z>-?\d+) (?<radius>\d+)?$");
            var match = regex.Match(commandText);
            if (!match.Success)
                wrongInput = true;
            center = new Point(int.Parse(match.Groups["x"].Value), int.Parse(match.Groups["y"].Value),
                int.Parse(match.Groups["z"].Value));
            var radiusString = match.Groups["radius"].Value;
            if (!string.IsNullOrEmpty(radiusString))
                radius = int.Parse(radiusString);

            edsm = new EdsmPort();
        }

        public bool Proceed()
        {
            if (wrongInput)
            {
                Console.WriteLine("There is an error in the input. Please try 'help' command for more information");
                return false;
            }

            Console.Write("Sphere: Getting stars from EDSM... ");
            var stars = edsm.GetSphereSystemsAsync(center, radius).Result;
            Console.WriteLine("Sphere: Done!");
            Console.Write("Sphere: Creating file for import... ");
            File.WriteAllText($"{config.OutputDir}\\ImportStars.txt", string.Join(Environment.NewLine, stars) + Environment.NewLine);
            Console.WriteLine("Done!");
            return false;
        }

        public string ShortHelp =>
            $"sphere <x> <y> <z> [<radius>] - get all stars in the specified sphere.{Environment.NewLine}" +
            $"* <x>, <y>, <z> - integers - Coordinates of the sphere center.{Environment.NewLine}" +
            "* <radius> - integer in range [0 to 200], 100 by default - Radius of the sphere.";
    }

    internal class HelpCommand : ICommand
    {
        public bool Proceed()
        {
            var commands = CommandFactory.GetAllCommands();
            foreach (var command in commands)
            {
                Console.WriteLine(command.ShortHelp);
            }

            return false;
        }

        public string ShortHelp => $"help - show this help";
    }

    internal class ExitCommand : ICommand
    {
        public bool Proceed()
        {
            return true;
        }

        public string ShortHelp => "exit - close application";
    }
}

internal class UnknownCommand : ICommand
{
    private readonly string commandName;

    public UnknownCommand(string commandName)
    {
        this.commandName = commandName;
    }

    public bool Proceed()
    {
        Console.WriteLine($"Unknown command {commandName}. Please use 'help'.");
        return false;
    }

    public string ShortHelp => "";
}

internal interface ICommand
{
    bool Proceed();
    string ShortHelp { get; }
}
