using Hl7.Fhir.Model;

namespace iCDR.FhirApi.Modules
{
    public class AllergyIntoleranceModule : Nancy.NancyModule
    {
        public AllergyIntoleranceModule()
        {
            Get["/AllergyIntolerance/{id:int}"] = parameters => iCdrHelper.ProcessGetRequest<AllergyIntolerance>(this.Request, (int) parameters.id);

            Get["/AllergyIntolerance/{id:int}/_history"] = parameters => iCdrHelper.ProcessGetHistoryRequest<AllergyIntolerance>(this.Request, (int)parameters.id);

            Get["/AllergyIntolerance"] = _ => iCdrHelper.ProcessGetRequest<AllergyIntolerance>(this.Request);

            Post["/AllergyIntolerance"] = _ => iCdrHelper.ProcessPostRequest<AllergyIntolerance>(this.Request);

            Put["/AllergyIntolerance/{id:int}"] = _ => iCdrHelper.ProcessPutRequest<AllergyIntolerance>(this.Request);
        }
    }
}
