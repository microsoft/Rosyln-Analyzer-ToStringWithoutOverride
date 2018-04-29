using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ToStringWithoutOverrideAnalyzer
{
    public class TypeInspection
    {
        public struct LackingOverridenToStringResult
        {
            public LackingOverridenToStringResult(ExpressionSyntax expression, TypeInfo typeInfo)
            {
                this.Expression = expression;
                this.TypeInfo = typeInfo;
            }

            public ExpressionSyntax Expression { get; }
            public TypeInfo TypeInfo { get; }
        }
        private readonly SemanticModel semanticModel;
        private readonly INamedTypeSymbol objectType;
        private readonly IArrayTypeSymbol objectArrayType;
        private readonly INamedTypeSymbol stringType;
        private readonly INamedTypeSymbol valueType;

        public TypeInspection(SemanticModel semanticModel)
        {
            this.semanticModel = semanticModel;
            this.objectType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            this.objectArrayType = semanticModel.Compilation.CreateArrayTypeSymbol(semanticModel.Compilation.GetSpecialType(SpecialType.System_Object));
            this.stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            this.valueType = semanticModel.Compilation.GetSpecialType(SpecialType.System_ValueType);
        }

        public bool LacksOverridenToString(TypeInfo typeInfo)
        {
            return !HasToString(typeInfo);
        }
        
        private bool HasToString(TypeInfo typeInfo)
        {
            return IsString(typeInfo) || IsObject(typeInfo) || TypeHasOverridenToString(typeInfo);
        }

        private bool IsObject(TypeInfo typeInfo)
        {
            return Equals(typeInfo.Type, this.objectType);
        }

        public bool IsObjectArray(TypeInfo typeInfo)
        {
            return Equals(typeInfo.Type, this.objectArrayType);
        }

        public bool IsString(TypeInfo typeInfo)
        {
            return Equals(typeInfo.Type, this.stringType);
        }

        private bool IsValueType(TypeInfo typeInfo)
        {
            return Equals(typeInfo.Type, this.valueType);
        }

        private bool TypeHasOverridenToString(TypeInfo typeInfo)
        {
            for (var type = typeInfo.Type; type != null && NotRootTypeSymbol(type); type = type.BaseType)
            {
                if (type.GetMembers("ToString").Any())
                {
                    return true;
                }
            }

            return false;
        }

        private bool NotRootTypeSymbol(ITypeSymbol type)
        {
            return !Equals(type, this.objectType) && !Equals(type, this.valueType);
        }

        public IEnumerable<LackingOverridenToStringResult> LackingOverridenToString(ArgumentListSyntax argumentList)
        {
            var arguments = argumentList.Arguments;

            if (arguments.Count == 2 && IsObjectArray(this.semanticModel.GetTypeInfo(arguments[1].Expression)))
            {
            }
            else if (arguments.Count == 2 && arguments[1].Expression is ImplicitArrayCreationExpressionSyntax)
            {
                var paramsArraryArgumentExpression = (ImplicitArrayCreationExpressionSyntax)arguments[1].Expression;

                foreach (var argument in paramsArraryArgumentExpression.Initializer.Expressions)
                {
                    var typeInfo = this.semanticModel.GetTypeInfo(argument);

                    if (LacksOverridenToString(typeInfo))
                    {
                        yield return new LackingOverridenToStringResult(argument, typeInfo);
                    }
                }
            }
            else
            {
                foreach (var argument in arguments.Skip(1))
                {
                    var typeInfo = this.semanticModel.GetTypeInfo(argument.Expression);

                    if (LacksOverridenToString(typeInfo))
                    {
                        yield return new LackingOverridenToStringResult(argument.Expression, typeInfo);
                    }
                }
            }
        }
    }
}