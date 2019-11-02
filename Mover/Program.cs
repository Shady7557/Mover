using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Mover
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var fromArg = args?.Where(p => p.StartsWith("-movefrom="))?.FirstOrDefault() ?? string.Empty;
                var toArg = args?.Where(p => p.StartsWith("-moveto="))?.FirstOrDefault() ?? string.Empty;
                var autoStart = args?.Any(p => p.Equals("-autostart", StringComparison.OrdinalIgnoreCase)) ?? false;
                var asArgs = args?.Where(p => p.StartsWith("-autostartargs="))?.FirstOrDefault() ?? string.Empty;
                var waitArg = args?.Where(p => p.StartsWith("-wait="))?.FirstOrDefault() ?? string.Empty;
                var waitStartArg = args?.Where(p => p.StartsWith("-waitstart="))?.FirstOrDefault() ?? string.Empty;
                var exitWaitArg = args?.Where(p => p.StartsWith("-exitwait="))?.FirstOrDefault() ?? string.Empty;
                var waitForPidExit = args?.Where(p => p.StartsWith("-waitforpid="))?.FirstOrDefault() ?? string.Empty;
                if (string.IsNullOrEmpty(fromArg) || string.IsNullOrEmpty(toArg))
                {
                    Console.WriteLine("Move from or move to arg is empty");
                    return;
                }
                Console.WriteLine("App started with full argument string: " + string.Join(" ", args));
               // Console.WriteLine("Auto start: " + autoStart + ", " + asArgs);

                var split = fromArg.Split('=');
                var split2 = toArg.Split('=');
                if (split.Length < 2)
                {
                    Console.WriteLine("split is < 2!");
                    return;
                }
                if (split2.Length < 2)
                {
                    Console.WriteLine("split 2 is < 2");
                    return;
                }
                if (!string.IsNullOrEmpty(waitForPidExit))
                {
                    var pidSplit = waitForPidExit.Split('=');
                    if (pidSplit.Length > 1)
                    {
                        var pidStr = pidSplit[1];
                        Console.WriteLine("Got pid wait str: " + pidStr);
                        int pid;
                        if (!int.TryParse(pidStr, out pid)) Console.WriteLine("Pid string is not int: " + pidStr);
                        else
                        {
                            try
                            {
                                var procWatch = Stopwatch.StartNew();
                                var isRunning = true;
                                while(isRunning)
                                {
                                    var procs = Process.GetProcesses();
                                    var found = false;
                                    for(int i = 0; i < procs.Length; i++)
                                    {
                                        var p = procs[i];
                                        if (p?.Id == pid)
                                        {
                                            found = true;
                                            break;
                                        }
                                    }
                                    if (!found)
                                    {
                                        isRunning = false;
                                        break;
                                    }
                                    System.Threading.Thread.Sleep(250);
                                }
                                procWatch.Stop();
                                Console.WriteLine("Waited " + procWatch.Elapsed.TotalMilliseconds + "ms for pid " + pid + " to exit");
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine("Couldn't wait for pid exit for pid: " + pid + Environment.NewLine + ex.ToString());
                            }
                        }
                    }
                }
                var waitStart = 0;
                if (!string.IsNullOrEmpty(waitArg))
                {
                    var waitSplit = waitArg.Split('=');
                    if (waitSplit.Length > 1)
                    {
                        var waitStr = waitSplit[1];
                        Console.WriteLine("Got wait string: " + waitStr);
                        int wait;
                        if (!int.TryParse(waitStr, out wait))
                        {
                            Console.WriteLine("Wait string is not int: " + waitStr);

                        }
                        else
                        {
                            if (wait <= 0) Console.WriteLine("Wait time is <= 0!");
                            else
                            {
                                Console.WriteLine("Waiting " + wait + " milliseconds");
                                System.Threading.Thread.Sleep(wait);
                                Console.WriteLine("Finished waiting");
                            }

                        }
                    }
                }
                if (!string.IsNullOrEmpty(waitStartArg))
                {
                    var waitSplit = waitStartArg.Split('=');
                    if (waitSplit.Length > 1)
                    {
                        var waitStr = waitSplit[1];
                        Console.WriteLine("Got wait start string: " + waitStr);
                        int wait;
                        if (!int.TryParse(waitStr, out wait)) Console.WriteLine("Wait start string is not int: " + waitStr);
                        else waitStart = wait;
                    }
                }
                var path = split[1].Replace("\"", string.Empty);
                var path2 = split2[1].Replace("\"", string.Empty);
                if (!File.Exists(path))
                {
                    Console.WriteLine("Path doesn't exist for: " + path);
                    return;
                }
                if (File.Exists(path2))
                {
                    Console.WriteLine("File existed for move to, attempting to delete...");
                    File.Delete(path2);
                }
                else new FileInfo(path2).Directory.Create();
                Console.WriteLine("Attempting to move: " + path + " to: " + path2);

                var moveTime = Stopwatch.StartNew();

                File.Move(path, path2);

                while (!File.Exists(path2))
                {
                    Console.WriteLine("doesn't exist yet!");
                    System.Threading.Thread.Sleep(150);
                }
                moveTime.Stop();
                Console.WriteLine("Moved successfully");
                Console.WriteLine("Took: " + moveTime.Elapsed.TotalMilliseconds + "ms to move file");
                if (autoStart)
                {
                    Console.WriteLine("Auto start is true... Trying to start program after move.");
                    var asArgs1 = asArgs.Split('=');
                    var startArgs = string.Empty;
                    if (asArgs1.Length > 1)
                    {
                        startArgs = asArgs1[1].Replace("\"", string.Empty);
                        Console.WriteLine("Starting with args: " + startArgs);
                    }
                    else Console.WriteLine("No args for auto start!");
                    var info = new ProcessStartInfo();
                    info.FileName = path2;
                    info.WorkingDirectory = Path.GetDirectoryName(path2);
                    info.Arguments = startArgs;
                    Console.WriteLine("Filename: " + info.FileName + ", working dir: " + info.WorkingDirectory + ", argu: " + info.Arguments);
                    if (waitStart > 0)
                    {
                        Console.WriteLine("Waiting to start process: " + waitStart + "ms");
                        System.Threading.Thread.Sleep(waitStart);
                    }
                    Process.Start(info);
                    var sleepExit = 5000;
                    if (!string.IsNullOrEmpty(exitWaitArg))
                    {
                        var waitSplit = exitWaitArg.Split('=');
                        if (waitSplit.Length > 1)
                        {
                            var waitStr = waitSplit[1];
                            Console.WriteLine("Got exit wait string: " + waitStr);
                            int wait;
                            if (!int.TryParse(waitStr, out wait)) Console.WriteLine("Exit wait string is not int: " + waitStr);
                            else sleepExit = wait;
                        }
                    }
                    Console.WriteLine("Sleeping " + sleepExit + " before exiting.");
                    System.Threading.Thread.Sleep(sleepExit); //the program will exit when this sleep is finished. prevents errors with some programs after launch
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(ex.HResult);
            }
        }
    }
}
