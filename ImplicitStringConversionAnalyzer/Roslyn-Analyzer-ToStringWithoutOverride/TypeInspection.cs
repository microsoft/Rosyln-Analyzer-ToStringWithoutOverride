using System.Linq;
using Microsoft.CodeAnalysis;

namespace Rosyln.Analyzer.ToStringWithOverride
{
    public class TypeInspection
    {
        private readonly INamedTypeSymbol stringType;
        private readonly INamedTypeSymbol objectType;

        public TypeInspection(SemanticModel semanticModel)
        {
            this.stringType = semanticModel.Compilation.GetSpecialType(SpecialType.System_String);
            this.objectType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
        }

        public bool IsReferenceTypeWithoutOverridenToString(TypeInfo typeInfo)
        {
            return NotStringType(typeInfo) && typeInfo.Type?.IsReferenceType == true && !Equals(typeInfo.Type, this.objectType) &&
                   TypeDidNotOverrideToString(typeInfo);
        }

        public bool NotStringType(TypeInfo typeInfo)
        {
            return !IsStringType(typeInfo);
        }

        public bool IsStringType(TypeInfo typeInfo)
        {
            return Equals(typeInfo.Type, this.stringType);
        }

        public bool TypeDidNotOverrideToString(TypeInfo typeInfo)
        {
            return !TypeHasOverridenToString(typeInfo);
        }

        public bool TypeHasOverridenToString(TypeInfo typeInfo)
        {
            for (var type = typeInfo.Type; type != null && !Equals(type, this.objectType); type = type.BaseType)
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