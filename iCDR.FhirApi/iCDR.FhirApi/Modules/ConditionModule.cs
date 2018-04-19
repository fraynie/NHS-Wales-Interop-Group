using Hl7.Fhir.Model;

namespace iCDR.FhirApi.Modules
{
    public class ObservationModule : Nancy.NancyModule
    {
        public ObservationModule()
        {
            Get["/Condition/{id:int}"] = parameters => iCdrHelper.ProcessGetRequest<Condition>(this.Request, (int)parameters.id);

            Get["/Condition/{id:int}/_history"] = parameters => iCdrHelper.ProcessGetHistoryRequest<Condition>(this.Request, (int)parameters.id);

            Get["/Condition"] = _ => iCdrHelper.ProcessGetRequest<Condition>(this.Request);

            Post["/Condition"] = _ => iCdrHelper.ProcessPostRequest<Condition>(this.Request);

            Put["/Condition/{id:int}"] = _ => iCdrHelper.ProcessPutRequest<Condition>(this.Request);
        }
    }
}
