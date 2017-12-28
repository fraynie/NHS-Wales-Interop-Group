using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace iCDR.FhirApi.Modules
{
    public class ObservationModule : Nancy.NancyModule
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

        public ObservationModule()
        {
            Get["/Condition/{id:int}"] = parameters => iCdrHelper.ProcessGetRequest<Condition>(this.Request, (int)parameters.id);

            Get["/Condition"] = parameters => iCdrHelper.ProcessGetRequest<Condition>(this.Request);

            Post["/Condition"] = _ => iCdrHelper.ProcessPostRequest<Condition>(this.Request, SearchFields);

            Put["/Condition"] = _ => iCdrHelper.ProcessPutRequest<Condition>(this.Request, SearchFields);
        }
    }
}
