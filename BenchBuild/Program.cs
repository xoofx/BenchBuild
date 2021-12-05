﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchBuild;
using BuildServer;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;

// Bug in msbuild: https://github.com/dotnet/msbuild/pull/7013
// MSBuild is trying to relaunch this process (instead of using dotnet), so we protect our usage here
// Also, if `dotnet.exe` is not 2 folders above msbuild.dll, as it is the case in our local build, then it will use this exe has the msbuild server process
if (MsBuildHelper.IsCommandLineArgsForMsBuild(args))
{
    var exitCode = MsBuildHelper.Run(args);
    Environment.Exit(exitCode);
    return;
}

// BEGIN
// ------------------------------------------------------------------------------------------------------------------------
// Make sure that we are using our local copy of msbuild
MsBuildHelper.RegisterCustomMsBuild();

// ------------------------------------------------------------------------------------------------------------------------
DumpHeader("Generate Projects");
var rootProject = ProjectGenerator.Generate();
Console.WriteLine($"RootProject {rootProject}");

RunBenchmark(rootProject);

// This need to run in a separate method to allow msbuild to load the .NET assemblies before in MsBuildHelper.RegisterCustomMsBuild.
static void RunBenchmark(string rootProject)
{
    var rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(rootProject));
    // ------------------------------------------------------------------------------------------------------------------------
    DumpHeader("Load Projects and graph");
    var clock = Stopwatch.StartNew();
    var builder = new Builder(rootProject)
    {
        UseGraph = true
    };
    Console.WriteLine($"Time to load: {clock.Elapsed.TotalMilliseconds}ms");

    //builder.DumpRootGlobs(graph);

    // ------------------------------------------------------------------------------------------------------------------------
    DumpHeader("Restore Projects");
    clock.Restart();
    builder.Run("Restore");
    Console.WriteLine($"=== Time to Restore {builder.Count} projects: {clock.Elapsed.TotalMilliseconds}ms");

    if (Debugger.IsAttached)
    {
        Console.WriteLine("Press key to attach to msbuild");
        Console.ReadLine();
    }

    // ------------------------------------------------------------------------------------------------------------------------
    DumpHeader("Build caches");
    clock.Restart();
    builder.Run("Build");
    Console.WriteLine($"=== Time to Build Cache {clock.Elapsed.TotalMilliseconds}ms");

    int index = 0;
    const int runCount = 5;
    // ------------------------------------------------------------------------------------------------------------------------
    foreach (var (kind, prepare, build) in new (string, Action, Func<IReadOnlyDictionary<ProjectGraphNode, BuildResult>>)[]
            {
            ("Build All (Clean)",
                () => builder.Run("Clean"),
                () => builder.Run("Build")
            ),
            ("Build Root - No Changes",
                null,
                () => builder.BuildRootOnlyWithParallelCache("Build")
            ),
            ("Build Root - 1 C# file changed in root", 
                () => System.IO.File.SetLastWriteTimeUtc(Path.Combine(rootFolder, "LibRoot", "LibRootClass.cs"), DateTime.UtcNow),
                () => builder.BuildRootOnlyWithParallelCache("Build")
            ),
            ("Build All - 1 C# file changed in leaf", 
                () => File.WriteAllText(Path.Combine(rootFolder, "LibLeaf", "LibLeafClass.cs"), $@"namespace LibLeaf;
public static class LibLeafClass {{
    public static void Run() {{
        // empty
    }}
    public static void Change{index}() {{ }}
}}
"),
                () => builder.Run("Build")
            )
            })
    {

        DumpHeader(kind);

        for (int i = 0; i < runCount; i++)
        {
            prepare?.Invoke();

            clock.Restart();

            var results = build();

            Console.WriteLine($"[{i}] Time to build {results.Count} projects: {clock.Elapsed.TotalMilliseconds}ms");
        }

        index++;
    }
}
// END
// **************************************************************

static void DumpHeader(string header)
{
    Console.WriteLine("============================================================================");
    Console.WriteLine(header);
    Console.WriteLine("****************************************************************************");
}