using System;
using IO = System.IO;
using Task = Microsoft.Build.Utilities.Task;

namespace Dzonny.ILProj
{
    /// <summary>MSBuild task that generates references in CIL format</summary>
    public class ILReferences : Task
    {
        /// <summary>Gets or sets assembly references</summary>
        public string[] References { get; set; }
        /// <summary>Gets or sets path of file where to write the references</summary>
        public string Target { get; set; }

        /// <summary>Executes a task.</summary>
        /// <returns>true if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            if (!IO.Directory.Exists(IO.Path.GetDirectoryName(Target)))
                IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(Target));
            using (var str = IO.File.Open(Target, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.Read))
            {
                using (var sw = new IO.StreamWriter(str))
                {
                    foreach (var reference in References)
                    {
                        sw.WriteLine($".assembly extern {reference} {{ }}");
                    }
                }
            }
            return true;
        }
    }
}           