using System;
using Nancy;

namespace iCDR.FhirApi
{
    internal class FhirApiException : Exception
    {
        internal HttpStatusCode HttpStatusCode { get; set; }
        internal string HttpStatusCodeValue { get; set; }
        internal string HttpStatusCodeDescription { get; set; }
        internal string DiagnosticMessage { get; set; }


        internal FhirApiException(string message, string diagnosticMessage, HttpStatusCode httpStatusCode, string httpStatusCodeValue, string httpStatusCodeDescription) : base(message)
        {           
            this.HttpStatusCode = httpStatusCode;
            this.HttpStatusCodeValue = httpStatusCodeValue;
            this.HttpStatusCodeDescription = httpStatusCodeDescription;
            this.DiagnosticMessage = diagnosticMessage;                       
        }
    }
}
