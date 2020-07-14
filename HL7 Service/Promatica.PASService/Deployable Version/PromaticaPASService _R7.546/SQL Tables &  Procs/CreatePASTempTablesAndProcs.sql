 
CREATE TABLE [dbo].[TempCSVPAS](
	[TempCSVPASID] [int] IDENTITY(1,1) NOT NULL,
	[PAS_ID] [varchar](50) NOT NULL,
	[SNAME] [varchar](100) NOT NULL,
	[FName] [varchar](100) NOT NULL,
	[DOB] [varchar](100) NULL,
	[PAS_NHS_NO] [varchar](30) NULL,
	[RID] [varchar](50) NULL,
	[ForPrinting] [bit] NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ProcessType] [varchar](50) NULL,
 CONSTRAINT [PK_TempCSVPAS] PRIMARY KEY CLUSTERED 
(
	[TempCSVPASID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[TempCSVPAS] ADD  CONSTRAINT [DF_TempCSVPAS_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
GO
 

CREATE TABLE [dbo].[TempHL7PAS](
	[TempHL7PASID] [int] IDENTITY(1,1) NOT NULL,
	[PAS_ID] [varchar](50) NOT NULL,
	[SNAME] [varchar](100) NOT NULL,
	[FName] [varchar](100) NOT NULL,
	[DOB] [varchar](100) NULL,
	[PAS_NHS_NO] [varchar](30) NULL,
	[RID] [varchar](50) NULL,
	[ForPrinting] [bit] NULL,
	[CreatedDate] [datetime] NOT NULL,
	[ProcessType] [varchar](50) NULL,
 CONSTRAINT [PK_TempHL7PAS] PRIMARY KEY CLUSTERED 
(
	[TempHL7PASID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[TempHL7PAS] ADD  CONSTRAINT [DF_TempHL7PAS_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
GO
 
CREATE Procedure [dbo].[PAS_ProcessPatientImports]

(@PAS_ID varchar(20)='', @SNAME varchar(50)='', @FName varchar(50)='', @DOB varchar(30)='', @PAS_NHS_NO varchar(20)='', @RID varchar(20)='',@MessageType varchar(20)='')

AS
 
IF NOT EXISTS (SELECT * FROM PAS WHERE Pas_ID = @PAS_ID) 
	BEGIN 
	IF(@MessageType ='CSV')
		BEGIN
			INSERT PAS (PAS_ID, SNAME, FName, DOB, PAS_NHS_NO) VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO); 
			INSERT dbo.TempCSVPAS(PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID, CreatedDate,ProcessType) 
			VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO, @RID, getdate(),'A28'); 
		END
	ELSE
		BEGIN
			INSERT PAS (PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID) VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO,@RID); 
			INSERT dbo.TempHL7PAS(PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID, CreatedDate,ProcessType) 
			VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO, @RID, getdate(),'A28'); 
		END
	END 
ELSE 
	BEGIN 
		IF(@MessageType ='CSV')
			BEGIN
				UPDATE PAS SET Sname = @SNAME, Fname = @FName, Dob = @DOB, Pas_Nhs_no = @PAS_NHS_NO, RID=@RID WHERE Pas_ID = @PAS_ID;
				INSERT dbo.TempCSVPAS(PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID, CreatedDate,ProcessType) 
				VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO, @RID, getdate(),'A31'); 
			END
		ELSE
			BEGIN
				UPDATE PAS SET Sname = @SNAME, Fname = @FName, Dob = @DOB, Pas_Nhs_no = @PAS_NHS_NO, RID=@RID WHERE Pas_ID = @PAS_ID;
				INSERT dbo.TempHL7PAS(PAS_ID, SNAME, FName, DOB, PAS_NHS_NO,RID, CreatedDate,ProcessType) 
				VALUES (@PAS_ID, @SNAME, @FName, @DOB, @PAS_NHS_NO, @RID, getdate(),'A31'); 
	END
END
GO

