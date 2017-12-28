using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Nancy;

namespace iCDR.FhirApi.Modules
{
    public class ProcedureModule : Nancy.NancyModule
    {
        internal static List<string> SearchFields // todo: find a better way to do this...
        {
            get
            {
                var searchFields = new List<string>
                {
                    "Code",
                    "Category"
                };
                return searchFields;
            }
        }

        public ProcedureModule()
        {
            Get["/Procedure/{id:int}"] = parameters => iCdrHelper.ProcessGetRequest<Procedure>(this.Request, (int) parameters.id);

            Get["/Procedure"] = parameters => iCdrHelper.ProcessGetRequest<Procedure>(this.Request);

            Post["/Procedure"] = _ => iCdrHelper.ProcessPostRequest<Procedure>(this.Request, SearchFields);

            Put["/Procedure"] = _ => iCdrHelper.ProcessPutRequest<Procedure>(this.Request, SearchFields);
        }
    }


}
