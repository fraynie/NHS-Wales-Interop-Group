using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using log4net;
using Nancy;
using Nancy.Helpers;
using Nancy.IO;

namespace iCDR.FhirApi
{
    // ReSharper disable once InconsistentNaming
    internal class iCdrHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal static Response ProcessGetRequest<TResource>(Request request, int resourceId) where TResource : DomainResource
        {
            //Logger.InfoFormat("GET {0}", request.Url);

            try
            {
                var resource = GetFhirResourceById<TResource>(resourceId);
                Response response = CreateFhirResponse(request, resource, null);

                return response;
            }
            catch (FhirApiException ex)
            {
                return CreateFhirResponse(request, CreateOutcomeResponse(ex), ex);
            }
            catch (Exception ex)
            {
                return CreateFhirResponse(request, CreateOutcomeResponse(ex), ex);
            }
        }

        internal static Response ProcessGetRequest<TResource>(Request request) where TResource : DomainResource
        {
            //Logger.InfoFormat("GET {0}", request.Url);

            try
            {
                Bundle bundle = SearchFhirResources<TResource>(request);
                Response response = CreateFhirResponse(request, bundle, null);
                return response;
            }
            catch (FhirApiException ex)
            {
                return CreateFhirResponse(request, CreateOutcomeResponse(ex), ex);
            }
            catch (Exception ex)
            {
                return CreateFhirResponse(request, CreateOutcomeResponse(ex), ex);
            }

        }

        internal static Response ProcessPostRequest<TResource>(Request request, List<string> searchFields)
            where TResource : DomainResource
        {
            try
            {
                var requestResource = RetrieveFhirResourceFromRequest<TResource>(request);
                Resource resource = SaveNewFhirResource(requestResource, searchFields);

                // see https://www.hl7.org/fhir/http.html#create for required HTTP headers for response
                string location = $"{request.Url}/{resource.Id}/_history/1";

                // we need to return the newly created resource to the client
                byte[] bytes;
                string contentType;
                if (DetermineResourceFormat(request) == ResourceFormat.Json)
                {
                    var jsonSerializer = new FhirJsonSerializer();
                    string json = jsonSerializer.SerializeToString(resource);
                    bytes = Encoding.UTF8.GetBytes(json);
                    contentType = "application/fhir+json;charset=UTF-8";
                }
                else
                {
                    var xmlSerializer = new FhirXmlSerializer();
                    string xml = xmlSerializer.SerializeToString(resource);
                    bytes = Encoding.UTF8.GetBytes(xml);
                    contentType = "application/fhir+xml;charset=UTF-8";
                }

                Response response = new Response
                {
                    StatusCode = HttpStatusCode.Created,
                    ContentType = contentType,
                    Headers = new Dictionary<string, string>
                    {
                        { "Location", location },
                        { "ETag", "W/\"1\"" }, // this ETag indicates the resource version - newly created resources are always version 1
                        { "Content-Type", contentType }
                    },
                    Contents = c => c.Write(bytes, 0, bytes.Length)
                };

                Logger.Info($"{request.Method} {request.Url} {HttpStatusCode.Created}");
                return response;
            }
            catch (FhirApiException ex)
            {
                return CreateFhirResponse(request, CreateOutcomeResponse(ex), ex);
            }
            catch (Exception ex)
            {
                return CreateFhirResponse(request, CreateOutcomeResponse(ex), ex);
            }
        }

        internal static Response ProcessPutRequest<TResource>(Request request, List<string> searchFields)
            where TResource : DomainResource
        {
            try
            {
                var requestResource = RetrieveFhirResourceFromRequest<TResource>(request);
                Resource resource = UpdateFhirResource(requestResource, searchFields);

                // see https://www.hl7.org/fhir/http.html#update for required HTTP headers for response
                string location = $"{request.Url}/{resource.Id}/_history/{resource.Meta.VersionId}";

                // we need to return the newly updated resource to the client
                byte[] bytes;
                string contentType;
                if (DetermineResourceFormat(request) == ResourceFormat.Json)
                {
                    var jsonSerializer = new FhirJsonSerializer();
                    string json = jsonSerializer.SerializeToString(resource);
                    bytes = Encoding.UTF8.GetBytes(json);
                    contentType = "application/fhir+json;charset=UTF-8";
                }
                else
                {
                    var xmlSerializer = new FhirXmlSerializer();
                    string xml = xmlSerializer.SerializeToString(resource);
                    bytes = Encoding.UTF8.GetBytes(xml);
                    contentType = "application/fhir+xml;charset=UTF-8";
                }

                Response response = new Response
                {
                    StatusCode = HttpStatusCode.OK,
                    ContentType = contentType,
                    Headers = new Dictionary<string, string>
                    {
                        { "Location", location },
                        { "ETag", $"W/\"{resource.Meta.VersionId}\"" }, // this ETag indicates the resource version
			            { "Content-Type", contentType }
                    },
                    Contents = c => c.Write(bytes, 0, bytes.Length)
                };

                Logger.Info($"{request.Method} {request.Url} {HttpStatusCode.OK}");
                return response;

            }
            catch (FhirApiException ex)
            {
                return CreateFhirResponse(request, CreateOutcomeResponse(ex), ex);
            }
            catch (Exception ex)
            {
                return CreateFhirResponse(request, CreateOutcomeResponse(ex), ex);
            }
        }

        private static OperationOutcome CreateOutcomeResponse(Exception ex)
        {
            return CreateOutcomeResponse(ex.Message, "", OperationOutcome.IssueSeverity.Error,
                    OperationOutcome.IssueType.Exception);
        }

        private static OperationOutcome CreateOutcomeResponse(FhirApiException ex)
        {
            return CreateOutcomeResponse(ex.Message, ex.DiagnosticMessage, OperationOutcome.IssueSeverity.Error,
                OperationOutcome.IssueType.Exception);
        }

        private static OperationOutcome CreateOutcomeResponse(string message, string diagnosticInformation,
            OperationOutcome.IssueSeverity severity, OperationOutcome.IssueType type)
        {
            var opOutcome = new OperationOutcome
            {
                Text = new Narrative
                {
                    Status = Narrative.NarrativeStatus.Generated
                    //Div = message
                },
                Issue = new List<OperationOutcome.IssueComponent>
                {
                    new OperationOutcome.IssueComponent
                    {
                        Severity = severity,
                        Code = type,
                        Details = new CodeableConcept { Text = message },
                        Diagnostics = diagnosticInformation
                    }
                },
            };
            return opOutcome;
        }

        private static ResourceFormat DetermineResourceFormat(Request request)
        {
            ResourceFormat format = ResourceFormat.Unknown;
            RequestHeaders headers = request.Headers;
            foreach (var tuple in headers.Accept)
            {
                if (tuple.Item1.ToLower().Contains("application/fhir+json"))
                {
                    format = ResourceFormat.Json;
                    break;
                }
                else if (tuple.Item1.ToLower().Contains("application/fhir+xml"))
                {
                    format = ResourceFormat.Xml;
                    break;
                }
            }
            return format;
        }

        private static TResource GetFhirResourceById<TResource>(int id) where TResource : Resource
        {
            string resourceXml = "";
            var iCdrDb = new iCDRDataContext();
            using (var sqlConnection = new SqlConnection(iCdrDb.Connection.ConnectionString))
            {
                var command = sqlConnection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetFhirResourceById";
                command.Parameters.Add(
                    new SqlParameter
                    {
                        ParameterName = "@ResourceId",
                        Direction = ParameterDirection.Input,
                        SqlDbType = SqlDbType.Int,
                        Value = id
                    }
                );
                sqlConnection.Open();

                using (XmlReader reader = command.ExecuteXmlReader())
                {
                    while (reader.Read())
                    {
                        resourceXml = reader.ReadOuterXml();
                    }
                }
            }

            var fhirXmlParser = new FhirXmlParser(new ParserSettings());
            try
            {
                return fhirXmlParser.Parse<TResource>(resourceXml);
            }
            catch (Exception ex)
            {
                throw new FhirApiException(ex.Message, $"Unable to parse FHIR resource - check resource is of type '{typeof(TResource)}'", HttpStatusCode.BadRequest);
            }
        }

        private static Bundle SearchFhirResources<TResource>(Request request) where TResource : Resource
        {
            var path = request.Path.TrimStart('/');
            var queryString = HttpUtility.UrlDecode(path + request.Url.Query);
            var resources = new List<TResource>();

            var iCdrDb = new iCDRDataContext();
            using (var sqlConnection = new SqlConnection(iCdrDb.Connection.ConnectionString))
            {
                var command = sqlConnection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "SearchResource";
                command.Parameters.Add(
                    new SqlParameter
                    {
                        ParameterName = "@QueryString",
                        Direction = ParameterDirection.Input,
                        SqlDbType = SqlDbType.Text,
                        Value = queryString
                    }
                );
                sqlConnection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string resourceXml = reader[0].ToString();
                        var fhirXmlParser = new FhirXmlParser(new ParserSettings());
                        TResource fhirResource = fhirXmlParser.Parse<TResource>(resourceXml);
                        resources.Add(fhirResource);
                    }
                }
            }

            Bundle respBundle = new Bundle
            {
                Meta = new Meta { LastUpdated = DateTime.Now },
                Type = Bundle.BundleType.Searchset,
                Total = resources.Count,
                Link = new List<Bundle.LinkComponent>
                {
                    new Bundle.LinkComponent
                    {
                        Relation = "self",
                        Url = request.Url
                    }
                }

            };

            foreach (TResource resource in resources)
            {
                respBundle.Entry.Insert(0, new Bundle.EntryComponent
                {
                    Resource = resource,
                    Search = new Bundle.SearchComponent
                    {
                        Mode = Bundle.SearchEntryMode.Match
                    }
                });
            }


            return respBundle;
        }

        /// <summary>
        /// This method generates the requested resource, bundle, or OperationOutcome
        /// as an XML or JSON response. All outcomes (including errors) are handled 
        /// and logged here, apart from POST and PUT results
        /// </summary>
        /// <param name="request">the HttpRequest requested by the client system</param>
        /// <param name="fhirResource">the resource to be returned to the client system</param>
        /// <param name="ex">any exception</param>
        /// <returns></returns>
        private static Response CreateFhirResponse(Request request, Resource fhirResource, Exception ex)
        {
            ResourceFormat format = DetermineResourceFormat(request);

            HttpStatusCode httpStatusCode;
            if (ex == null)
            {
                httpStatusCode = HttpStatusCode.OK;
            }
            else
            {
                if (ex.GetType() == typeof(FhirApiException))
                {
                    var fhirApiEx = (FhirApiException)ex;
                    httpStatusCode = fhirApiEx.HttpStatusCode;
                }
                else httpStatusCode = HttpStatusCode.InternalServerError;
            }

            string body;
            string contentType;
            if (format == ResourceFormat.Json)
            {
                var jsonSerializer = new FhirJsonSerializer();
                body = jsonSerializer.SerializeToString(fhirResource);
                contentType = "application/fhir+json";
            }
            else // assume xml as default
            {
                var xmlSerializer = new FhirXmlSerializer();
                body = xmlSerializer.SerializeToString(fhirResource);
                contentType = "application/fhir+xml";
            }

            byte[] bytes = Encoding.UTF8.GetBytes(body);

            var response = new Response()
            {
                StatusCode = httpStatusCode,
                ContentType = contentType,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", contentType }
                },
                Contents = c => c.Write(bytes, 0, bytes.Length)
            };

            switch (httpStatusCode)
            {
                case HttpStatusCode.OK:
                    Logger.Info($"{request.Method} {request.Url} {httpStatusCode}");
                    break;
                case HttpStatusCode.BadRequest:
                case HttpStatusCode.UnprocessableEntity:                
                    Logger.WarnFormat($"{request.Method} {request.Url} {httpStatusCode}");
                    Logger.Debug(ex);
                    if (request.Method == "POST" || request.Method == "PUT") LogFhirRequestBody(request);
                    break;
                default:
                    Logger.Error($"{request.Method} {request.Url} {httpStatusCode}");
                    Logger.Debug(ex);
                    if (request.Method == "POST" || request.Method == "PUT") LogFhirRequestBody(request);
                    break;
            }
            return response;
        }

        private static void LogFhirRequestBody(Request request)
        {
            string debugText = "";
            string body = "";

            if (Logger.IsDebugEnabled)
            {
                try
                {
                    request.Body.Position = 0;
                    body = GetRequestBody(request).Trim();

                    string contentType = request.Headers.ContentType;

                    if (contentType.ToLower().Contains("text/xml"))
                    {
                        var doc = new XmlDocument { PreserveWhitespace = false };
                        doc.LoadXml(body);
                        debugText = doc.OuterXml;
                    }
                    else if (contentType.ToLower().Contains("application/json"))
                    {
                        debugText = Regex.Replace(body, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1");
                    }
                    else
                    {
                        {
                            throw new FhirApiException("Unable to determine the resource Content-Type", "Please check HTTP request header", HttpStatusCode.BadRequest);
                        }
                    }
                }
                catch
                {
                    debugText = body;
                }
                finally
                {
                    Logger.Debug(debugText);
                }
            }
        }
        private static string GetRequestBody(Request request)
        {
            RequestStream stream = request.Body;
            var length = request.Body.Length;
            var data = new byte[length];
            stream.Read(data, 0, (int)length);
            string body = Encoding.Default.GetString(data);
            return body;
        }

        private static TResource RetrieveFhirResourceFromRequest<TResource>(Request request) where TResource : DomainResource
        {
            TResource fhirResource;
            string contentType = request.Headers.ContentType;

            // get the text of the request body
            string body = GetRequestBody(request);

            // determine the content type (XML or JSON)
            if (contentType.ToLower().Contains("text/xml") || contentType.ToLower().Contains("application/fhir+xml"))
            {
                var fhirXmlParser = new FhirXmlParser(new ParserSettings());
                fhirResource = fhirXmlParser.Parse<TResource>(body);
            }
            else if (contentType.ToLower().Contains("application/json") || contentType.ToLower().Contains("application/fhir+json"))
            {
                var fhirJsonParser = new FhirJsonParser(new ParserSettings());
                fhirResource = fhirJsonParser.Parse<TResource>(body);
            }
            else
            {
                throw new FhirApiException("Unable to determine the resource Content-Type", "Please check HTTP request header", HttpStatusCode.BadRequest);
            }
            return fhirResource;
        }

        private static Resource SaveNewFhirResource(DomainResource resource, List<string> searchIndexFields)
        {
            int? resourceId = 0;
            DateTime timeStamp = DateTime.Now;
            var iCdrDb = new iCDRDataContext();

            // Save the resource - step 1: Check the resource includes a contained patient resource with > 0 identifiers, and retrieve the identifiers
            List<Identifier> patientIdentifiers = GetPatientIdentifiers(resource);

            // Save the resource - step 2: Get the resource id
            iCdrDb.CreateFhirResourceId(resource.ResourceType.ToString(), ref resourceId);

            // Save the resource - step 3: Update resource metadata
            resource.Id = resourceId.ToString();
            resource.Meta = new Meta
            {
                LastUpdated = timeStamp,
                VersionId = "1"
            };

            // Save the resource - step 4: Save resource version
            var xmlSerializer = new FhirXmlSerializer();
            string xml = xmlSerializer.SerializeToString(resource);
            XElement xmlTree = XElement.Parse(xml);
            iCdrDb.CreateFhirResourceVersion(resourceId, 1, timeStamp, new XElement(xmlTree));

            // Save the resource - step 5: Save patient identifiers
            foreach (Identifier identifier in patientIdentifiers)
            {
                iCdrDb.CreatePatientIdentifierResourceLink(identifier.Value, identifier.System, resourceId);
            }

            // Save the resource - step 6: Save search index data 
            foreach (string field in searchIndexFields)
            {
                AddSearchDataToIndex(field, resource);
            }

            // todo: need to consider how we can encapsulate sql inserts into a single transaction

            if (resourceId.HasValue)
            {
                return resource;
            }
            throw new FhirApiException("Unable to create resource", "It was not possible to create an id for the FHIR resource - check log", HttpStatusCode.InternalServerError);
        }

        private static Resource UpdateFhirResource(DomainResource resource, List<string> searchIndexFields)
        {
            int resourceId = Convert.ToInt32(resource.Id);
            int? currentVersion = 0;
            int version;
            var timeStamp = DateTime.Now;
            var iCdrDb = new iCDRDataContext();

            // Save the resource - step 1: Check the resource includes a contained patient resource with > 0 identifiers, and retrieve the identifiers
            List<Identifier> patientIdentifiers = GetPatientIdentifiers(resource);

            // Save the resource - step 2: Get the current version of the resource to determine the next version number
            iCdrDb.GetFhirResourceCurrentVersion(resourceId, ref currentVersion);
            if (currentVersion.HasValue)
            {
                version = currentVersion.Value + 1;
            }
            else
            {
                throw new Exception("Unable to determine new version number");
            }

            // Save the resource - step 3: Update resource metadata
            resource.Meta = new Meta
            {
                LastUpdated = timeStamp,
                VersionId = version.ToString()
            };

            // Save the resource - step 3: Save resource version
            var xmlSerializer = new FhirXmlSerializer();
            string xml = xmlSerializer.SerializeToString(resource);
            XElement xmlTree = XElement.Parse(xml);
            iCdrDb.CreateFhirResourceVersion(resourceId, version, timeStamp, new XElement(xmlTree));

            // Save the resource - step 4: Save patient identifiers
            foreach (var identifier in patientIdentifiers)
            {
                iCdrDb.CreatePatientIdentifierResourceLink(identifier.Value, identifier.System, resourceId);
            }

            // Save the resource - step 5: Clear search data for previous version and re-save search index data 
            iCdrDb.ClearSearchIndex(resourceId);
            foreach (string field in searchIndexFields)
            {
                AddSearchDataToIndex(field, resource);
            }

            return resource;
        }

        private static List<Identifier> GetPatientIdentifiers(DomainResource resource)
        {
            var hasContainedPatient = false;
            List<Identifier> patientIdentifiers = new List<Identifier>();
            foreach (Resource containedResource in resource.Contained)
            {
                if (containedResource.TypeName == "Patient")
                {
                    var patient = (Patient)containedResource;
                    if (patient.Identifier.Count == 0)
                        throw new FhirApiException("The contained patient resourse has zero identifiers", "Resources posted to this FHIR API must include a contained patient resource with > 0 identifiers", HttpStatusCode.UnprocessableEntity);
                    patientIdentifiers = patient.Identifier;
                    hasContainedPatient = true;
                    break;
                }
            }

            if (!hasContainedPatient)
                throw new FhirApiException(
                    $"The {resource.TypeName} reasource does not include a contained patient resource", "Resources posted to this FHIR API must include a contained patient resource with > 0 identifiers", HttpStatusCode.UnprocessableEntity);

            return patientIdentifiers;
        }

        private static void AddSearchDataToIndex(string field, DomainResource resource)
        {
            int resourceId = Convert.ToInt32(resource.Id);
            PropertyInfo prop = resource.GetType().GetProperty(field);
            if (prop != null)
            {
                if (prop.GetValue(resource, null) is IEnumerable)
                {
                    var collection = (IEnumerable)prop.GetValue(resource, null);
                    foreach (object obj in collection)
                    {
                        AddSearchDataToIndexInternal(resourceId, field, obj);
                    }
                }
                else
                {
                    object obj = prop.GetValue(resource, null);
                    AddSearchDataToIndexInternal(resourceId, field, obj);
                }
            }
        }

        private static void AddSearchDataToIndexInternal(int resourceId, string field, object obj)
        {
            if (obj != null)
            {
                if (obj is Enum)
                {
                    AddToIndex(resourceId, field, obj.ToString());
                }
                else
                {
                    if (obj.GetType() == typeof(CodeableConcept))
                    {
                        var codeableConcept = (CodeableConcept) obj;
                        foreach (Coding c in codeableConcept.Coding)
                        {
                            if (String.IsNullOrEmpty(c.System))
                            {
                                AddToIndex(resourceId, field, c.Code);
                            }
                            else
                            {
                                AddToIndex(resourceId, field, $"{c.System}|{c.Code}");
                            }
                        }
                    }
                }
            }
        }

        private static void AddToIndex(int resourceId, string field, string value)
        {
            var iCdrDb = new iCDRDataContext();
            iCdrDb.AddToSearchIndex(resourceId, field, value);
        }
    }
}
