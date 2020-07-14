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

