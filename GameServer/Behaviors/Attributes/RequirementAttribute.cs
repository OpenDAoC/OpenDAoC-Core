using System;

namespace Core.GS.Behaviour.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RequirementAttribute :Attribute
    {
        private ERequirementType requirementType;

        public ERequirementType RequirementType
        {
            get { return requirementType; }
            set { requirementType = value; }
        }        

        private bool isNullableN;

        public bool IsNullableN
        {
            get { return isNullableN; }
            set { isNullableN = value; }
        }

        private bool isNullableV;

        public bool IsNullableV
        {
            get { return isNullableV; }
            set { isNullableV = value; }
        }

        private Object defaultValueN;

        public Object DefaultValueN
        {
            get { return defaultValueN; }
            set { defaultValueN = value; }
        }

        private Object defaultValueV;

        public Object DefaultValueV
        {
            get { return defaultValueV; }
            set { defaultValueV = value; }
        }
    }
}