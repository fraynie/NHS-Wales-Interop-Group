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

        internal static Response ProcessTransaction(Request request)
        {
            //Resource response;
            try
            {
                var requestBundle = RetrieveFhirResourceFromRequest<Bundle>(request);
                if (requestBundle.Type != Bundle.BundleType.Batch)
                    throw new FhirApiException("Unable to hanlde this bundle type",
                        "FHIR API can only support 'batch' transactions", HttpStatusCode.NotImplemented, "501", "Not Implemented");

                Bundle responseBundle = new Bundle {Type = Bundle.BundleType.BatchResponse};

                foreach (Bundle.EntryComponent entry in requestBundle.Entry)
                {
                    try
                    {
                        if (entry.Request == null) throw new FhirApiException("Null entry request object", "Provide HTTP verb (i.e. GET | POST | PUT | DELETE)", HttpStatusCode.BadRequest, "400", "Bad Request");
                        if (entry.Request.Method.HasValue == false) throw new FhirApiException("HTTP verb not supplied", "Provide HTTP verb (i.e. GET | POST | PUT | DELETE)", HttpStatusCode.BadRequest, "400", "Bad Request");

                        if (entry.Request.Method.Value == Bundle.HTTPVerb.POST) 
                        {
                            // Save new resource
                            Resource createdResource = SaveNewFhirResource(entry.Resource);
                            var responseComponent = new Bundle.ResponseComponent
                            {
                                Status = "201 Created",
                                Location = $"{createdResource.TypeName}/{createdResource.Id}/_history/1",
                                Etag = "W/\"1\""
                            };
                            if (createdResource.Meta.LastUpdated.HasValue)
                                responseComponent.LastModified = createdResource.Meta.LastUpdated;

                            // Add response to entry (see https://www.hl7.org/fhir/bundle.html#transaction-response)
                            responseBundle.Entry.Add(new Bundle.EntryComponent
                            {
                                Resource = createdResource,
                                Response = responseComponent
                            });

                        }
                        else if (entry.Request.Method.Value == Bundle.HTTPVerb.PUT) 
                        {
                            // Update resource
                            Resource updatedResource = UpdateFhirResource(entry.Resource);
                            var responseComponent = new Bundle.ResponseComponent
                            {
                                Status = "200 OK",
                                Location = $"{updatedResource.TypeName}/{updatedResource.Id}/_history/{updatedResource.VersionId}",
                                Etag = $"W/\"{updatedResource.VersionId}\""
                            };
                            if (updatedResource.Meta.LastUpdated.HasValue)
                                responseComponent.LastModified = updatedResource.Meta.LastUpdated;

                            // Add response to entry (see https://www.hl7.org/fhir/bundle.html#transaction-response)
                            responseBundle.Entry.Add(new Bundle.EntryComponent
                            {
                                Resource = updatedResource,
                                Response = responseComponent
                            });

                        }
                        else
                        {
                            throw new FhirApiException("Unable to hanlde this HTTP verb",
                                "Only POST and PUT are supported ", HttpStatusCode.NotImplemented, "501", "Not Implemented");
                        }                                             
                    }
                    catch (FhirApiException ex)
                    {
                        // Create Response
                        OperationOutcome operationOutcomeResource = CreateOutcomeResponse(ex);
                        var responseComponent = new Bundle.ResponseComponent
                        {
                            Status = $"{ex.HttpStatusCodeValue} {ex.HttpStatusCodeDescription}", // If this is a FhirApiException, we need to return the associated Http Status code 
                            Outcome = operationOutcomeResource
                        };

                        // Add response to entry (see https://www.hl7.org/fhir/bundle.html#transaction-response)
                        responseBundle.Entry.Add(new Bundle.EntryComponent
                        {
                            Response = responseComponent
                        });
                    }
                    catch (Exception ex)
                    {
                        // Create Response
                        OperationOutcome operationOutcomeResource = CreateOutcomeResponse(ex);
                        var responseComponent = new Bundle.ResponseComponent
                        {
                            Status = "500 Internal Server Error", // If this is a 'general' Exception Http Status code will be 500
                            Outcome = operationOutcomeResource
                        };

                        // Add response to entry (see https://www.hl7.org/fhir/bundle.html#transaction-response)
                        responseBundle.Entry.Add(new Bundle.EntryComponent
                        {
                            Response = responseComponent
                        });
                    }
                }

                responseBundle.Total = responseBundle.Entry.Count;

                // we need to return the response bundle the client
                byte[] bytes;
                string contentType;
                if (DetermineResourceFormat(request) == ResourceFormat.Json)
                {
                    var jsonSerializer = new FhirJsonSerializer();
                    string json = jsonSerializer.SerializeToString(responseBundle);
                    bytes = Encoding.UTF8.GetBytes(json);
                    contentType = "application/fhir+json;charset=UTF-8";
                }
                else
                {
                    var xmlSerializer = new FhirXmlSerializer();
                    string xml = xmlSerializer.SerializeToString(responseBundle);
                    bytes = Encoding.UTF8.GetBytes(xml);
                    contentType = "application/fhir+xml;charset=UTF-8";
                }

                Response response = new Response
                {
                    StatusCode = HttpStatusCode.OK,
                    ContentType = contentType,
                    Headers = new Dictionary<string, string>
                            {
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

        internal static Response ProcessGetRequest<TResource>(Request request, int resourceId) where TResource : DomainResource
        {
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

        public static Response ProcessGetHistoryRequest<TResource>(Request request, int id) where TResource : Resource
        {
            try
            {
                Bundle bundle = GetFhirResourceHistory<TResource>(request, id);
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

        internal static Response ProcessPostRequest<TResource>(Request request)
            where TResource : DomainResource
        {
            try
            {
                var requestResource = RetrieveFhirResourceFromRequest<TResource>(request);
                Resource resource = SaveNewFhirResource(requestResource);

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

        internal static Response ProcessPutRequest<TResource>(Request request)
            where TResource : DomainResource
        {
            try
            {
                var requestResource = RetrieveFhirResourceFromRequest<TResource>(request);
                Resource resource = UpdateFhirResource(requestResource);

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
                throw new FhirApiException(ex.Message, $"Unable to parse FHIR resource - check resource is of type '{typeof(TResource)}'", HttpStatusCode.BadRequest, "400", "Bad Request");
            }
        }

        private static Bundle GetFhirResourceHistory<TResource>(Request request, int id) where TResource : Resource
        {
            var fhirXmlParser = new FhirXmlParser(new ParserSettings());
            List<TResource> historyResources = new List<TResource>();

            var iCdrDb = new iCDRDataContext();
            using (var sqlConnection = new SqlConnection(iCdrDb.Connection.ConnectionString))
            {
                var command = sqlConnection.CreateCommand();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetFhirResourceHistory";
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

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string resourceXml = reader[0].ToString();
                        try
                        {
                            TResource historyResource = fhirXmlParser.Parse<TResource>(resourceXml);
                            historyResources.Add(historyResource);
                        }
                        catch (Exception ex)
                        {
                            throw new FhirApiException(ex.Message, $"Unable to retrieve FHIR resource - check resource is of type '{typeof(TResource)}'", HttpStatusCode.BadRequest, "400", "Bad Request");
                        }
                    }
                }
            }

            // Create history bundle
            Bundle responseBundle = new Bundle
            {
                Type = Bundle.BundleType.History,
                Meta = new Meta {LastUpdated = DateTime.Now},
                Total = historyResources.Count
            };

            foreach (TResource historyResource in historyResources)
            {
                responseBundle.Entry.Add(new Bundle.EntryComponent
                {
                    FullUrl = $"{request.Url.SiteBase}/{historyResource.TypeName}/{historyResource.Id}/_history/{historyResource.VersionId}",
                    Resource = historyResource
                });
            }

            return responseBundle;

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
                        SqlDbType = SqlDbType.VarChar,                        
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
                    FullUrl = $"{request.Url.SiteBase}/{resource.TypeName}/{resource.Id}",
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
                case HttpStatusCode.NotImplemented:                    
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
                            throw new FhirApiException("Unable to determine the resource Content-Type", "Please check HTTP request header", HttpStatusCode.BadRequest, "400", "Bad Request");
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

        private static TResource RetrieveFhirResourceFromRequest<TResource>(Request request) where TResource : Resource
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
                throw new FhirApiException("Unable to determine the resource Content-Type", "Please check HTTP request header", HttpStatusCode.BadRequest, "400", "Bad Request");
            }
            return fhirResource;
        }

        private static Resource SaveNewFhirResource(Resource resource)
        {
            int? resourceId = 0;
            DateTime timeStamp = DateTime.Now;
            var iCdrDb = new iCDRDataContext();

            // Save the resource - step 1: Check the resource includes a contained patient resource with > 0 identifiers, and retrieve the identifiers
            List<Identifier> patientIdentifiers = GetPatientIdentifiers((DomainResource)resource);

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
            List<string> searchIndexFields = GetSearchFieldsForResourceType(resource.TypeName);
            foreach (string field in searchIndexFields)
            {
                AddSearchDataToIndex(field, (DomainResource)resource);
            }

            // Save the resource - step 7: Call GW's logging function 'prStoreResource'
            var jsonSerializer = new FhirJsonSerializer();
            string json = jsonSerializer.SerializeToString(resource);
            iCdrDb.prStoreResource(new XElement(xmlTree), json, "POST", "");


            // todo: need to consider how we can encapsulate sql inserts into a single transaction

            if (resourceId.HasValue)
            {
                return resource;
            }
            throw new FhirApiException("Unable to create resource", "It was not possible to create an id for the FHIR resource - check log", HttpStatusCode.InternalServerError, "500", "Internal Server Error");
        }

        private static Resource UpdateFhirResource(Resource resource)
        {
            int resourceId = Convert.ToInt32(resource.Id);
            int? currentVersion = 0;
            int version;
            var timeStamp = DateTime.Now;
            var iCdrDb = new iCDRDataContext();

            // Update the resource - step 1: Check the resource includes a contained patient resource with > 0 identifiers, and retrieve the identifiers
            List<Identifier> patientIdentifiers = GetPatientIdentifiers((DomainResource)resource);

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

            // Update the resource - step 3: Update resource metadata
            resource.Meta = new Meta
            {
                LastUpdated = timeStamp,
                VersionId = version.ToString()
            };

            // Update the resource - step 3: Save resource version
            var xmlSerializer = new FhirXmlSerializer();
            string xml = xmlSerializer.SerializeToString(resource);
            XElement xmlTree = XElement.Parse(xml);
            iCdrDb.CreateFhirResourceVersion(resourceId, version, timeStamp, new XElement(xmlTree));

            // Update the resource - step 4: Save patient identifiers
            foreach (var identifier in patientIdentifiers)
            {
                iCdrDb.CreatePatientIdentifierResourceLink(identifier.Value, identifier.System, resourceId);
            }

            // Update the resource - step 5: Clear search data for previous version and re-save search index data 
            iCdrDb.ClearSearchIndex(resourceId);
            List<string> searchIndexFields = GetSearchFieldsForResourceType(resource.TypeName);
            foreach (string field in searchIndexFields)
            {
                AddSearchDataToIndex(field, (DomainResource)resource);
            }


            // Update the resource - step 6: Call GW's logging function 'prStoreResource'
            var jsonSerializer = new FhirJsonSerializer();
            string json = jsonSerializer.SerializeToString(resource);
            iCdrDb.prStoreResource(new XElement(xmlTree), json, "PUT", "");

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
                        throw new FhirApiException("The contained patient resourse has zero identifiers", 
                            "Resources posted to this FHIR API must include a contained patient resource with > 0 identifiers", 
                            HttpStatusCode.UnprocessableEntity, "422", "Unprocessable Entity");
                    patientIdentifiers = patient.Identifier;
                    hasContainedPatient = true;
                    break;
                }
            }

            if (!hasContainedPatient)
                throw new FhirApiException(
                    $"The {resource.TypeName} reasource does not include a contained patient resource", 
                        "Resources posted to this FHIR API must include a contained patient resource with > 0 identifiers", 
                        HttpStatusCode.UnprocessableEntity, "422", "Unprocessable Entity");

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

        // ReSharper disable once UnusedParameter.Local TODO: fix this!!
        private static List<string> GetSearchFieldsForResourceType(string resourceType)
        {
            // TODO: hardcoded for now...
            var searchFields = new List<string>
                {
                    "Code",
                    "Category"
                };
            return searchFields;
        }
    }
}