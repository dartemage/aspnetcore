// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

public sealed partial class XmlCommentGenerator
{
    internal static IEnumerable<(string, string?, XmlComment?)> ParseComments(Compilation compilation, CancellationToken cancellationToken)
    {
        var visitor = new AssemblyTypeSymbolsVisitor(cancellationToken);
        visitor.VisitAssembly(compilation.Assembly);
        // foreach (var reference in compilation.References)
        // {
        //     if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
        //     {
        //         visitor.VisitAssembly(assemblySymbol);
        //     }
        // }
        var types = visitor.GetPublicTypes();
        var comments = new List<(string, string?, XmlComment?)>();
        foreach (var type in types)
        {
            var comment = type.GetDocumentationCommentXml(
                preferredCulture: CultureInfo.InvariantCulture,
                expandIncludes: true,
                cancellationToken: cancellationToken);
            if (comment is not null)
            {
                var typeInfo = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                comments.Add((typeInfo, null, XmlComment.Parse(comment, new())));
            }
        }
        var properties = visitor.GetPublicProperties();
        foreach (var property in properties)
        {
            var comment = property.GetDocumentationCommentXml(
                preferredCulture: CultureInfo.InvariantCulture,
                expandIncludes: true,
                cancellationToken: cancellationToken);
            if (comment is not null)
            {
                var typeInfo = property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var propertyInfo = property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                comments.Add((typeInfo, propertyInfo, XmlComment.Parse(comment, new())));
            }
        }
        return comments;
    }

    internal static bool FilterInvocations(SyntaxNode node, CancellationToken _)
        => node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddOpenApi" } };

    internal static AddOpenApiInvocation GetAddOpenApiOverloadVariant(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var invocationExpression = (InvocationExpressionSyntax)context.Node;
        var interceptableLocation = context.SemanticModel.GetInterceptableLocation(invocationExpression, cancellationToken);
        return new(invocationExpression.ArgumentList.Arguments.Count switch
        {
            0 => AddOpenApiOverloadVariant.AddOpenApi,
            1 => AddOpenApiOverloadVariant.AddOpenApiDocumentName,
            2 => AddOpenApiOverloadVariant.AddOpenApiDocumentNameConfigureOptions,
            _ => throw new InvalidOperationException("Invalid number of arguments for supported `AddOpenApi` overload."),
        }, invocationExpression, interceptableLocation);
    }
}
