using System;
using static System.Globalization.CultureInfo;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using IO = System.IO;

namespace Dzonny.ILProj
{
    /// <summary>Wraps CIL compiler (ilasm) as MSBuild task</summary>
    public class ILAsm : Task
    {
        /// <summary>Gets or sets path to CIL compiler</summary>
        /// <value>By default points to relative path of ilasm.exe depending on ilasm.exe being in PATH</value>
        public string ILAsmExe { get; set; } = "ilasm.exe";

        /// <summary>Gets or sets value indcating if progress from assemly is supressed</summary>
        /// <value>True not to report progress. False to report progress</value>
        [DefaultValue(false)]
        public bool Quiet { get; set; }

        /// <summary>Gets or sets value indicating if automatic inheriting form <see cref="System.Object"/> is disabled by default</summary>
        [DefaultValue(false)]
        public bool NoAutoInherit { get; set; }

        /// <summary>Gets or sets type of targe binary</summary>
        /// <value>
        /// <list type="table">
        /// <listheader><term>Value</term><description>Description</description></listheader>
        /// <item>EXE<term></term><description>Console application (default)</description></item>
        /// <item><term>DLL</term><description>DLL library</description></item>
        /// </list>
        /// </value>
        [DefaultValue("EXE")]
        public string Target { get; set; } = "EXE";

        /// <summary>Gets or sets value indicating if PDB is generated /even without debug infor tracking)</summary>
        [DefaultValue(false)]
        public bool Pdb { get; set; }

        /// <summary>Gets or sets value indicating if AppContainer exe or dll is created</summary>
        [DefaultValue(false)]
        public bool AppContainer { get; set; }

        private DebugOption debug;
        /// <summary>Gets or sets option of generationg debugging information</summary>
        /// <value>One of <see cref="DebugOption"/> values</value>
        [DefaultValue(DebugOption.NoDebug)]
        public string Debug
        {
            get { return debug.ToString(); }
            set { debug = (DebugOption)Enum.Parse(typeof(DebugOption), value, true); }
        }

        /// <summary>Gets or sets value indicating if long instructions are optimized to short ones</summary>
        [DefaultValue(false)]
        public bool Optimize { get; set; }

        /// <summary>Gets or sets value indicating if identical method bodies are folded to one</summary>
        [DefaultValue(false)]
        public bool Fold { get; set; }

        /// <summary>Gets or sets value indicating if compilation time is measured and reported</summary>
        [DefaultValue(false)]
        public bool Clock { get; set; }

        /// <summary>Gets or sets resource files to ling to resulting DLL or EXE</summary>
        [DefaultValue(null)]
        public string[] Resources { get; set; }

        /// <summary>Gets or sets name and path (including extension) of resulting file</summary>
        [DefaultValue(null)]
        public string Output { get; set; }

        /// <summary>Gets or sets name and path of file or name of key source to compile with strong name</summary>
        /// <value>Provide either path or name of file, or prefixed with at (@) privete key source</value>
        [DefaultValue(null)]
        public string Key { get; set; }

        /// <summary>Gets or sets paths to search for <c>#include</c>d files</summary>
        [DefaultValue(null)]
        public string[] Includes { get; set; }

        private int? subsystem;
        /// <summary>Gets or sets subsystem value in the NT optional header</summary>
        public int SubSystem { get { return subsystem ?? 0; } set { subsystem = value; } }

        /// <summary>Gets or sest subsystem version in the NT optional header</summary>
        /// <value>Version in format &lt;int>.&lt;int></value>
        public string SubsystemVersion { get; set; }

        private int? flags;
        /// <summary>Gets or sets CLR ImageFlags value in the CLR header</summary>
        public int Flags { get { return flags ?? 0; } set { flags = value; } }

        private int? alignment;
        /// <summary>Gets or sets FileAlignment value in the NT optional header</summary>
        public int Alignment { get { return alignment ?? 0; } set { alignment = value; } }

        private int? @base;
        /// <summary>Gets or sets ImageBase value in the NT Optional header</summary>
        /// <remarks>Max 2GB for 32-bit images</remarks>
        public int Base { get { return @base ?? 0; } set { @base = value; } }

        private int? stack;
        /// <summary>Gets or sets SizeOfStackReserve value in the NT optional header</summary>
        public int Stack { get { return stack ?? 0; } set { stack = value; } }

        /// <summary>Gets or sets metadata version string</summary>
        [DefaultValue(null)]
        public string MetadataVersion { get; set; }

        /// <summary>Gets or sets metadata stream version</summary>
        /// <value>Version in format &lt;int>.&lt;int></value>
        [DefaultValue(null)]
        public string MetadataStreamVersion { get; set; }

        /// <summary>Gets or sets value indicating wether to create 64-bit image (PE32+)</summary>
        [DefaultValue(false)]
        public bool PE64 { get; set; }

        /// <summary>Gets or sets High Entropy Virtual Address capable PE32+ images</summary>
        /// <remarks>Set anyway if <see cref="AppContainer"/> is true</remarks>
        public bool HighEntropyVirtualAddress { get; set; }

        /// <summary>Gets or sets value indicating if generation of CORExeMain stub is suppressed</summary>
        public bool NoCorStub { get; set; }

        /// <summary>Gets or sets value indicating that no base relocations are needed</summary>
        public bool StripRelocations { get; set; }

        /// <summary>Gets or sets target CPU platform</summary>
        /// <value>Allowed values are: ITANIUM, X64, ARM</value>
        [DefaultValue(null)]
        public string TargetCpu { get; set; }

        /// <summary>Gets or sets value indicating that 36BitPreffered image (PE32) is created</summary>
        public bool Prefer32Bit { get; set; }

        /// <summary>Gets or sets source files to create edit-and-continue delats from</summary>
        [DefaultValue(null)]
        public string[] EditAndContinueDeltas { get; set; }

        /// <summary>Gets or sets source files</summary>
        [DefaultValue(null), Required]
        public string[] Files { get; set; }

        /// <summary>Executes a task.</summary>
        /// <returns>true if the task executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            using (var ilasm = new Process())
            {
                ilasm.StartInfo.UseShellExecute = false;
                ilasm.StartInfo.FileName = ILAsmExe;
                StringBuilder cmd = new StringBuilder();

                cmd.Append("/NOLOGO ");

                if (Quiet) cmd.Append("/QUIET ");

                if (NoAutoInherit) cmd.Append("/NOAUTOINHERIT ");

                if (!string.IsNullOrEmpty(Target)) cmd.Append($"/{Target} ");

                if (Pdb) cmd.Append("/PDB ");

                if (AppContainer) cmd.Append("/APPCONTAINER ");

                if (debug == DebugOption.Debug) cmd.Append("/DEBUG ");
                else if (debug != DebugOption.NoDebug) cmd.Append($"/DEBUG={debug.ToString().ToUpperInvariant()} ");

                if (Optimize) cmd.Append("/OPTIMIZE ");

                if (Fold) cmd.Append("/FOLD ");

                if (Clock) cmd.Append("/CLOCK ");

                if (Resources != null)
                    foreach (var r in Resources)
                        cmd.Append($"/RESOURCE=\"{r}\" ");

                if (!string.IsNullOrEmpty(Output))
                    cmd.Append($"/OUTPUT=\"{Output}\" ");

                if (Key?.StartsWith("@") ?? false)
                    cmd.Append($"/KEY=@\"{Key.Substring(1)}\" ");
                else if (!string.IsNullOrEmpty(Key))
                    cmd.Append($"/KEY=\"{Key}\" ");

                if (Includes != null)
                    foreach (var i in Includes)
                        cmd.Append($"/INCLUDE=\"{i}\" ");

                if (subsystem.HasValue) cmd.Append(((FormattableString)$"/SUBSYSTEM={SubSystem} ").ToString(InvariantCulture));

                if (!string.IsNullOrEmpty(SubsystemVersion)) cmd.Append($"/SSVER=\"{SubsystemVersion}\" ");

                if (flags.HasValue) cmd.Append(((FormattableString)$"/FLAGS={Flags} ").ToString(InvariantCulture));

                if (alignment.HasValue) cmd.Append(((FormattableString)$"/ALIGNMENT={alignment} ").ToString(InvariantCulture));

                if (@base.HasValue) cmd.Append(((FormattableString)$"/BASE={Base} ").ToString(InvariantCulture));

                if (stack.HasValue) cmd.Append(((FormattableString)$"/STACK={Stack} ").ToString(InvariantCulture));

                if (!string.IsNullOrEmpty(MetadataVersion)) cmd.Append($"/MDV=\"{MetadataVersion}\" ");

                if (!string.IsNullOrEmpty(MetadataStreamVersion)) cmd.Append($"/MSV=\"{MetadataStreamVersion}\" ");

                if (PE64) cmd.Append("/PE64 ");

                if (HighEntropyVirtualAddress) cmd.Append("/HIGHENTROPYVA ");

                if (NoCorStub) cmd.Append("/NOCORSTUB ");

                if (StripRelocations) cmd.Append("/STRIPRELOC ");

                if (!string.IsNullOrEmpty(TargetCpu)) cmd.Append($"/{TargetCpu} ");

                if (Prefer32Bit) cmd.Append("/32BITPREFERRED ");

                if (EditAndContinueDeltas != null)
                    foreach (var δ in EditAndContinueDeltas)
                        cmd.Append($"/ENC=\"{δ}\" ");

                if (Files != null)
                    foreach (var file in Files)
                        cmd.Append($"\"{file}\" ");

                ilasm.StartInfo.Arguments = cmd.ToString();

                Log.LogCommandLine(ilasm.StartInfo.FileName + " " + ilasm.StartInfo.Arguments);
                Log.LogMessage($"Running {ilasm.StartInfo.FileName} {ilasm.StartInfo.Arguments}");

                ilasm.StartInfo.RedirectStandardError = true;
                ilasm.StartInfo.RedirectStandardOutput = true;
                ilasm.ErrorDataReceived += (sender, e) => { if (e.Data != null) Log.LogError(e.Data); };
                ilasm.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        if (e.Data.IndexOf("error", StringComparison.InvariantCultureIgnoreCase) >= 0)
                            Log.LogError(e.Data);
                        else if (e.Data.IndexOf("warning", StringComparison.InvariantCultureIgnoreCase) >= 0)
                            Log.LogWarning(e.Data);
                        else Log.LogMessage(e.Data);
                    }
                };
                ilasm.Start();
                ilasm.BeginErrorReadLine();
                ilasm.BeginOutputReadLine();
                ilasm.WaitForExit();
                if (ilasm.ExitCode != 0)
                    Log.LogError($"Process {System.IO.Path.GetFileName(ilasm.StartInfo.FileName)} {ilasm.StartInfo.Arguments} exited with code {ilasm.ExitCode}");
                else
                    Log.LogMessage($"Process {System.IO.Path.GetFileName(ilasm.StartInfo.FileName)} {ilasm.StartInfo.Arguments} exited with code {ilasm.ExitCode}");
                return ilasm.ExitCode == 0;
            }
        }
    }

    /// <summary>Options for generating debug info</summary>
    public enum DebugOption
    {
        /// <summary>Generate no debug info</summary>
        NoDebug,
        /// <summary>Disable JIT optimization, create PDB file, use sequence points from PDB</summary>
        Debug,
        /// <summary>Disable JIT optimization, create PDB file, use implicit sequence points</summary>
        Impl,
        /// <summary>Enable JIT optimization, create PDB file, use implicit sequence points</summary>
        Opt
    }
}