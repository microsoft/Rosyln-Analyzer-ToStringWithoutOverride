using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ToStringWithoutOverrideAnalyzer
{
    public class TypeInspection
    {
        private readonly INamedTypeSymbol objectType;
        private readonly INamedTypeSymbol stringType;
        private readonly INamedTypeSymbol valueType;

        public TypeInspection(SemanticModel semanticModel)
        {
            this.objectType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            this.stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            this.valueType = semanticModel.Compilation.GetSpecialType(SpecialType.System_ValueType);
        }

        public bool LacksOverridenToString(TypeInfo typeInfo)
        {
            return !HasToString(typeInfo);
            //return typeInfo.Type?.IsReferenceType == true &&
        }
        
        private bool HasToString(TypeInfo typeInfo)
        {
            return IsString(typeInfo) || IsObject(typeInfo) || TypeHasOverridenToString(typeInfo);
        }

        private bool IsObject(TypeInfo typeInfo)
        {
            return Equals(typeInfo.Type, this.objectType);
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
    }
}