using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;



namespace Doot.SourceGenerators
{
    public class SerialisableSyntaxContextReceiver : ISyntaxContextReceiver
    {
        public readonly List<INamedTypeSymbol> Classes;

        public SerialisableSyntaxContextReceiver()
        {
            Classes = new List<INamedTypeSymbol>();
        }

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not ClassDeclarationSyntax clsNode)
                return;

            var sym = context.SemanticModel.GetDeclaredSymbol(clsNode);

            if (sym.Interfaces.Length == 0)
                return;

            if (!sym.Interfaces.Any(i => $"{i.ContainingNamespace.Name}.{i.Name}" == "Doot.ISerialisable"))
                return;

            Classes.Add(sym);
        }
    }

    [Generator]
    public class SerialisableSourceGenerator : ISourceGenerator
    {
        static readonly DiagnosticDescriptor NonPartialDescriptor = new(
            "SG0001",
            "Non-partial serialisable class type",
            "The class '{0}' is not marked as partial. Classes that implement 'Doot.ISerialisable' must be marked as partial for source generation to work.",
            "Source Generator",
            DiagnosticSeverity.Error,
            true
        );

        static readonly DiagnosticDescriptor NoPublicFieldsDescriptor = new(
            "SG1001",
            "No public fields in serialisable class type",
            "The class '{0}' contains no public fields and will not be serialised",
            "Source Generator",
            DiagnosticSeverity.Warning,
            true
        );

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SerialisableSyntaxContextReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var ctxReceiver = (SerialisableSyntaxContextReceiver)context.SyntaxContextReceiver;

            foreach (var cls in ctxReceiver.Classes)
            {
                var clsSyntax = cls.DeclaringSyntaxReferences[0].GetSyntax() as ClassDeclarationSyntax;

                if (!clsSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(NonPartialDescriptor, cls.Locations.FirstOrDefault(), cls.Name)
                    );
                }

                var fields = cls.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.DeclaredAccessibility == Accessibility.Public);

                if (fields.Count() == 0)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(NoPublicFieldsDescriptor, cls.Locations.FirstOrDefault(), cls.Name)
                    );
                }

                var serBody = new StringBuilder();
                var desBody = new StringBuilder();

                foreach (IFieldSymbol field in fields)
                {
                    serBody.AppendLine($"serialiser.Write({field.Name});");

                    if (field.Type.Interfaces.Any(i => $"{i.ContainingNamespace}.{i.Name}" == "Doot.ISerialisable"))
                    {
                        desBody.AppendLine($"deserialiser.Read(out ISerialisable {field.Name}_tmp);");
                        desBody.AppendLine($"{field.Name} = ({field.Type}){field.Name}_tmp;");
                    }
                    else
                    {
                        desBody.AppendLine($"deserialiser.Read(out {field.Name});");
                    }
                }

                var containingClasses = new List<string>();
                var parent = cls.ContainingType;

                while (parent != null)
                {
                    if (parent.TypeKind != TypeKind.Class)
                        break;

                    containingClasses.Add(parent.Name);
                    parent = parent.ContainingType;
                }

                var source = $@"
using System;

namespace {cls.ContainingNamespace}
{{
    {String.Join("\n", containingClasses.Select(c => $"partial class {c} {{"))}
    partial class {cls.Name}
    {{
        public void Serialise(MessageSerialiser serialiser)
        {{
            {serBody}
        }}

        public void Deserialise(MessageDeserialiser deserialiser)
        {{
            {desBody}
        }}
    }}
    {String.Join("\n", containingClasses.Select(c => "}"))}
}}
";
                var normalised = CSharpSyntaxTree.ParseText(source).GetRoot().NormalizeWhitespace().ToFullString();
                context.AddSource($"{cls.ToDisplayString()}.g.cs", normalised);
            }

        }
    }
}
