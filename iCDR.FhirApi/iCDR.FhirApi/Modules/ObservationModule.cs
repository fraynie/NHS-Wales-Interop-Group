using Hl7.Fhir.Model;

namespace iCDR.FhirApi.Modules
{
    public class ConditionModule : Nancy.NancyModule
    {
        public ConditionModule()
        {
            Get["/Observation/{id:int}"] = parameters => iCdrHelper.ProcessGetRequest<Observation>(this.Request, (int)parameters.id);

            Get["/Observation/{id:int}/_history"] = parameters => iCdrHelper.ProcessGetHistoryRequest<Observation>(this.Request, (int)parameters.id);

            Get["/Observation"] = _ => iCdrHelper.ProcessGetRequest<Observation>(this.Request);

            Post["/Observation"] = _ => iCdrHelper.ProcessPostRequest<Observation>(this.Request);

            Put["/Observation/{id:int}"] = _ => iCdrHelper.ProcessPutRequest<Observation>(this.Request);
        }
    }
}
