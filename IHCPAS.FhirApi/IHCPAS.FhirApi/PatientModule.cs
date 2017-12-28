using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using log4net;
using Nancy;

namespace IHCPAS.FhirApi
{
    public class PatientModule : Nancy.NancyModule
    {

        private static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public PatientModule()
        {

            //Get["/Patient/{id:int}"] = parameters =>
            //{
            //    /////////////////////////////////////////////////////////////////////////////////////
            //    // EXAMPLE REQUEST:                                  
            //    // GET http://localhost:12349/patient/10098
            //    /////////////////////////////////////////////////////////////////////////////////////





            //    int id = (int)parameters.id; // this is the patient ID field

            //    var patient = new Patient(); // Hard-coded patient example
            //    patient.Id = id.ToString();
            //    patient.Identifier = new List<Identifier>
            //    {
            //        new Identifier
            //        {
            //            System = "https://fhir.nhs.uk/Id/nhs-number", // URL defined in CareConnect patient profile
            //            Value = "1231234568"
            //        },
            //        new Identifier
            //        {
            //            System = "urn:oid:2.16.840.1.113883.2.1.8.1.3.140", // use the full OID 
            //            Value = "X123456"
            //        }
            //    };
            //    patient.Active = true;
            //    patient.Name = new List<HumanName>
            //    {
            //        new HumanName
            //        {
            //            Prefix = new List<string> {"Mr"},
            //            Family = "Dunne",
            //            Given = new List<string> {"John", "Peter"}
            //        }
            //    };
            //    patient.Gender = AdministrativeGender.Male;
            //    patient.BirthDate = "1956-05-27";
            //    patient.Address = new List<Address>
            //    {
            //        new Address
            //        {
            //            Line = new List<string> {"100 Main Street"},
            //            City = "Barry",
            //            PostalCode = "CF62 1XX",
            //            Country = "UK"
            //        }
            //    };

            //    var response = CreateFhirResponse(this.Request, patient, HttpStatusCode.OK);
            //    return response;

            //};

            Get["/Patient"] = parameters =>
            {
                /////////////////////////////////////////////////////////////////////////////////////
                // EXAMPLE REQUESTS:                                  
                // GET http://localhost:12349/patient?given=John&family=Dunne&birthdate=1956-05-27
                //
                // GET http://localhost:12349/patient?identifier=https://fhir.nhs.uk/Id/nhs-number|1234567890 (NHS Number search)
                //
                // GET http://localhost:12349/patient?identifier=urn:oid:2.16.840.1.113883.2.1.8.1.3.140|X123456 (140 identifier search)
                /////////////////////////////////////////////////////////////////////////////////////

                // to get the query string you can do something like this...
                // string query = this.Request.Url.Query; // = "?given=John&family=Dunne&birthdate=1956-05-27", or "?identifier=urn:oid:2.16.840.1.113883.2.1.8.1.3.140|X123456" etc...                
                // ... and pass this value to your stored proc

                //for (int i = 0; i < this.Request.Query.)

                try
                {
                    string patientId = "";
                    string patientIdType = "";
                    string lastName = "";
                    string firstName = "";
                    string dob = "";
                    string address = "";
                    var patients = new List<Patient>();

                    var dd = (Nancy.DynamicDictionary) this.Request.Query;

                    dynamic value;
                    if (dd.TryGetValue("identifier", out value))
                    {
                        // search by patient identifier
                        string id = value.ToString();
                        if (!id.Contains("|")) throw new Exception("Unable to parse patient identifier value");
                        patientIdType = id.Split('|')[0] == "https://fhir.nhs.uk/Id/nhs-number"
                            ? "nhsnumber"
                            : "hospitalnumber";
                        patientId = id.Split('|')[1];
                    }
                    else
                    {
                        if (dd.TryGetValue("given", out value)) firstName = value.ToString();
                        if (dd.TryGetValue("family", out value)) lastName = value.ToString();
                        if (dd.TryGetValue("birthdate", out value)) dob = value.ToString();
                        if (dd.TryGetValue("address", out value)) address = value.ToString();
                    }

                    var ihcPasDb = new IHCPASDataContext();
                    using (var sqlConnection = new SqlConnection(ihcPasDb.Connection.ConnectionString))
                    {
                        var command = sqlConnection.CreateCommand();
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandText = "spSearchForPatient";
                        command.Parameters.Add(
                            new SqlParameter
                            {
                                ParameterName = "@PatientID",
                                Direction = ParameterDirection.Input,
                                SqlDbType = SqlDbType.VarChar,
                                Value = patientId
                            }
                        );
                        command.Parameters.Add(
                            new SqlParameter
                            {
                                ParameterName = "@PatientIDType",
                                Direction = ParameterDirection.Input,
                                SqlDbType = SqlDbType.VarChar,
                                Value = patientIdType
                            }
                        );
                        command.Parameters.Add(
                            new SqlParameter
                            {
                                ParameterName = "@LastName",
                                Direction = ParameterDirection.Input,
                                SqlDbType = SqlDbType.VarChar,
                                Value = lastName
                            }
                        );
                        command.Parameters.Add(
                            new SqlParameter
                            {
                                ParameterName = "@FirstName",
                                Direction = ParameterDirection.Input,
                                SqlDbType = SqlDbType.VarChar,
                                Value = firstName
                            }
                        );
                        if (string.IsNullOrEmpty(dob))
                        {
                            command.Parameters.Add(
                                new SqlParameter
                                {
                                    ParameterName = "@Dob",
                                    Direction = ParameterDirection.Input,
                                    SqlDbType = SqlDbType.VarChar,
                                    Value = DBNull.Value
                                }
                            );
                        }
                        else
                        {
                            DateTime dt = DateTime.Parse(dob);
                            dob = dt.ToString("dd-MMM-yyyy");

                            command.Parameters.Add(
                                new SqlParameter
                                {
                                    ParameterName = "@Dob",
                                    Direction = ParameterDirection.Input,
                                    SqlDbType = SqlDbType.VarChar,
                                    Value = dob
                                }
                            );
                        }
                        command.Parameters.Add(
                            new SqlParameter
                            {
                                ParameterName = "@Address",
                                Direction = ParameterDirection.Input,
                                SqlDbType = SqlDbType.VarChar,
                                Value = address
                            }
                        );

                        sqlConnection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var patient = new Patient();
                                patient.Identifier = new List<Identifier>
                                {
                                    new Identifier
                                    {
                                        System = "https://fhir.nhs.uk/Id/nhs-number",
                                        // URL defined in CareConnect patient profile
                                        Value = (string) reader["NHSNUMBER"]
                                    },
                                    new Identifier
                                    {
                                        System = "urn:oid:2.16.840.1.113883.2.1.8.1.3.140", // use the full OID 
                                        Value = (string) reader["HOSPITALNUMBER"]
                                    }
                                };
                                patient.Active = true;
                                string title = "";
                                int i = reader.GetOrdinal("TITLE");
                                if (!reader.IsDBNull(i)) title = (string) reader["TITLE"];
                                patient.Name = new List<HumanName>
                                {
                                    new HumanName
                                    {
                                        Prefix = new List<string> {title},
                                        Family = (string) reader["LASTNAME"],
                                        Given =
                                            new List<string>
                                            {
                                                (string) reader["FIRSTNAME"],
                                                (string) reader["OTHERNAMES"]
                                            }
                                    }
                                };
                                var gender = (string) reader["Gender"];
                                patient.Gender = AdministrativeGender.Unknown;
                                if (gender.ToUpper().StartsWith("M")) patient.Gender = AdministrativeGender.Male;
                                if (gender.ToUpper().StartsWith("F")) patient.Gender = AdministrativeGender.Female;
                                if (gender.ToUpper().StartsWith("O")) patient.Gender = AdministrativeGender.Other;
                                var doby = (DateTime) reader["Dob"];
                                patient.BirthDate = doby.ToString("yyyy-MM-dd");
                                patient.Address = new List<Address>
                                {
                                    new Address
                                    {
                                        Line =
                                            new List<string> {(string) reader["address1"], (string) reader["address2"]},
                                        District = (string) reader["address3"],
                                        City = (string) reader["address4"],
                                        PostalCode = (string) reader["postcode"],
                                        Country = "UK"
                                    }
                                };
                                patients.Add(patient);
                            }
                        }
                    }

                    Bundle bundle = new Bundle
                    {
                        Meta = new Meta {LastUpdated = DateTime.Now},
                        Type = Bundle.BundleType.Searchset,
                        Total = patients.Count,
                        Link = new List<Bundle.LinkComponent>
                        {
                            new Bundle.LinkComponent
                            {
                                Relation = "self",
                                Url = this.Request.Url
                            }
                        }

                    };

                    foreach (var patient in patients)
                    {
                        bundle.Entry.Insert(0, new Bundle.EntryComponent
                        {
                            Resource = patient,
                            Search = new Bundle.SearchComponent
                            {
                                Mode = Bundle.SearchEntryMode.Match
                            }
                        });
                    }
                    var response = CreateFhirResponse(this.Request, bundle, HttpStatusCode.OK);

                    Logger.Info($"{this.Request.Method} {this.Request.Url} OK");

                    return response;
                }
                catch (Exception ex)
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
                                Severity = OperationOutcome.IssueSeverity.Error,
                                Code = OperationOutcome.IssueType.Exception,
                                Details = new CodeableConcept { Text = "An error has occurred - please check log for details" },
                                Diagnostics = "An error has occurred - please check log for details"
                            }
                        },
                    };
                    var response = CreateFhirResponse(this.Request, opOutcome,
                        HttpStatusCode.InternalServerError);

                    Logger.Error($"{this.Request.Method} {this.Request.Url} InternalServerError");
                    Logger.Debug(ex);

                    return response;
                }
            };
        }

        private static Response CreateFhirResponse(Request request, Resource fhirResource, HttpStatusCode httpStatusCode)
        {
            ResourceFormat format = DetermineResourceFormat(request);

            string body;
            string contentType;
            if (format == ResourceFormat.Json)
            {
                body = FhirSerializer.SerializeResourceToJson(fhirResource);
                contentType = "application/fhir+json";
            }
            else
            {
                body = FhirSerializer.SerializeResourceToXml(fhirResource);
                contentType = "application/fhir+xml";
            }

            byte[] bytes = Encoding.UTF8.GetBytes(body);

            return new Response()
            {
                StatusCode = httpStatusCode,
                ContentType = contentType,
                Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", contentType }
                    },
                Contents = c => c.Write(bytes, 0, bytes.Length)
            };
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

    }
}
