﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;

namespace XelaBuild.Core;

public static class ResultsHelper
{
    public static int Verify(Builder builder, object resultsArg)
    {
        if (resultsArg == null) return 0;

        int resultCount = 0;
        bool hasErrors = false;
        if (resultsArg is IReadOnlyDictionary<ProjectGraphNode, BuildResult> results)
        {
            foreach (var result in results)
            {
                CheckBuildResult(result.Key.ProjectInstance.FullPath, result.Value);
            }
            resultCount = results.Count;
        }
        else if (resultsArg is BuildResult result)
        {
            CheckBuildResult(builder.Provider.GetProjectPaths().First(), result);
            resultCount = 1;
        }

        if (hasErrors)
        {
            Console.Error.WriteLine("*** Exiting due to errors above ***");
            // Exiting if we have errors
            Environment.Exit(1);
        }

        return resultCount;

        void CheckBuildResult(string path, BuildResult result)
        {
            if (result.OverallResult == BuildResultCode.Failure)
            {
                Console.Error.WriteLine($"msbuild failed on project {path} {result.Exception}");
                hasErrors = true;
            }
        }
    }
}