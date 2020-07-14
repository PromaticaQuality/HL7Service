ALTER TABLE dbo.PAS ADD
	PatientAddress varchar(1000) NULL,
	PatientCity varchar(100) NULL,
	PatientRegion varchar(100) NULL,
	PatientPostcode varchar(10) NULL,
	PatientGender varchar(10) NULL,
	PatientTitle varchar(10) NULL,
	GPName varchar(500) NULL,
	GPCode varchar(50) NULL,
	GPAddress varchar(1000) NULL,
	GPPostcode varchar(10) NULL,
	GPDoctorName varchar(500) NULL,
	GPDoctorCode varchar(50) NULL
GO

ALTER TABLE dbo.TempHL7PAS ADD
	PatientAddress varchar(1000) NULL,
	PatientCity varchar(100) NULL,
	PatientRegion varchar(100) NULL,
	PatientPostcode varchar(10) NULL,
	PatientGender varchar(10) NULL,
	PatientTitle varchar(10) NULL,
	GPName varchar(500) NULL,
	GPCode varchar(50) NULL,
	GPAddress varchar(1000) NULL,
	GPPostcode varchar(10) NULL,
	GPDoctorName varchar(500) NULL,
	GPDoctorCode varchar(50) NULL
GO
ALTER TABLE dbo.TempCSVPAS ADD
	PatientAddress varchar(1000) NULL,
	PatientCity varchar(100) NULL,
	PatientRegion varchar(100) NULL,
	PatientPostcode varchar(10) NULL,
	PatientGender varchar(10) NULL,
	PatientTitle varchar(10) NULL,
	GPName varchar(500) NULL,
	GPCode varchar(50) NULL,
	GPAddress varchar(1000) NULL,
	GPPostcode varchar(10) NULL,
	GPDoctorName varchar(500) NULL,
	GPDoctorCode varchar(50) NULL
GO

  
CREATE Procedure [dbo].[PAS_ProcessPatientImportsInfo]

(@PAS_ID varchar(20)='', @SNAME varchar(50)='', @FName varchar(50)='', @DOB varchar(30)='', @PAS_NHS_NO varchar(20)='',
 @RID varchar(20)='',@MessageType varchar(20)='', @ProcessType varchar(20)='', @IsMerged bit=0, @PreviousPASID varchar(20)='', @PreviousFirstname varchar(20)='', @PreviousLastname varchar(20)='', @PatientAddress VARCHAR(500)='', @PatientCity VARCHAR(500)=''
           ,@PatientRegion VARCHAR(500)=''
           ,@PatientPostcode VARCHAR(50)=''
           ,@PatientGender VARCHAR(50)=''
           ,@PatientTitle VARCHAR(50)=''
           ,@GPName VARCHAR(500)=''
           ,@GPCode VARCHAR(50)='' 
           ,@GPAddress VARCHAR(500)=''
           ,@GPPostcode VARCHAR(50)=''
           ,@GPDoctorName VARCHAR(500)=''
           ,@GPDoctorCode VARCHAR(50)='') 
 AS

IF(@IsMerged=1)
BEGIN
	IF NOT EXISTS(SELECT * FROM DBO.PAS WHERE PAS_ID=@PAS_ID)
	BEGIN
	INSERT dbo.PAS (PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID, HasParent,[PatientAddress]
           ,[PatientCity]
           ,[PatientRegion]
           ,[PatientPostcode]
           ,[PatientGender]
           ,[PatientTitle]
           ,[GPName]
           ,[GPCode]
           ,[GPAddress]
           ,[GPPostcode]
           ,[GPDoctorName]
           ,[GPDoctorCode]) 
		   VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO,@RID, @IsMerged 
		   ,@PatientAddress
           ,@PatientCity
           ,@PatientRegion
           ,@PatientPostcode
           ,@PatientGender
           ,@PatientTitle
           ,@GPName
           ,@GPCode
           ,@GPAddress
           ,@GPPostcode
           ,@GPDoctorName
           ,@GPDoctorCode); 
	END
	ELSE
	BEGIN
	UPDATE dbo.PAS SET HasParent=1 WHERE PAS_ID = @PAS_ID
	END

	INSERT dbo.TempHL7PAS(PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID, CreatedDate,ProcessType, IsMerged, MergedDate, PreviousPASID, PreviousFirstName, PreviousLastName,[PatientAddress]
           ,[PatientCity]
           ,[PatientRegion]
           ,[PatientPostcode]
           ,[PatientGender]
           ,[PatientTitle]
           ,[GPName]
           ,[GPCode]
           ,[GPAddress]
           ,[GPPostcode]
           ,[GPDoctorName]
           ,[GPDoctorCode]) 
			VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO, @RID, GETDATE(), @ProcessType, @IsMerged, GETDATE(),@PreviousPASID, @PreviousFirstname, @PreviousLastname
		   ,@PatientAddress
           ,@PatientCity
           ,@PatientRegion
           ,@PatientPostcode
           ,@PatientGender
           ,@PatientTitle
           ,@GPName
           ,@GPCode
           ,@GPAddress
           ,@GPPostcode
           ,@GPDoctorName
           ,@GPDoctorCode);  
	UPDATE dbo.PAS SET IsMerged =@IsMerged, MergedDate = GETDATE(), NewPasID=@PAS_ID WHERE PAS_ID = [dbo].fnGetPatientLatestPasId(@PreviousPASID)
END
ELSE IF NOT EXISTS (SELECT * FROM PAS WHERE Pas_ID = @PAS_ID) 
	BEGIN 
			INSERT dbo.PAS (PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID,[PatientAddress]
           ,[PatientCity]
           ,[PatientRegion]
           ,[PatientPostcode]
           ,[PatientGender]
           ,[PatientTitle]
           ,[GPName]
           ,[GPCode]
           ,[GPAddress]
           ,[GPPostcode]
           ,[GPDoctorName]
           ,[GPDoctorCode]) 
		   VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO,@RID
		   ,@PatientAddress
           ,@PatientCity
           ,@PatientRegion
           ,@PatientPostcode
           ,@PatientGender
           ,@PatientTitle
           ,@GPName
           ,@GPCode
           ,@GPAddress
           ,@GPPostcode
           ,@GPDoctorName
           ,@GPDoctorCode); 
			
			INSERT dbo.TempHL7PAS(PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID, CreatedDate,ProcessType,[PatientAddress]
           ,[PatientCity]
           ,[PatientRegion]
           ,[PatientPostcode]
           ,[PatientGender]
           ,[PatientTitle]
           ,[GPName]
           ,[GPCode]
           ,[GPAddress]
           ,[GPPostcode]
           ,[GPDoctorName]
           ,[GPDoctorCode]) 
			VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO, @RID, getdate(), @ProcessType
		   ,@PatientAddress
           ,@PatientCity
           ,@PatientRegion
           ,@PatientPostcode
           ,@PatientGender
           ,@PatientTitle
           ,@GPName
           ,@GPCode
           ,@GPAddress
           ,@GPPostcode
           ,@GPDoctorName
           ,@GPDoctorCode); 
	END 
ELSE 
	BEGIN 
		UPDATE dbo.PAS SET Sname = @SNAME, Fname = @FName, Dob = @DOB, Pas_Nhs_no = @PAS_NHS_NO, RID=@RID,
		[PatientAddress]=@PatientAddress
           ,[PatientCity]=@PatientCity
           ,[PatientRegion]=@PatientRegion
           ,[PatientPostcode]=@PatientPostcode
           ,[PatientGender]=@PatientGender
           ,[PatientTitle]=@PatientTitle
           ,[GPName]=@GPName
           ,[GPCode]=@GPCode
           ,[GPAddress]=@GPAddress
           ,[GPPostcode]=@GPPostcode
           ,[GPDoctorName]=@GPDoctorName
           ,[GPDoctorCode]=@GPDoctorCode
		
		 WHERE Pas_ID = @PAS_ID;


				INSERT dbo.TempHL7PAS(PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID, CreatedDate,ProcessType,
				[PatientAddress]
           ,[PatientCity]
           ,[PatientRegion]
           ,[PatientPostcode]
           ,[PatientGender]
           ,[PatientTitle]
           ,[GPName]
           ,[GPCode]
           ,[GPAddress]
           ,[GPPostcode]
           ,[GPDoctorName]
           ,[GPDoctorCode]) 
			VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO, @RID, getdate(), @ProcessType
		   ,@PatientAddress
           ,@PatientCity
           ,@PatientRegion
           ,@PatientPostcode
           ,@PatientGender
           ,@PatientTitle
           ,@GPName
           ,@GPCode
           ,@GPAddress
           ,@GPPostcode
           ,@GPDoctorName
           ,@GPDoctorCode); 
	END
