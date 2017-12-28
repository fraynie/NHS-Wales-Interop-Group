using System;
using Nancy;

namespace iCDR.FhirApi
{
    internal class FhirApiException : Exception
    {
        internal HttpStatusCode HttpStatusCode { get; set; }
        internal string DiagnosticMessage { get; set; }

        internal FhirApiException(string message, string diagnosticMessage, HttpStatusCode httpStatusCode) : base(message)
        {           
            this.HttpStatusCode = httpStatusCode;
            this.DiagnosticMessage = diagnosticMessage;            
        }
    }
}
