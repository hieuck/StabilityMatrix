﻿using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using StabilityMatrix.Core.Extensions;
using StabilityMatrix.Core.Processes;

namespace StabilityMatrix.Core.Python;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public record PipInstallArgs : ProcessArgsBuilder
{
    public PipInstallArgs(params Argument[] arguments)
        : base(arguments) { }

    public PipInstallArgs WithTorch(string version = "") =>
        this.AddArg(new Argument("torch", $"torch{version}"));

    public PipInstallArgs WithTorchDirectML(string version = "") =>
        this.AddArg(new Argument("torch-directml", $"torch-directml{version}"));

    public PipInstallArgs WithTorchVision(string version = "") =>
        this.AddArg(new Argument("torchvision", $"torchvision{version}"));

    public PipInstallArgs WithTorchAudio(string version = "") =>
        this.AddArg(new Argument("torchaudio", $"torchaudio{version}"));

    public PipInstallArgs WithXFormers(string version = "") =>
        this.AddArg(new Argument("xformers", $"xformers{version}"));

    public PipInstallArgs WithExtraIndex(string indexUrl) =>
        this.AddKeyedArgs("--extra-index-url", ["--extra-index-url", indexUrl]);

    public PipInstallArgs WithTorchExtraIndex(string index) =>
        WithExtraIndex($"https://download.pytorch.org/whl/{index}");

    public PipInstallArgs WithParsedFromRequirementsTxt(
        string requirements,
        [StringSyntax(StringSyntaxAttribute.Regex)] string? excludePattern = null
    )
    {
        var requirementsEntries = requirements
            .SplitLines(StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(s => !s.StartsWith('#'))
            .Select(s => s.Contains('#') ? s.Substring(0, s.IndexOf('#')) : s)
            .Where(s => !string.IsNullOrWhiteSpace(s));

        if (excludePattern is not null)
        {
            var excludeRegex = new Regex($"^{excludePattern}$");

            requirementsEntries = requirementsEntries.Where(s => !excludeRegex.IsMatch(s));
        }

        return this.AddArgs(requirementsEntries.Select(s => (Argument)s).ToArray());
    }

    public PipInstallArgs WithUserOverrides(List<PipPackageSpecifier> overrides)
    {
        var newArgs = this;

        foreach (var pipOverride in overrides)
        {
            if (string.IsNullOrWhiteSpace(pipOverride.Name))
                continue;

            newArgs = newArgs.RemovePipArgKey(pipOverride.Name);

            // if version is -1, just remove and continue
            if (pipOverride.Version?.Equals("-1") ?? false)
            {
                continue;
            }

            var argument = string.IsNullOrWhiteSpace(pipOverride.Version)
                ? pipOverride.Name
                : $"{pipOverride.Name}{pipOverride.Constraint}{pipOverride.Version}";

            newArgs = newArgs.AddArg(argument);
        }

        return newArgs;
    }

    [Pure]
    public PipInstallArgs RemovePipArgKey(string argumentKey)
    {
        return this with
        {
            Arguments = Arguments
                .Where(
                    arg =>
                        arg.HasKey
                            ? (arg.Key != argumentKey)
                            : (arg.Value != argumentKey && !arg.Value.Contains($"{argumentKey}=="))
                )
                .ToImmutableList()
        };
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return base.ToString();
    }
}
