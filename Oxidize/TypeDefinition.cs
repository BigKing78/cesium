﻿using Microsoft.CodeAnalysis;

namespace Oxidize
{
    internal struct InteropConstructor
    {
        public InteropConstructor(ITypeSymbol type, CppType cppType, IMethodSymbol constructor, string cppTarget, string cppSignature)
        {
            this.Type = type;
            this.CppType = cppType;
            this.Constructor = constructor;
            this.CppTarget = cppTarget;
            this.CppSignature = cppSignature;
        }

        /// <summary>
        /// The managed type that contains this method.
        /// </summary>
        public ITypeSymbol Type;

        /// <summary>
        /// The C++ type that contains this method.
        /// </summary>
        public CppType CppType;

        /// <summary>
        /// The C# constructor to be made callable from C++.
        /// </summary>
        public IMethodSymbol Constructor;

        /// <summary>
        /// The C++ expression to which a function pointer corresponding to the
        /// constructor is assigned.
        /// For example: `::Oxidize::ObjectHandle::CallCreateHandle`
        /// </summary>
        public string CppTarget;

        /// <summary>
        /// The C++ function signature type. For example:
        /// `void* (*)(void* o)`
        /// </summary>
        public string CppSignature;
    }

    internal struct InteropMethod
    {
        public InteropMethod(ITypeSymbol type, CppType cppType, IMethodSymbol method, string cppTarget, string cppSignature)
        {
            this.Type = type;
            this.CppType = cppType;
            this.Method = method;
            this.CppTarget = cppTarget;
            this.CppSignature = cppSignature;
        }

        /// <summary>
        /// The managed type that contains this method.
        /// </summary>
        public ITypeSymbol Type;

        /// <summary>
        /// The C++ type that contains this method.
        /// </summary>
        public CppType CppType;

        /// <summary>
        /// The C# method to be made callable from C++.
        /// </summary>
        public IMethodSymbol Method;

        /// <summary>
        /// The C++ expression to which a function pointer corresponding to the
        /// method is assigned.
        /// For example: `::Oxidize::ObjectHandle::CallCreateHandle`
        /// </summary>
        public string CppTarget;

        /// <summary>
        /// The C++ function signature type. For example:
        /// `void* (*)(void* o)`
        /// </summary>
        public string CppSignature;
    }

    internal class TypeDefinition
    {
        /// <summary>
        /// The type being defined.
        /// </summary>
        public CppType? Type = null;

        /// <summary>
        /// The type's public declarations in the header .h file.
        /// </summary>
        public List<string> declarations = new List<string>();

        /// <summary>
        /// The type's definitions in the implementation .cpp file.
        /// </summary>
        public List<string> definitions = new List<string>();

        /// <summary>
        /// #includes required in the header .h file.
        /// </summary>
        public HashSet<string> headerIncludes = new HashSet<string>();

        /// <summary>
        /// #include required in the implementation .cpp file.
        /// </summary>
        public HashSet<string> cppIncludes = new HashSet<string>();

        /// <summary>
        /// Forward declarations to include the header .h file.
        /// </summary>
        public HashSet<string> forwardDeclarations = new HashSet<string>();

        /// <summary>
        /// The type's private declarations in the header .h file.
        /// </summary>
        public List<string> privateDeclarations = new List<string>();

        /// <summary>
        /// The constructors required to be accessible via C++ function pointer to
        /// enable interoperability between C# and C++.
        /// </summary>
        public List<InteropConstructor> interopConstructors = new List<InteropConstructor>();

        /// <summary>
        /// The methods required to be accessible via C++ function pointer to
        /// enable interoperability between C# and C++.
        /// </summary>
        public List<InteropMethod> interopMethods = new List<InteropMethod>();

        /// <summary>
        /// The methods that are defined in a C# class but implemented in C++.
        /// </summary>
        public List<InteropMethod> methodsImplementedInCpp = new List<InteropMethod>();
    }
}
