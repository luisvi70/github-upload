using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Belkin.WeMo;
using System.Net;
using System.IO;

namespace Program
{
    class Program
    {
        public enum Action
        {
            On,
            Off
        }

        /// <summary>
        /// Display usage information to end user
        /// </summary>
        public static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("WeMo.exe /device {device_name} /action {action_name} [/q] [/?]");
            Console.WriteLine(" /device  - The name you gave the device (ex. WeMo Light)");
            Console.WriteLine(" /action  - The action to perform (ex. ON -or- OFF)");
            Console.WriteLine(" /query   - Lists active WeMo Devices on Network");
            Console.WriteLine();
        }

        /// <summary>
        /// Entry point for WeMo Device Controller
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            Console.WriteLine("WeMo Device Controller v{0} - by: Barnacules", System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString(2));
            Console.WriteLine("Come Visit Me @ Barnacules Nerdgasm YouTube Channel");
            Console.WriteLine("http://youtube.com/barnacules1");
            Console.WriteLine();

            Action? targetAction = null;
            string targetDevice = String.Empty;
            bool queryOnly = false;

            //
            //  Show usage if no arguments are passed
            //
            if (args.Length == 0)
            {
                PrintUsage();
                return 0;
            }

            //
            //  Process command line arguments
            //
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "/d":
                    case "/device":
                    case "-d":
                    case "-device":
                        targetDevice = args[++i];    
                    break;

                    case "/a":
                    case "/action":
                    case "-a":
                    case "-action":
                        try
                        {
                            targetAction = (Action)Enum.Parse(typeof(Action), args[++i], true);
                        }
                        catch
                        {
                            Console.WriteLine("{0} is not a valid action", args[i]);
                            Console.WriteLine("Supported Actions");
                            Console.WriteLine("=================");
                            foreach( var action in Enum.GetNames(typeof(Action)))
                            {
                                Console.WriteLine("* {0}", action);
                            }
                            return 1;
                        }
                        break;

                    case "/q":
                    case "/query":
                    case "-q":
                    case "-query":
                        queryOnly = true;
                        break;

                    case "/?":
                    case "/help":
                    case "-?":
                    case "-help":
                        PrintUsage();
                        return 0;

                    default:
                        Console.WriteLine("Bad argument {0}", args[i]);
                        return 1;
                }
            }

            //
            //  Set query only mode if no meaningful arguments are passed
            //
            if (targetAction == null && targetDevice == String.Empty)
            {
                queryOnly = true;
            }

            //
            //  Display information on located WeMo Devices
            //
            if (queryOnly)
            {
                DateWriteLine("Searching...");
                List<WeMoDevice> devices = WeMoDevice.GetDevices();

                if (devices.Count > 0 && queryOnly)
                {
                    DateWriteLine("\rFound Device(s)        ");
                    DateWriteLine("=========================");
                    foreach (var device in devices)
                    {
                        DateWriteLine("Name: {0}", device.Name);
                    }
                }
                else
                {
                    DateWriteLine("\rFailed to find WeMo devices on your network");
                }
                return 0;
            }

            //
            //  Query for WeMo switch by name
            //
            DateWriteLine("Searching for device {0}...", targetDevice);
            WeMoSwitch wemoSwitch = (WeMoSwitch)WeMoDevice.GetDevice(targetDevice);

            //
            //  Perform action
            //
            switch (targetAction)
            {
                case Action.On:
                    wemoSwitch.On();
                    DateWriteLine("The switch has been turned on");
                    break;

                case Action.Off:
                    wemoSwitch.Off();
                    DateWriteLine("The switch has been turned off");
                    break;
            }

            //
            //  Goodbye! :)
            //
            return 0;
        }

        /// <summary>
        /// Time stamping console output
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void DateWriteLine(string message, params object [] args)
        {
            Console.WriteLine("[{0}] {1}", DateTime.Now.ToShortTimeString(), String.Format(message, args));
        }
    }
}
