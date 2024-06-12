// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.OpenApi.SourceGenerators;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

public partial class XmlCommentGeneratorTests
{
    [Fact]
    public async Task SmokeTest()
    {
        await new CSharpSourceGeneratorTest<XmlCommentGenerator, XUnitVerifier>
        {
            TestState =
            {
                Sources = { Sources[nameof(SmokeTest)].Input },
                GeneratedSources =
                {
                    (typeof(XmlCommentGenerator), "XmlCommentGenerator.CommentCache.generated.cs", Sources[nameof(SmokeTest)].CommentCache)
                },
                ReferenceAssemblies = GetReferenceAssemblies(),
            }
        }.RunAsync();
    }

    internal static ReferenceAssemblies GetReferenceAssemblies()
    {
        var nugetConfigPath = SkipOnHelixAttribute.OnHelix() ?
            Path.Combine(
                Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"),
                "NuGet.config") :
            Path.Combine(GetRepoRoot(), "NuGet.config");
        var net8Ref = new ReferenceAssemblies(
            "net9.0",
            new PackageIdentity(
                "Microsoft.NETCore.App.Ref",
                GetMicrosoftNETCoreAppRefPackageVersion()),
            Path.Combine("ref", "net9.0"))
        .WithNuGetConfigFilePath(nugetConfigPath);

        return net8Ref.AddAssemblies([
            TrimAssemblyExtension(typeof(OpenApiSchemaService).Assembly.Location)
        ]);
    }

    static string TrimAssemblyExtension(string fullPath) => fullPath.Replace(".dll", string.Empty);

    public static string GetMicrosoftNETCoreAppRefPackageVersion() => GetTestDataValue("MicrosoftNETCoreAppRefVersion");

    public static string GetRepoRoot() => GetTestDataValue("RepoRoot");

    private static string GetTestDataValue(string key)
         => typeof(XmlCommentGeneratorTests).Assembly.GetCustomAttributes<TestDataAttribute>().Single(d => d.Key == key).Value;
}
