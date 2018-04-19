using Hl7.Fhir.Model;

namespace iCDR.FhirApi.Modules
{
    public class ProcedureModule : Nancy.NancyModule
    {
        public ProcedureModule()
        {
            Get["/Procedure/{id:int}"] = parameters => iCdrHelper.ProcessGetRequest<Procedure>(this.Request, (int) parameters.id);
       
            Get["/Procedure/{id:int}/_history"] = parameters => iCdrHelper.ProcessGetHistoryRequest<Procedure>(this.Request, (int)parameters.id);

            Get["/Procedure"] = _ => iCdrHelper.ProcessGetRequest<Procedure>(this.Request);

            Post["/Procedure"] = _ => iCdrHelper.ProcessPostRequest<Procedure>(this.Request);

            Put["/Procedure/{id:int}"] = _ => iCdrHelper.ProcessPutRequest<Procedure>(this.Request);
        }
    }


}
