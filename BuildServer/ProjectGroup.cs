﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;

namespace BuildServer;

/// <summary>
/// Simple hosting of BuildManager from msbuild
/// </summary>
public class ProjectGroup : IDisposable
{
    private readonly ProjectCollection _projectCollection;
    private readonly Builder _builder;
    private ProjectGraph _projectGraph;
    private readonly Dictionary<string, ProjectState> _projectStates;

    internal ProjectGroup(Builder builder, IReadOnlyDictionary<string, string> globalProperties)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        if (globalProperties == null) throw new ArgumentNullException(nameof(globalProperties));

        var properties = new Dictionary<string, string>(globalProperties)
        {
            ["UnityBuildServer"] = "true",
            ["IsGraphBuild"] = "true" // Make this upfront to include it in the cache file names
        };

        _projectStates = new Dictionary<string, ProjectState>();
        _projectCollection = new ProjectCollection(properties, null, null, ToolsetDefinitionLocations.Default, builder.MaxNodeCount, false, true, builder.ProjectCollectionRootElementCache);
        _builder = builder;
    }

    public int Count => _projectCollection.LoadedProjects.Count;

    public ProjectCollection ProjectCollection => _projectCollection;

    public ProjectGraph ProjectGraph => _projectGraph;

    public ProjectState FindProjectState(string projectPath)
    {
        if (projectPath == null) throw new ArgumentNullException(nameof(projectPath));
        _projectStates.TryGetValue(projectPath, out var projectState);
        return projectState;
    }
       
    internal void InitializeGraph()
    {
        // Initialize the project graph
        var parallelism = 8;
        var entryPoints = _builder.Provider.GetProjectPaths().Select(x => new ProjectGraphEntryPoint(x, _projectCollection.GlobalProperties));
        _projectGraph = new ProjectGraph(entryPoints, _projectCollection, CreateProjectInstance, parallelism, CancellationToken.None);

        // Group graph node with Project
        foreach (var graphNode in _projectGraph.ProjectNodes)
        {
            this.FindProjectState(graphNode).ProjectGraphNode = graphNode;
        }

        // Check
        bool hasErrors = false;
        foreach (var project in _projectCollection.LoadedProjects)
        {
            if (string.IsNullOrEmpty(project.GetPropertyValue("TargetFramework")))
            {
                Console.Error.WriteLine($"Error: the project {project.FullPath} is multi-targeting {project.GetPropertyValue("TargetFrameworks")}. Multi-targeting is currently not supported by this prototype");
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            throw new InvalidOperationException("Invalid error in project.");
        }
    }

    private ProjectInstance CreateProjectInstance(string projectPath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
    {
        ProjectState projectState;
        lock (_projectStates)
        {
            if (!_projectStates.TryGetValue(projectPath, out projectState))
            {
                projectState = new ProjectState();
                _projectStates[projectPath] = projectState;
            }
        }

        projectState.Project = new Project(projectPath, globalProperties, projectCollection.DefaultToolsVersion, projectCollection);
        projectState.ProjectInstance = new ProjectInstance(projectState.Project.Xml, globalProperties, projectState.Project.ToolsVersion, projectCollection);
        return projectState.ProjectInstance;
    }

    public (Project, ProjectInstance) ReloadProject(Project project)
    {
        ProjectCollection.UnloadProject(project);
        return (project, CreateProjectInstance(project.FullPath, project.GlobalProperties, ProjectCollection));
    }

    public void Dispose()
    {
        _projectCollection.UnloadAllProjects();
        _projectCollection.Dispose();
    }
}