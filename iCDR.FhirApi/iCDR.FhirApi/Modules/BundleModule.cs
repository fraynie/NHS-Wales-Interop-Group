namespace iCDR.FhirApi.Modules
{
    public class BundleModule : Nancy.NancyModule
    {
        public BundleModule()
        {
            Post["/"] = _ => iCdrHelper.ProcessTransaction(this.Request);
        }
    }
}

