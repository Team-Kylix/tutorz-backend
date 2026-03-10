using System;

namespace Tutorz.Api.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ApiPurposeAttribute : Attribute
    {
        public string Purpose { get; }

        public ApiPurposeAttribute(string purpose)
        {
            Purpose = purpose;
        }
    }
}
