using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Task = Microsoft.Build.Utilities.Task;

namespace Azure.MsBuild.Tasks
{
    public class CreateNewDatabaseContext : Task
    {
        [Required]
        public string AzureModulePath { get; set; }

        [Required]
        public string PublishSettingsFile { get; set; }

        [Required]
        public string SubscriptionName { get; set; }
        
        [Required]
        public string WebsiteName { get; set; }

        [Required]
        public string AzureDatabaseServer { get; set; }

        [Required]
        public string DatabaseName { get; set; }

        [Required]
        public string WorkerRoleConfig { get; set; }

        [Required]
        public string Slot { get; set; }

        [Required]
        public string WorkerRoleName { get; set; }

        [Required]
        public string DatabaseAdmin { get; set; }
        [Required]
        public string DatabasePassword { get; set; }

        public string ExecutionPolicy { get; set; }
        private System.IO.DirectoryInfo tempFolder;
        private string AzureDbCreateScript = "DatabaseCreate.ps1";
        private string AzureDbCreateScriptFullPath = null;
        private string AzureDeployCommandFormat =
            "-Command \"Import-Module '{0}'; {1} -SubscriptionName '{2}' -PublishSettingsFile '{3}' -WebsiteName '{4}' -AzureDatabaseServer '{5}' -DatabaseName '{6}' -WorkerRoleConfig '{7}' -Slot '{8}' -WorkerRoleName {9} -DatabaseAdmin '{10}' -DatabasePassword '{11}'\"";
        private string AzureDeployCommand;

        private string DeployCommand = "CreateNewDB";
        private string ProgramFilesPath = null;
        public string PowerShellLocation { get; set; }
        public string AzurePowerShellLocation { get; set; }

        public CreateNewDatabaseContext()
        {
            var systemRoot = System.Environment.GetEnvironmentVariable("SystemRoot");
            PowerShellLocation = System.IO.Path.Combine(systemRoot, @"System32\WindowsPowerShell\v1.0\powershell.exe");
            ProgramFilesPath = System.Environment.GetEnvironmentVariable("ProgramFiles(x86)");

            tempFolder = new DirectoryInfo(System.Environment.GetEnvironmentVariable("temp"));
            if (!tempFolder.Exists) throw new FileNotFoundException("Couldnt resolve the temp path, aborting");

            AzureDbCreateScriptFullPath = System.IO.Path.Combine(tempFolder.FullName, AzureDbCreateScript);

#if DEBUG
            if (System.IO.File.Exists(AzureDbCreateScript)) System.IO.File.Delete(AzureDbCreateScriptFullPath);
#endif
            if (!System.IO.File.Exists(AzureDbCreateScriptFullPath))
            {
                var script = GetEmbeddedResource(AzureDbCreateScript, typeof(PublishWorkerRole).Assembly);
                if (!string.IsNullOrEmpty(AzureDbCreateScriptFullPath)) System.IO.File.WriteAllText(AzureDbCreateScriptFullPath, script);
            }

        }

        public override bool Execute()
        {
            Log.LogMessage("Starting Creation of new database");
            AzurePowerShellLocation = System.IO.Path.Combine(ProgramFilesPath, AzureModulePath);
            if (!System.IO.File.Exists(PowerShellLocation))
            {
                Log.LogErrorFromException(
                    new FileNotFoundException("Could not resolve powershell, current location is set to " +
                                              PowerShellLocation +
                                              ", use 'PowerShellLocation' msbuild parameter on the task to set a custom path."));
                return false;
            }

            AzureDeployCommand = string.Format(AzureDeployCommandFormat, AzurePowerShellLocation, AzureDbCreateScriptFullPath, SubscriptionName, PublishSettingsFile, WebsiteName, AzureDatabaseServer, DatabaseName, WorkerRoleConfig, Slot, WorkerRoleName, DatabaseAdmin, DatabasePassword);
            
            Console.WriteLine(string.Format("Executing powershell command:\r\n{0} {1}", PowerShellLocation, AzureDeployCommand));

            if (!string.IsNullOrEmpty(ExecutionPolicy))
            {
                Log.LogMessage("Setting PowerShell ExecutionPolicy To:" + ExecutionPolicy);
                System.Diagnostics.Process.Start(PowerShellLocation, "Set-ExecutionPolicy " + ExecutionPolicy).WaitForExit();
            }
            Log.LogMessage("Executing Azure publishing steps. This may take a while...");

            ProcessStartInfo psi = new ProcessStartInfo(PowerShellLocation, AzureDeployCommand);
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            var p = System.Diagnostics.Process.Start(psi);
            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();
            p.WaitForExit();


            Console.WriteLine("-------------------DEPLOYMENT OUTPUT---------------------------");
            Log.LogMessage(output);
            Console.WriteLine("-------------------/DEPLOYMENT OUTPUT---------------------------");
            Console.WriteLine(string.Format("Exit Code:{0}", p.ExitCode));
            if (p.ExitCode != 0)
            {
                Console.WriteLine("-------------------DEPLOYMENT ERROR---------------------------");
                Log.LogError(error);
                Console.WriteLine("-------------------/DEPLOYMENT ERROR---------------------------");
                return false;
            }
            return true;
        }

        public static string GetEmbeddedResource(string resourceName, System.Reflection.Assembly assembly)
        {
            resourceName = FormatResourceName(assembly, resourceName);
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                    return null;

                using (StreamReader reader = new StreamReader(resourceStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        private static string FormatResourceName(System.Reflection.Assembly assembly, string resourceName)
        {
            return assembly.GetName().Name + "." + resourceName.Replace(" ", "_")
                                                               .Replace("\\", ".")
                                                               .Replace("/", ".");
        }
    }
}
