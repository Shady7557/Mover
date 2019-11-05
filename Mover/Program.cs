using System;
using System.Diagnostics;
using System.IO;

namespace Mover
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("App started with full argument string: " + string.Join(" ", args));
                var moveFrom = string.Empty;
                var moveTo = string.Empty;
                var autoStart = false;
                var autoStartArgs = string.Empty;
                var wait = 0;
                var waitStart = 0;
                var exitWait = 0;
                var waitPid = 0;
                for(int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    if (arg.Equals("-autostart", StringComparison.OrdinalIgnoreCase)) autoStart = true;
                    var val = arg.Split('=')[1];
                    if (string.IsNullOrEmpty(val)) continue;
                    if (arg.Equals("-movefrom=")) moveFrom = val;
                    if (arg.Equals("-moveto=")) moveTo = val;
                    if (arg.Equals("-autostartargs=")) autoStartArgs = val;
                    if (arg.Equals("-wait=")) int.TryParse(val, out wait);
                    if (arg.Equals("-waitstart=")) int.TryParse(val, out waitStart);
                    if (arg.Equals("-exitwait=")) int.TryParse(val, out exitWait);
                    if (arg.Equals("-waitforpid=")) int.TryParse(val, out waitPid);
                }
                if (string.IsNullOrEmpty(moveFrom))
                {
                    Console.WriteLine("moveFrom arg is null/empty!");
                    return;
                }
                if (string.IsNullOrEmpty(moveTo))
                {
                    Console.WriteLine("moveTo arg is null/empty!");
                    return;
                }

                if (waitPid > 0)
                {
                    try
                    {
                        var procWatch = Stopwatch.StartNew();
                        var isRunning = true;
                        while (isRunning)
                        {
                            var procs = Process.GetProcesses();
                            var found = false;
                            for (int i = 0; i < procs.Length; i++)
                            {
                                var p = procs[i];
                                if (p?.Id == waitPid)
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
                        Console.WriteLine("Waited " + procWatch.Elapsed.TotalMilliseconds + "ms for pid " + waitPid + " to exit");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Couldn't wait for pid exit for pid: " + waitPid + Environment.NewLine + ex.ToString());
                    }
                }
                if (wait > 0)
                {
                    Console.WriteLine("Waiting " + wait + " milliseconds");
                    System.Threading.Thread.Sleep(wait);
                    Console.WriteLine("Finished waiting");
                }

                var path = moveFrom.Replace("\"", string.Empty);
                var path2 = moveTo.Replace("\"", string.Empty);
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
                    var info = new ProcessStartInfo();
                    info.FileName = path2;
                    info.WorkingDirectory = Path.GetDirectoryName(path2);
                    info.Arguments = autoStartArgs;
                    Console.WriteLine("Filename: " + info.FileName + ", working dir: " + info.WorkingDirectory + ", argu: " + info.Arguments);
                    if (waitStart > 0)
                    {
                        Console.WriteLine("Waiting to start process: " + waitStart + "ms");
                        System.Threading.Thread.Sleep(waitStart);
                    }
                    Process.Start(info);
                    var sleepExit = exitWait > 0 ? exitWait : 5000;
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
