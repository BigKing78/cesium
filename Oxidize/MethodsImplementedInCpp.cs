﻿using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Xml.Linq;

namespace Oxidize
{
    internal class MethodsImplementedInCpp
    {
        public static void Generate(CppGenerationContext context, TypeToGenerate item, GeneratedResult result)
        {
            Debug.Assert(result.CppImplementationInvoker != null);
            if (result.CppImplementationInvoker == null)
                return;
            if (result.CSharpPartialMethodDefinitions == null)
                return;

            // Add functions to create and destroy the implementation class instance.
            CppType wrapperType = result.CppDefinition.Type;
            CppType implType = result.CppImplementationInvoker.ImplementationType;
            CppType objectHandleType = CppObjectHandle.GetCppType(context);

            result.CppImplementationInvoker.Functions.Add(new(
                Content:
                    $$"""
                    void* {{wrapperType.GetFullyQualifiedName(false).Replace("::", "_")}}_Create(void* handle) {
                      const {{wrapperType.GetFullyQualifiedName()}} wrapper{{{objectHandleType.GetFullyQualifiedName()}}(handle)};
                      return reinterpret_cast<void*>(new {{implType.GetFullyQualifiedName()}}(wrapper));
                    }
                    """,
                TypeDefinitionsReferenced: new[]
                {
                    wrapperType,
                    implType,
                    objectHandleType,
                }));

            result.CppImplementationInvoker.Functions.Add(new(
                Content:
                    $$"""
                    void {{wrapperType.GetFullyQualifiedName(false).Replace("::", "_")}}_Dispose(void* handle, void* pImpl) {
                      const {{wrapperType.GetFullyQualifiedName()}} wrapper{{{objectHandleType.GetFullyQualifiedName()}}(handle)};
                      auto pImplTyped = reinterpret_cast<{{implType.GetFullyQualifiedName()}}*>(pImpl);
                      pImplTyped->JustBeforeDelete(wrapper);
                      delete pImplTyped;
                    }
                    """,
                TypeDefinitionsReferenced: new[]
                {
                    wrapperType,
                    implType,
                    objectHandleType
                }));

            string disposeName = $$"""{{wrapperType.GetFullyQualifiedName(false).Replace("::", "_")}}_Dispose""";
            result.CSharpPartialMethodDefinitions.Methods.Add(new(
                methodDefinition:
                    $$"""
                    public void Dispose()
                    {
                        if (_implementation != System.IntPtr.Zero)
                        {
                            {{disposeName}}(ObjectHandleUtility.CreateHandle(this), _implementation);
                            _implementation = System.IntPtr.Zero;
                        }
                    }
                    """,
                interopFunctionDeclaration:
                    $$"""
                    [DllImport("CesiumForUnityNative.dll", CallingConvention=CallingConvention.Cdecl)]
                    private static extern void {{disposeName}}(System.IntPtr thiz, System.IntPtr implementation);
                    """));

            // Add functions for other methods.
            foreach (IMethodSymbol method in item.MethodsImplementedInCpp)
            {
                GenerateMethod(context, item, result, method);
            }
        }

        private static void GenerateMethod(CppGenerationContext context, TypeToGenerate item, GeneratedResult result, IMethodSymbol method)
        {
            if (result.CppImplementationInvoker == null)
                return;
            if (result.CSharpPartialMethodDefinitions == null)
                return;

            CppType wrapperType = result.CppDefinition.Type;
            CppType implType = result.CppImplementationInvoker.ImplementationType;

            // TODO: support static methods
            // TODO: support return values

            CppType returnType = CppType.FromCSharp(context, method.ReturnType).AsReturnType();
            CppType interopReturnType = returnType.AsInteropType();
            var parameters = method.Parameters.Select(parameter =>
            {
                CppType type = CppType.FromCSharp(context, parameter.Type).AsParameterType();
                return (Name: parameter.Name, Type: type, InteropType: type.AsInteropType());
            });

            string name = $"{wrapperType.GetFullyQualifiedName(false).Replace("::", "_")}_{method.Name}";
            
            var parameterList = new[] { "void* handle", "void* pImpl" }.Concat(parameters.Select(parameter => $"{parameter.InteropType.GetFullyQualifiedName()} {parameter.Name}"));
            string parameterListString = string.Join(", ", parameterList);

            var callParameterList = new[] { "wrapper" }.Concat(parameters.Select(parameter => parameter.Type.GetConversionFromInteropType(context, parameter.Name)));
            string callParameterListString = string.Join(", ", callParameterList);

            CppType objectHandleType = CppObjectHandle.GetCppType(context);

            result.CppImplementationInvoker.Functions.Add(new(
                Content: $$"""
                    {{interopReturnType.GetFullyQualifiedName()}} {{name}}({{parameterListString}}) {
                      const {{wrapperType.GetFullyQualifiedName()}} wrapper{{{objectHandleType.GetFullyQualifiedName()}}(handle)};
                      auto pImplTyped = reinterpret_cast<{{implType.GetFullyQualifiedName()}}*>(pImpl);
                      pImplTyped->{{method.Name}}({{callParameterListString}});
                    }
                    """,
                TypeDefinitionsReferenced: new[]
                {
                    wrapperType,
                    implType,
                    returnType,
                    objectHandleType
                },
                TypeDeclarationsReferenced:
                    parameters.Select(parameter => parameter.Type)
                    .Concat(parameters.Select(parameter => parameter.InteropType))
            ));

            CSharpType csWrapperType = CSharpType.FromSymbol(context.Compilation, item.Type);
            CSharpType csReturnType = CSharpType.FromSymbol(context.Compilation, method.ReturnType);
            var csParameters = method.Parameters.Select(parameter => (Name: parameter.Name, CallName: parameter.Name, Type: CSharpType.FromSymbol(context.Compilation, parameter.Type)));
            var csParametersInterop = csParameters;
            var implementationPointer = CSharpType.FromSymbol(context.Compilation, context.Compilation.GetSpecialType(SpecialType.System_IntPtr));

            if (!method.IsStatic)
            {
                csParametersInterop = new[] { (Name: "thiz", CallName: "this", Type: csWrapperType), (Name: "implementation", CallName: "_implementation", Type: implementationPointer) }.Concat(csParametersInterop);
            }

            string implementation;
            if (csReturnType.Symbol.SpecialType == SpecialType.System_Void)
            {
                implementation =
                    $$"""
                    {{name}}({{string.Join(", ", csParametersInterop.Select(parameter => parameter.Type.GetConversionToInteropType(parameter.CallName)))}});
                    """;
            }
            else
            {
                implementation =
                    $$"""
                    var result = {{ name }}({{string.Join(", ", csParametersInterop.Select(parameter => parameter.Type.GetConversionToInteropType(parameter.CallName)))}});
                    return {{csReturnType.GetConversionFromInteropType("result")}};
                    """;
            }

            string modifiers = CSharpTypeUtility.GetAccessString(method.DeclaredAccessibility);
            if (method.IsStatic)
                modifiers += " static";

            result.CSharpPartialMethodDefinitions.Methods.Add(new(
                methodDefinition:
                    $$"""
                    {{modifiers}} partial {{csReturnType.GetFullyQualifiedName()}} {{method.Name}}({{string.Join(", ", csParameters.Select(parameter => $"{parameter.Type.GetFullyQualifiedName()} {parameter.Name}"))}})
                    {
                        System.Diagnostics.Debug.Assert(_implementation != System.IntPtr.Zero, "Implementation instance has already been destroyed.");
                        {{new[] { implementation }.JoinAndIndent("    ")}}
                    }
                    """,
                interopFunctionDeclaration:
                    $$"""
                    [DllImport("CesiumForUnityNative.dll", CallingConvention=CallingConvention.Cdecl)]
                    private static extern {{csReturnType.AsInteropType().GetFullyQualifiedName()}} {{name}}({{string.Join(", ", csParametersInterop.Select(parameter => parameter.Type.AsInteropType().GetFullyQualifiedName() + " " + parameter.Name))}});
                    """));
        }
    }
}