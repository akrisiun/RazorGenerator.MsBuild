using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using RazorGenerator.Core;
using System.Reflection;

namespace RazorGenerator.MsBuild
{
    public class RazorCodeGen : Task
    {
        public readonly Regex _namespaceRegex = new Regex(@"($|\.)(\d)");
        public readonly List<ITaskItem> _generatedFiles = new List<ITaskItem>();

        public ITaskItem[] FilesToPrecompile { get; set; }

        [Required]
        public string ProjectRoot { get; set; }

        public string RootNamespace { get; set; }

        [Required]
        public string CodeGenDirectory { get; set; }

        [Output]
        public ITaskItem[] GeneratedFiles
        {
            get
            {
                return _generatedFiles.ToArray();
            }
        }

        public override bool Execute()
        {
            try
            {
                return this.ExecuteCore();
            }
            catch (Exception ex)
            {
                if (ex is System.Reflection.ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    foreach (var item in typeLoadException.LoaderExceptions)
                    {
                        Log.LogError("RazorGenerator err", item);
                    }
                }

                if (ex.InnerException != null)
                    ex = ex.InnerException;
                Log.LogError(ex.Message);
            }
            // return false;
            return true;
        }

        public RTaskLoggingProxy _Log;
        public new TaskLoggingHelper Log { get { return _Log ?? base.Log; } }
    }

    public class RTaskItem : ITaskItem
    {
        public System.Collections.IDictionary CloneCustomMetadata() { return null; }
        public void CopyMetadataTo(ITaskItem destinationItem) { }
        public string GetMetadata(string metadataName) { return null; }

        public string ItemSpec { get; set; }
        public int MetadataCount { get { return 0; } }

        public System.Collections.ICollection MetadataNames { get { return null; } }
        public void RemoveMetadata(string metadataName) { }
        public void SetMetadata(string metadataName, string metadataValue) { }
    }

    public class RTaskLoggingProxy : TaskLoggingHelper
    {
        public class FakeTask : ITask
        {
            public IBuildEngine BuildEngine { get; set; }
            public bool Execute()
            {
                return false;
            }

            public ITaskHost HostObject { get; set; }
        }

        public RTaskLoggingProxy(ITask taskInstance = null) : base(taskInstance ?? new FakeTask()) { }
    }

    public static class Razor
    {
        public static RazorCodeGen Start(string directory = null)
        {
            directory = directory ?? Environment.CurrentDirectory;
            var task = new RazorCodeGen();

            task._Log = new RTaskLoggingProxy();
            return task;
        }

        public static void LogMessage(this RazorCodeGen task, MessageImportance importance, string message, params object[] messageArgs)
        {
            if (task._Log != null)
                Console.WriteLine(String.Format(message, messageArgs));
            else
                task.Log.LogMessage(importance, message, messageArgs);
        }
        public static void LogError(this RazorCodeGen task, string message, params object[] messageArgs)
        {
            if (task._Log != null)
                Console.WriteLine(String.Format(message, messageArgs));
            else
                task.Log.LogError(message, messageArgs);
        }

        public static bool ExecuteCore(this RazorCodeGen task)
        {
            if (task.FilesToPrecompile == null)
            {
                var dir = Environment.CurrentDirectory;
                var listCs = Directory.EnumerateFiles(dir, "*.cshtml", SearchOption.AllDirectories);
                var numCS = listCs.GetEnumerator();
                var list = new List<ITaskItem>();
                while (numCS.MoveNext())
                    list.Add(new RTaskItem { ItemSpec = numCS.Current });

                if (list.Count > 0)
                {
                    ITaskItem[] array = new ITaskItem[list.Count];
                    list.CopyTo(array);
                    task.FilesToPrecompile = array;
                }

                if (!task.FilesToPrecompile.Any())
                    return true;
            }

            var currentDirectory = Environment.CurrentDirectory;
            task.CodeGenDirectory = task.CodeGenDirectory ?? currentDirectory + @"\CodeGen";

            string projectRoot = String.IsNullOrEmpty(task.ProjectRoot)
                ? Directory.GetCurrentDirectory() : task.ProjectRoot;

            using (var hostManager = new HostManager(projectRoot))
            {
                foreach (var file in task.FilesToPrecompile)
                {
                    string filePath = file.GetMetadata("FullPath") ?? file.ItemSpec;
                    string fileName = Path.GetFileName(filePath);
                    var projectRelativePath = GetProjectRelativePath(filePath, projectRoot);
                    string itemNamespace = task.GetNamespace(file, projectRelativePath);

                    string outputPath = Path.Combine(task.CodeGenDirectory, projectRelativePath.TrimStart(Path.DirectorySeparatorChar)) + ".cs";

                    var shortfilePath = filePath.StartsWith(currentDirectory) ? filePath.Substring(currentDirectory.Length + 1) : filePath;
                    var shortoutputPath = outputPath.StartsWith(currentDirectory) ? outputPath.Substring(currentDirectory.Length + 1) : outputPath;
                    if (!RequiresRecompilation(filePath, outputPath))
                    {
                        task.LogMessage(MessageImportance.Low, "Skipping file {0} since {1} is already up to date", shortfilePath, shortoutputPath);
                        continue;
                    }
                    EnsureDirectory(outputPath);

                    task.LogMessage(MessageImportance.Low, "Precompiling {0} at path {1}", shortfilePath, shortoutputPath);
                    var host = hostManager.CreateHost(filePath, projectRelativePath, itemNamespace);

                    bool hasErrors = false;
                    host.Error += (o, eventArgs) =>
                    {
                        if (Debugger.IsAttached)
                            Debugger.Break();

                        task.LogError("RazorGenerator unknown error", eventArgs.ErorrCode.ToString() + " msg: " + eventArgs.ErrorMessage);
                                //, helpKeyword: "", file: file.ItemSpec,
                                //     lineNumber: (int)eventArgs.LineNumber, columnNumber: (int)eventArgs.ColumnNumber,
                                //     endLineNumber: (int)eventArgs.LineNumber, endColumnNumber: (int)eventArgs.ColumnNumber,
                                //     message: eventArgs.ErrorMessage);

                        hasErrors = true;
                    };

                    try
                    {
                        // TypeUtil.GetTypeWithReflectionPermission

                        string result = host.GenerateCode();

                        if (!String.IsNullOrWhiteSpace(result)) // !hasErrors)
                        {
                            // If we had errors when generating the output, don't write the file.
                            File.WriteAllText(outputPath, result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (exception.InnerException != null)
                            exception = exception.InnerException;

                        if (Debugger.IsAttached)
                            Debugger.Break();

                        task.LogError("RazorGenerator GenerateCode error", exception.Message);

                        Console.WriteLine(exception.Message);
                        if (task._Log == null)
                            task.Log.LogErrorFromException(exception, showStackTrace: true, showDetail: true, file: null);
                        //  return false;
                    }
                    //if (hasErrors)
                    //{
                    //    return false;
                    //}

                    var taskItem = new TaskItem(outputPath);
                    taskItem.SetMetadata("AutoGen", "true");
                    taskItem.SetMetadata("DependentUpon", "fileName");

                    task._generatedFiles.Add(taskItem);
                }
            }
            return true;
        }

        /// <summary>
        /// Determines if the file has a corresponding output code-gened file that does not require updating.
        /// </summary>
        private static bool RequiresRecompilation(string filePath, string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                return true;
            }
            return File.GetLastWriteTimeUtc(filePath) > File.GetLastWriteTimeUtc(outputPath);
        }

        private static string GetNamespace(this RazorCodeGen task, ITaskItem file, string projectRelativePath)
        {
            string itemNamespace = file.GetMetadata("CustomToolNamespace");
            if (!String.IsNullOrEmpty(itemNamespace))
            {
                return itemNamespace;
            }
            projectRelativePath = Path.GetDirectoryName(projectRelativePath);
            // To keep the namespace consistent with VS, need to generate a namespace based on the folder path if no namespace is specified.
            // Also replace any non-alphanumeric characters with underscores.
            itemNamespace = projectRelativePath.Trim(Path.DirectorySeparatorChar);
            if (String.IsNullOrEmpty(itemNamespace))
            {
                return task.RootNamespace;
            }

            var stringBuilder = new StringBuilder(itemNamespace.Length);
            foreach (char c in itemNamespace)
            {
                if (c == Path.DirectorySeparatorChar)
                {
                    stringBuilder.Append('.');
                }
                else if (!Char.IsLetterOrDigit(c))
                {
                    stringBuilder.Append('_');
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }
            itemNamespace = stringBuilder.ToString();
            itemNamespace = task._namespaceRegex.Replace(itemNamespace, "$1_$2");

            if (!String.IsNullOrEmpty(task.RootNamespace))
            {
                itemNamespace = task.RootNamespace + '.' + itemNamespace;
            }
            return itemNamespace;
        }

        private static string GetProjectRelativePath(string filePath, string projectRoot)
        {
            if (filePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                return filePath.Substring(projectRoot.Length);
            }
            return filePath;
        }

        private static void EnsureDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}