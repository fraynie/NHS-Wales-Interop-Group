# NHS Wales Interop Group
This GitHub project has been created to share code and useful resources.

The project currently contains two proof-of-concept FHIR APIs to provide read only access to Patient resources, and CRUD operations for a limited set of other FHIR resources (AllergyIntolerance, Condition, Procedure and Observation). Both projects make use of Nancy (see http://nancyfx.org/) which provides a lightweight and simple framework to create RESTful services. *Database Schemas to follow*

1)	The IHCPAS.FhirApi service allows access to a mock PAS system providing patient demographic records and search functionality against patient identifiers and basic demographic information (i.e. name, dob and address). Example requests:
	o	http://{server}:12349/patient?given=irene&family=jones&birthdate=1948-12-29 
	o	http://{server}:12349/patient?identifier=urn:oid:2.16.840.1.113883.2.1.8.1.3.140|E100012 
   
2)  The iCDR.FhirApi service allows CRUD and limited search operations against AllergyIntolerance, Condition, Observation and Procedure FHIR resources. Example requests:
	o	http://{server}:12345/observation/2036 	

