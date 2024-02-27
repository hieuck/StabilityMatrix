﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using StabilityMatrix.Avalonia.Models;
using StabilityMatrix.Avalonia.ViewModels.Base;
using StabilityMatrix.Avalonia.Views.Dialogs;
using StabilityMatrix.Core.Attributes;
using StabilityMatrix.Core.Helper;
using StabilityMatrix.Core.Models;
using StabilityMatrix.Core.Models.Api.OpenArt;
using StabilityMatrix.Core.Models.Packages.Extensions;

namespace StabilityMatrix.Avalonia.ViewModels.Dialogs;

[View(typeof(OpenArtWorkflowDialog))]
[ManagedService]
[Transient]
public partial class OpenArtWorkflowViewModel : ContentDialogViewModelBase
{
    public required OpenArtSearchResult Workflow { get; init; }
    public PackagePair? InstalledComfy { get; init; }

    [ObservableProperty]
    private ObservableCollection<OpenArtCustomNode> customNodes = [];

    [ObservableProperty]
    private string prunedDescription = string.Empty;

    public List<PackageExtension> MissingNodes { get; } = [];

    public override async Task OnLoadedAsync()
    {
        CustomNodes = new ObservableCollection<OpenArtCustomNode>(
            await ParseNodes(Workflow.NodesIndex.ToList())
        );
        PrunedDescription = Utilities.RemoveHtml(Workflow.Description);
    }

    [Localizable(false)]
    private async Task<List<OpenArtCustomNode>> ParseNodes(List<string> nodes)
    {
        var indexOfFirstDot = nodes.IndexOf(".");
        if (indexOfFirstDot != -1)
        {
            nodes = nodes[(indexOfFirstDot + 1)..];
        }

        var installedNodesNames = new HashSet<string>();
        var nameToManifestNodes = new Dictionary<string, PackageExtension>();

        if (InstalledComfy?.BasePackage.ExtensionManager is { } extensionManager)
        {
            var installedNodes = (
                await extensionManager.GetInstalledExtensionsLiteAsync(InstalledComfy.InstalledPackage)
            ).ToList();

            var manifestExtensionsMap = await extensionManager.GetManifestExtensionsMapAsync(
                extensionManager.GetManifests(InstalledComfy.InstalledPackage)
            );

            // Add manifestExtensions definition to installedNodes if matching git repository url
            installedNodes = installedNodes
                .Select(installedNode =>
                {
                    if (
                        installedNode.GitRepositoryUrl is not null
                        && manifestExtensionsMap.TryGetValue(
                            installedNode.GitRepositoryUrl,
                            out var manifestExtension
                        )
                    )
                    {
                        installedNode = installedNode with { Definition = manifestExtension };
                    }

                    return installedNode;
                })
                .ToList();

            // There may be duplicate titles, deduplicate by using the first one
            nameToManifestNodes = manifestExtensionsMap
                .GroupBy(x => x.Value.Title)
                .ToDictionary(x => x.Key, x => x.First().Value);

            installedNodesNames = installedNodes.Select(x => x.Title).ToHashSet();
        }

        var sections = new List<OpenArtCustomNode>();
        OpenArtCustomNode? currentSection = null;

        foreach (var node in nodes)
        {
            if (node is "." or ",")
            {
                currentSection = null; // End of the current section
                continue;
            }

            if (currentSection == null)
            {
                currentSection = new OpenArtCustomNode
                {
                    Title = node,
                    IsInstalled = installedNodesNames.Contains(node)
                };

                // Add missing nodes to the list
                if (
                    !currentSection.IsInstalled && nameToManifestNodes.TryGetValue(node, out var manifestNode)
                )
                {
                    MissingNodes.Add(manifestNode);
                }

                sections.Add(currentSection);
            }
            else
            {
                currentSection.Children.Add(node);
            }
        }

        if (sections.FirstOrDefault(x => x.Title == "ComfyUI") != null)
        {
            sections = sections.Where(x => x.Title != "ComfyUI").ToList();
        }

        return sections;
    }
}
