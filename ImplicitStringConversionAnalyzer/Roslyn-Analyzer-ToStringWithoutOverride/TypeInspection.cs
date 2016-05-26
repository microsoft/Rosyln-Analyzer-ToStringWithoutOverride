using System.Linq;
using Microsoft.CodeAnalysis;

namespace ImplicitStringConversionAnalyzer
{
    public class TypeInspection
    {
        private readonly INamedTypeSymbol stringType;
        private readonly INamedTypeSymbol objectType;

        public TypeInspection(SemanticModel semanticModel)
        {
            stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            objectType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
        }

        public bool IsReferenceTypeWithoutOverridenToString(TypeInfo typeInfo)
        {
            return NotStringType(typeInfo) && typeInfo.Type?.IsReferenceType == true && !Equals(typeInfo.Type, objectType) &&
                   TypeDidNotOverrideToString(typeInfo);
        }

        public bool NotStringType(TypeInfo typeInfo)
        {
            return !IsStringType(typeInfo);
        }

        public bool IsStringType(TypeInfo typeInfo)
        {
            return Equals(typeInfo.Type, stringType);
        }

        public bool TypeDidNotOverrideToString(TypeInfo typeInfo)
        {
            return !TypeHasOverridenToString(typeInfo);
        }

        public bool TypeHasOverridenToString(TypeInfo typeInfo)
        {
            for (var type = typeInfo.Type; type != null && !Equals(type, objectType); type = type.BaseType)
            {
                if (type.GetMembers("ToString").Any())
                {
                    return true;
                }
            }

            return false;
        }
    }
}