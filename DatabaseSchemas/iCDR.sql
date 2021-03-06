USE [iCDR]
GO
/****** Object:  User [iCDR]    Script Date: 29/12/2017 11:26:42 ******/
CREATE USER [iCDR] FOR LOGIN [iCDR] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [iCDR]
GO
/****** Object:  Table [dbo].[PatientIdentifier]    Script Date: 29/12/2017 11:26:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PatientIdentifier](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Value] [varchar](50) NOT NULL,
	[System] [varchar](50) NOT NULL,
	[DateTimeUpdated] [datetime] NOT NULL,
 CONSTRAINT [PK_PatientIdentifier] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PatientIdentifierResourceLink]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PatientIdentifierResourceLink](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ResourceId] [int] NOT NULL,
	[PatientIdentifierId] [int] NOT NULL,
 CONSTRAINT [PK_PatientIdentifierResourceLink] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Resource]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Resource](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TypeId] [int] NOT NULL,
 CONSTRAINT [PK_Resource] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ResourceSearchIndex]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ResourceSearchIndex](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ResourceId] [int] NOT NULL,
	[Field] [varchar](50) NOT NULL,
	[Value] [varchar](100) NOT NULL,
 CONSTRAINT [PK_ResourceSearchIndex] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ResourceType]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ResourceType](
	[Id] [int] NOT NULL,
	[Type] [varchar](50) NOT NULL,
 CONSTRAINT [PK_ResourceType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ResourceVersion]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ResourceVersion](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ResourceId] [int] NOT NULL,
	[Version] [int] NOT NULL,
	[DateTimeCreated] [datetime] NOT NULL,
	[ResourceXml] [xml] NOT NULL,
 CONSTRAINT [PK_ResourceVersion] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[SearchLog]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[SearchLog](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[paramter] [varchar](max) NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  UserDefinedFunction [dbo].[fnSplitStringsIntoTable]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[fnSplitStringsIntoTable]
(
   @List NVARCHAR(MAX),
   @Delimiter NVARCHAR(255)
)
RETURNS TABLE
WITH SCHEMABINDING AS
RETURN
  WITH E1(N)        AS ( SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 
                         UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 
                         UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1),
       E2(N)        AS (SELECT 1 FROM E1 a, E1 b),
       E4(N)        AS (SELECT 1 FROM E2 a, E2 b),
       E42(N)       AS (SELECT 1 FROM E4 a, E2 b),
       cteTally(N)  AS (SELECT 0 UNION ALL SELECT TOP (DATALENGTH(ISNULL(@List,1))) 
                         ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) FROM E42),
       cteStart(N1) AS (SELECT t.N+1 FROM cteTally t
                         WHERE (SUBSTRING(@List,t.N,1) = @Delimiter OR t.N = 0))
  SELECT Item = SUBSTRING(@List, s.N1, ISNULL(NULLIF(CHARINDEX(@Delimiter,@List,s.N1),0)-s.N1,8000))
    FROM cteStart s;
GO
/****** Object:  UserDefinedFunction [dbo].[SplitStrings_Moden]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[SplitStrings_Moden]
(
   @List NVARCHAR(MAX),
   @Delimiter NVARCHAR(255)
)
RETURNS TABLE
WITH SCHEMABINDING AS
RETURN
  WITH E1(N)        AS ( SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 
                         UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1 
                         UNION ALL SELECT 1 UNION ALL SELECT 1 UNION ALL SELECT 1),
       E2(N)        AS (SELECT 1 FROM E1 a, E1 b),
       E4(N)        AS (SELECT 1 FROM E2 a, E2 b),
       E42(N)       AS (SELECT 1 FROM E4 a, E2 b),
       cteTally(N)  AS (SELECT 0 UNION ALL SELECT TOP (DATALENGTH(ISNULL(@List,1))) 
                         ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) FROM E42),
       cteStart(N1) AS (SELECT t.N+1 FROM cteTally t
                         WHERE (SUBSTRING(@List,t.N,1) = @Delimiter OR t.N = 0))
  SELECT Item = SUBSTRING(@List, s.N1, ISNULL(NULLIF(CHARINDEX(@Delimiter,@List,s.N1),0)-s.N1,8000))
    FROM cteStart s;
GO
ALTER TABLE [dbo].[PatientIdentifierResourceLink]  WITH CHECK ADD  CONSTRAINT [FK_PatientIdentifierResourceLink_PatientIdentifier] FOREIGN KEY([PatientIdentifierId])
REFERENCES [dbo].[PatientIdentifier] ([Id])
GO
ALTER TABLE [dbo].[PatientIdentifierResourceLink] CHECK CONSTRAINT [FK_PatientIdentifierResourceLink_PatientIdentifier]
GO
ALTER TABLE [dbo].[PatientIdentifierResourceLink]  WITH CHECK ADD  CONSTRAINT [FK_PatientIdentifierResourceLink_Resource] FOREIGN KEY([ResourceId])
REFERENCES [dbo].[Resource] ([Id])
GO
ALTER TABLE [dbo].[PatientIdentifierResourceLink] CHECK CONSTRAINT [FK_PatientIdentifierResourceLink_Resource]
GO
ALTER TABLE [dbo].[Resource]  WITH CHECK ADD  CONSTRAINT [FK_Resource_ResourceType] FOREIGN KEY([TypeId])
REFERENCES [dbo].[ResourceType] ([Id])
GO
ALTER TABLE [dbo].[Resource] CHECK CONSTRAINT [FK_Resource_ResourceType]
GO
ALTER TABLE [dbo].[ResourceSearchIndex]  WITH CHECK ADD  CONSTRAINT [FK_ResourceSearchIndex_Resource] FOREIGN KEY([ResourceId])
REFERENCES [dbo].[Resource] ([Id])
GO
ALTER TABLE [dbo].[ResourceSearchIndex] CHECK CONSTRAINT [FK_ResourceSearchIndex_Resource]
GO
ALTER TABLE [dbo].[ResourceVersion]  WITH CHECK ADD  CONSTRAINT [FK_ResourceVersion_Resource] FOREIGN KEY([ResourceId])
REFERENCES [dbo].[Resource] ([Id])
GO
ALTER TABLE [dbo].[ResourceVersion] CHECK CONSTRAINT [FK_ResourceVersion_Resource]
GO
/****** Object:  StoredProcedure [dbo].[AddToSearchIndex]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[AddToSearchIndex]
	-- Add the parameters for the stored procedure here
	@ResourceId INT,
	@Field VARCHAR(50),
	@Value VARCHAR(100)
AS
BEGIN

	INSERT INTO [dbo].[ResourceSearchIndex]
			   ([ResourceId]
			   ,[Field]
			   ,[Value])
		 VALUES
			   (@ResourceId
			   ,@Field
			   ,@Value)
END
GO
/****** Object:  StoredProcedure [dbo].[ClearSearchIndex]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[ClearSearchIndex]
	-- Add the parameters for the stored procedure here
	@ResourceId INT
AS
BEGIN

	DELETE FROM [ResourceSearchIndex] WHERE ResourceId = @ResourceId

END
GO
/****** Object:  StoredProcedure [dbo].[CreateFhirResourceId]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Mark Frayne
-- Create date: 07/11/17
-- Description:	Inserts new record to the Resource table to create a new resource id.
--				The resource version is created via a separate stored procedure call.
-- =============================================
CREATE PROCEDURE [dbo].[CreateFhirResourceId]
	-- Add the parameters for the stored procedure here
	@ResourceType VARCHAR(50),
	@ResourceId INT OUT
AS
BEGIN
	--DECLARE @OrderItemID AS INT

	INSERT INTO Resource (TypeId) VALUES ((SELECT Id FROM ResourceType WHERE [Type] = @ResourceType))
	SET @ResourceId = SCOPE_IDENTITY()
	
	--INSERT INTO ResourceVersion
	--		   (ResourceId
	--		   ,[Version]
	--		   ,DateTimeCreated
	--		   ,ResourceXml)
	--	 VALUES
	--		   (@ResourceId
	--		   ,1
	--		   ,CURRENT_TIMESTAMP
	--		   ,@ResourceXml)	
	
END


GO
/****** Object:  StoredProcedure [dbo].[CreateFhirResourceVersion]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- =============================================
-- Author:		Mark Frayne
-- Create date: 07/11/17
-- Description:	Inserts new FHIR resource version
-- =============================================
CREATE PROCEDURE [dbo].[CreateFhirResourceVersion]
	-- Add the parameters for the stored procedure here
	@ResourceId INT,
	@Version INT,
	@DateTimeCreated DATETIME,
	@ResourceXml XML
AS
BEGIN
	
	INSERT INTO ResourceVersion
			   (ResourceId
			   ,[Version]
			   ,DateTimeCreated
			   ,ResourceXml)
		 VALUES
			   (@ResourceId
			   ,@Version
			   ,@DateTimeCreated
			   ,@ResourceXml)		
END

GO
/****** Object:  StoredProcedure [dbo].[CreatePatientIdentifierResourceLink]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Mark Frayne
-- Create date: 10/11/17
-- Description:	Creates a link between a patient identifier and the resource. Inserts patient identifier if it does not yet exist
-- =============================================
CREATE PROCEDURE [dbo].[CreatePatientIdentifierResourceLink]	
	@IdentifierValue VARCHAR(50),
	@IdentifierSystem VARCHAR(50),
	@ResourceId INT 
AS
BEGIN
	DECLARE @PatientIdentifierId AS INT
	IF NOT EXISTS(SELECT TOP 1 1 FROM PatientIdentifier WHERE Value = @IdentifierValue AND [System] = @IdentifierSystem)
	BEGIN
		INSERT INTO PatientIdentifier
			   (Value
			   ,[System]
			   ,DateTimeUpdated)
		 VALUES
			   (@IdentifierValue
			   ,@IdentifierSystem
			   ,CURRENT_TIMESTAMP)	
		SET @PatientIdentifierId = SCOPE_IDENTITY()	
	END
	ELSE
	BEGIN
	  SELECT @PatientIdentifierId = Id 
	    FROM PatientIdentifier 
		WHERE Value = @IdentifierValue AND [System] = @IdentifierSystem
	END

	-- Insert links between patient identifiers and resource
	INSERT INTO PatientIdentifierResourceLink
			   (PatientIdentifierId
			   ,ResourceId)
		 VALUES
			   (@PatientIdentifierId
			   ,@ResourceId)	


END

GO
/****** Object:  StoredProcedure [dbo].[GetFhirResourceById]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GetFhirResourceById] 
	@ResourceId INT
AS
BEGIN
	DECLARE @ResourceVersion INT
	
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT @ResourceVersion = MAX(Version) FROM ResourceVersion 
	WHERE ResourceId = @ResourceId

    
	SELECT ResourceXml FROM ResourceVersion
	WHERE ResourceId = @ResourceId AND [Version] = @ResourceVersion
END
GO
/****** Object:  StoredProcedure [dbo].[GetFhirResourceCurrentVersion]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Mark Frayne
-- Create date: 23/11/17
-- Description:	Gets the current version of a FHIR resource.
-- =============================================
CREATE PROCEDURE [dbo].[GetFhirResourceCurrentVersion]
	-- Add the parameters for the stored procedure here
	@ResourceId INT,
	@ResourceVersion INT OUT
AS
BEGIN

	SELECT @ResourceVersion = MAX(Version) FROM ResourceVersion 
	WHERE ResourceId = @ResourceId
	
END


GO
/****** Object:  StoredProcedure [dbo].[SearchResource]    Script Date: 29/12/2017 11:26:43 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Gareth Williams
-- Create date: 2017-11-24
-- Description:	Search for resources where each individual resource contains 
--				ALL search parameters
--
/*
    This is and AND query ie. the resource must contain all parameters

    The only exception is the patient.identifiers ie. the resource must Contain at least one or many
    
    Observation?patient.identifier=urn:oid:2.16.840.1.113883.2.1.8.1.3.108|A8098666,urn:oid:2.16.840.1.113883.2.1.8.1.3.140|X123456&category=vital-signs&code=75367002

    TODO:

    The statement to query ResourceSearchIndex needs to take into account whether or not a System value is applied e.g
	   
	   code=http://snomed.info/sct|75367002 versus code=75367002

    I would consider normalising out the system from the Value columb in the table
    So we would end up with 3 columns in ResourceSearchIndex
    Parameter, System, Value
    This would allow us to query and index more efficiently

*/

-- =============================================
CREATE PROCEDURE [dbo].[SearchResource] @QueryString VARCHAR(MAX)
AS
BEGIN

    SET NOCOUNT ON;
	-- @SearchParameters is a temp var table to hold our shredded query string 
	-- @PatientIdentities holds the patient.identifiers shredded out from query string
    DECLARE @ResourceName VARCHAR(200)= '';
    DECLARE @SearchParameters TABLE (id INT IDENTITY(1, 1), parameter VARCHAR(500), value VARCHAR(500));
    DECLARE @PatientIdentities TABLE (id INT IDENTITY(1, 1), [system] VARCHAR(500), identifier VARCHAR(100));
             
    INSERT INTO searchlog VALUES(@QueryString); -- for debug only

    IF(@QueryString = '') 
	   BEGIN 
    --  set @QueryString = 'Observation?patient.identifier=urn:oid:2.16.840.1.113883.2.1.8.1.3.108|A8098666,urn:oid:2.16.840.1.113883.2.1.8.1.3.140|X123456&category=vital-signs&code=75367002'
	--	set @QueryString = 'Observation?patient.identifier=urn:oid:2.16.840.1.113883.2.1.8.1.3.108|A8098666,urn:oid:2.16.840.1.113883.2.1.8.1.3.140|X123456&category=vital-signs&code=75367002'
                     SET @querystring = 'AllergyIntolerance?code=http://snomed.info/sct|397192001&category=medication';
        END;
		
	/*** 1. Process FHIR Query String ***/

    -- 1.1 FHIR ResourceName (i.e. everything left of ?)
        SELECT @ResourceName = SUBSTRING(@querystring, 1, CHARINDEX('?', @QueryString)-1);
             IF(@ResourceName = '')
                 BEGIN
                     ;THROW 60000, 'FHIR ResourceName must be provided e.g. AllergyIntolerance?patient.identi....', 1;
                 END;
	--1.2 Update @QueryString to only include everything that is right of the '?'
        SELECT @QueryString = SUBSTRING(@queryString, CHARINDEX('?', @QueryString)+1, LEN(@QueryString)); 
	
	--1.3 Split by appersand to get each search parameter
        DELETE FROM @SearchParameters;
             INSERT INTO @SearchParameters
                    SELECT item [each item is a search parameter pair],
                           '' emptyvaluefornow
                    FROM dbo.fnSplitStringsIntoTable(@QueryString, N'&');

	/* 2. Split out patient identifers in to it's own temporary table @PatientIdentifiers */
	-- this will leave all other parameters in the @SearchParameters table
	-- if there is no patient.identifier parameter we will throw and error!
     DECLARE @TmpPIDString NVARCHAR(MAX) = (  SELECT TOP 1 parameter    FROM @SearchParameters    WHERE parameter LIKE 'patient.identifier=%');
	
	IF (@tmpPIDString is not null) -- if there's no patient.identifier then we throw an error
	   BEGIN
			-- Get Everything to the Right of the '='
               SET @TmpPIDString = SUBSTRING(@TmpPIDString, CHARINDEX('=', @tmpPIDString)+1, LEN(@TmpPIDString)); 

			-- each ID key-value pairs are seperated by ',' 
			-- so lets put them in to a temp table for further shredding...				
               DECLARE @tmppids TABLE (id          INT IDENTITY(1, 1), identifiers VARCHAR(500) );
               
			INSERT INTO @tmppids
                     SELECT item FROM dbo.fnSplitStringsIntoTable(@TmpPIDString, N',');
		
			-- Finally, we need to split on the '|' and insert into the final @PatientIdentities table
               INSERT INTO @PatientIdentities
                     SELECT SUBSTRING(T.IDENTIFIERS, 1, CHARINDEX('|', T.IDENTIFIERS)-1),
                            SUBSTRING(T.IDENTIFIERS, CHARINDEX('|', T.IDENTIFIERS)+1, LEN(T.IDENTIFIERS))
                            FROM @TMPPIDS T;

			-- Because we now have search PIDs in its own table we can remove patient.identifier 
			-- from the @SearchParameters table, this is to make processing easier later...
               DELETE FROM @SearchParameters WHERE parameter LIKE '%patient.identifier%';
        END;
                 ELSE
        BEGIN
            ;THROW 60000, 'FHIR parameter patient.identifier MUST be supplied in the @QueryString.', 1;
        END;

	/* Do PatientIdentifier search first and stored in temp table */

     -- Get resourceIds from PatientIdentifierReousrceLink
        CREATE TABLE #resourceIds
			 (Resourceid INT PRIMARY KEY );
        
	   INSERT INTO #resourceIds
                    SELECT DISTINCT
                           ResourceId
                    FROM PatientIdentifier p
                         JOIN PatientIdentifierResourceLink pirl ON pirl.PatientIdentifierId = p.Id
                         JOIN @patientIdentities s ON p.Value = s.identifier
                                                      AND p.System = s.system
                    ORDER BY resourceid ASC;
	
	/* Search the Resources for given parameters */

-- simply split the parameters that are left over
        UPDATE @SearchParameters
               SET
                   parameter = SUBSTRING(parameter, 1, CHARINDEX('=', parameter)-1),
                   value = SUBSTRING(parameter, CHARINDEX('=', parameter)+1, LEN(parameter));
     	
	   CREATE TABLE #results
			([ResourceId]  [INT] NOT NULL,
			 [Field]       [VARCHAR](50) NOT NULL,
			 [Value]       [VARCHAR](100) NOT NULL,
			 [parameterid] INT ); 
             
	   INSERT INTO #results
                    SELECT DISTINCT
                           rsi.ResourceId,
                           rsi.field,
                           rsi.value,
                           0
                    FROM dbo.ResourceSearchIndex rsi
                         JOIN dbo.[Resource] r ON rsi.ResourceId = r.Id
                         JOIN dbo.ResourceType t ON t.Id = r.TypeId
                                                    AND t.Type = @ResourceName
                         JOIN @SearchParameters p ON p.parameter = rsi.Field
                                                     AND rsi.value LIKE '%'+p.value
                         JOIN ResourceVersion rv ON rv.ResourceId = rsi.ResourceId
                                                    AND rv.version =
						  (
							 SELECT MAX(rv3.version)
								FROM resourceversion rv3
								    WHERE rv3.ResourceId = rv.ResourceId)
								         AND rsi.ResourceId IN 
									    (SELECT patient.Resourceid FROM #ResourceIds patient); -- filter identifier

             SELECT ResourceXml
             FROM ResourceVersion rv
             WHERE rv.ResourceId IN
			 (SELECT resourceid
				FROM #results
				GROUP BY resourceid
				HAVING COUNT(resourceid) =
			 (
			 SELECT COUNT(*)   FROM @SearchParameters )) -- only select the resource that has the correct number of @searchParameters
                   AND rv.version = (
			 SELECT MAX(rv3.version)
				FROM resourceversion rv3
				WHERE rv3.ResourceId = rv.ResourceId
				);
				
	

             DROP TABLE #resourceids;
             DROP TABLE #results;
END
GO


-- Insert supported resource types...
USE [iCDR]
GO
INSERT [dbo].[ResourceType] ([Id], [Type]) VALUES (1, N'AllergyIntolerance')
GO
INSERT [dbo].[ResourceType] ([Id], [Type]) VALUES (2, N'Observation')
GO
INSERT [dbo].[ResourceType] ([Id], [Type]) VALUES (3, N'Condition')
GO
INSERT [dbo].[ResourceType] ([Id], [Type]) VALUES (4, N'Procedure')
GO
