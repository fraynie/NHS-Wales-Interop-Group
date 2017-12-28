using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Nancy;

namespace iCDR.FhirApi.Modules
{
    public class AllergyIntoleranceModule : Nancy.NancyModule
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

        public AllergyIntoleranceModule()
        {
            Get["/AllergyIntolerance/{id:int}"] = parameters => iCdrHelper.ProcessGetRequest<AllergyIntolerance>(this.Request, (int) parameters.id);

            Get["/AllergyIntolerance"] = parameters => iCdrHelper.ProcessGetRequest<AllergyIntolerance>(this.Request);

            Post["/AllergyIntolerance"] = _ => iCdrHelper.ProcessPostRequest<AllergyIntolerance>(this.Request, SearchFields);

            Put["/AllergyIntolerance"] = _ => iCdrHelper.ProcessPutRequest<AllergyIntolerance>(this.Request, SearchFields);
        }
    }


}
