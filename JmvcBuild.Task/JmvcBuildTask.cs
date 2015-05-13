using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;

namespace JmvcBuild.Task
{
    public class JmvcBuildTask: Microsoft.Build.Utilities.Task
    {
        [Required]
        public string JmvcProjectName { get;set; }

        [Required]
        public string Root { get; set; }

        public override bool Execute()
        {
            Directory.SetCurrentDirectory(Root);

            try
            {
                var info = new ProcessStartInfo("js.bat", JmvcProjectName + @"\scripts\build.js");
                info.RedirectStandardOutput = true;
                info.CreateNoWindow = true;
                info.UseShellExecute = false;

                using (var process = new Process())
                {
                    process.StartInfo = info;
                    process.Start();

                    var parsing = false;

                    var message = string.Empty;
                    var fileName = string.Empty;
                    var name = string.Empty;
                    var lineNumber = string.Empty;

                    var error = false;
                    var output = process.StandardOutput;
                    while (true)
                    {
                        var line = output.ReadLine();
                        if (line == null) break;

                        if (line == "!!!!!!!!!!! ERROR !!!!!!!!!!!")
                        {
                            parsing = true;
                            error = true;

                            message = string.Empty;
                            fileName = string.Empty;
                            name = string.Empty;
                            lineNumber = string.Empty;
                        }
                        Log.LogMessage(line);

                        if (parsing)
                        {
                            if (line.StartsWith("-message")) message = GetValue(line);
                            if (line.StartsWith("-fileName")) fileName = GetValue(line);
                            if (line.StartsWith("-name")) name = GetValue(line);
                            if (line.StartsWith("-lineNumber")) lineNumber = GetValue(line);

                            if (!string.IsNullOrEmpty(message) &&
                                !string.IsNullOrEmpty(fileName) &&
                                !string.IsNullOrEmpty(name) &&
                                !string.IsNullOrEmpty(lineNumber))
                            {
                                Log.LogError(
                                    "JMVC Build '{0}' has been failed with error '{1}', {2}, line: {3}",
                                    JmvcProjectName, message, fileName, lineNumber);
                                parsing = false;
                            }
                        }
                    }

                    process.WaitForExit();

                    return process.ExitCode == 0 && !error;
                }
            }
            catch (Exception)
            {
                Log.LogError("Can't build JMVC project.");
                return false;
            }
        }

        private static string GetValue(string line)
        {
            var position = line.IndexOf('=');
            return line.Substring(position + 1).Trim();
        }
    }
}
