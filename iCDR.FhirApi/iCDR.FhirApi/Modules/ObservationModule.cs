using System.Collections.Generic;
using Hl7.Fhir.Model;

namespace iCDR.FhirApi.Modules
{
    public class ConditionModule : Nancy.NancyModule
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

        public ConditionModule()
        {
            Get["/Observation/{id:int}"] = parameters => iCdrHelper.ProcessGetRequest<Observation>(this.Request, (int)parameters.id);

            Get["/Observation"] = parameters => iCdrHelper.ProcessGetRequest<Observation>(this.Request);

            Post["/Observation"] = _ => iCdrHelper.ProcessPostRequest<Observation>(this.Request, SearchFields);

            Put["/Observation/{id}"] = _ => iCdrHelper.ProcessPutRequest<Observation>(this.Request, SearchFields);
        }
    }
}
