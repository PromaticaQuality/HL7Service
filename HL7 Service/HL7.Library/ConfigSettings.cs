using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlClient;

namespace HL7.Library
{
    public class ConfigSettings
    {
        public static bool MRGHasFirstSegmentAsPasID
        {
            get
            {
                return !String.IsNullOrEmpty(ConfigurationManager.AppSettings["MRGHasFirstSegmentAsPasID"]) && ConfigurationManager.AppSettings["MRGHasFirstSegmentAsPasID"].Equals("True");
            }
        }


        public static string PasImportType { get { return ConfigurationManager.AppSettings["PASImportType"]; } }
        public static char CsvDelimeter { get { return ConfigurationManager.AppSettings["CSVDelimiter"].ToCharArray()[0]; } }
        public static string CsvImportPath { get { return ConfigurationManager.AppSettings["CSVImportPath"]; } }
        public static string CsvProcessedPath { get { return ConfigurationManager.AppSettings["CSVProcessedPath"]; } }
        public static string CsvFileExtension { get { return ConfigurationManager.AppSettings["CSVFileExtension"]; } }
        public static string CsvQualifier { get { return ConfigurationManager.AppSettings["CSVQualifier"]; } }
        public static int CsvFieldsCount { get { return int.Parse(ConfigurationManager.AppSettings["CSVFieldsCount"]); } }
        public static string CsvInsertColumnsOrder { get { return ConfigurationManager.AppSettings["CSVInsertColumnsOrder"]; } }
        public static string CsvImportIntervalMins { get { return ConfigurationManager.AppSettings["CSVImportIntervalMins"]; } }
        public static int CsvDateFieldIndex { get { return int.Parse(ConfigurationManager.AppSettings["CSVDateFieldIndex"]); } }
        public static string CsvDateSourceFormat { get { return ConfigurationManager.AppSettings["CSVDateSourceFormat"].ToString(); } }

        public static string Hl7InsertColumnsOrder { get { return ConfigurationManager.AppSettings["HL7InsertColumnsOrder"]; } }
        public static string Hl7TcpPort { get { return ConfigurationManager.AppSettings["HL7TcpPort"]; } }
        public static string Hl7Logging { get { return ConfigurationManager.AppSettings["HL7Logging"]; } }
        public static string Hl7ErrorLogPath { get { return ConfigurationManager.AppSettings["HL7ErrorLogPath"] + "\\logs_" + DateTime.Now.ToString("ddMMyyyy") + ".log"; } }

        public static int Hl7IdentifiersIndex { get { return ConfigurationManager.AppSettings["HL7Segment_INDEX"] == null ? 4 : int.Parse(ConfigurationManager.AppSettings["HL7Segment_INDEX"]); } }
        public static string Hl7AckStartChars { get { return ConfigurationManager.AppSettings["HL7ACK_StartCharacter"]; } }
        public static string Hl7AckPipes { get { return ConfigurationManager.AppSettings["HL7ACK_FinishPipes"]; } }

        public static int BakIntervalHours { get { return int.Parse(ConfigurationManager.AppSettings["BakIntervalHours"] ?? "12"); } }
        public static string BakSourcePath { get { return ConfigurationManager.AppSettings["BakSourcePath"]; } }
        public static string BakDestinationPath { get { return ConfigurationManager.AppSettings["BakDestinationPath"]; } }
        public static string BakDestinationFilePrefix { get { return ConfigurationManager.AppSettings["BakDestinationFilePrefix"]; } }

        public static string Hl7AckSourceName {
            get
            {
                return ConfigurationManager.AppSettings["HL7ACK_SourceName"]??"WebTracker";
            } 
        }

        public static bool Hl7DoubleVerticalHeaderSegmentStart
        {
            get
            {
                return !String.IsNullOrEmpty(ConfigurationManager.AppSettings["Hl7DoubleVerticalHeaderSegmentStart"]) && ConfigurationManager.AppSettings["Hl7DoubleVerticalHeaderSegmentStart"].Equals("True");
            }
        }

        public static string Hl7BulkUploadDirectory { get { return ConfigurationManager.AppSettings["HL7BulkUploadPath"]; } }
        public static string Hl7PasidSegmentName { get { return ConfigurationManager.AppSettings["HL7Segment_PASID"] ?? "HOSP"; } }
        public static string Hl7NhsNoSegmentName { get { return ConfigurationManager.AppSettings["HL7Segment_NHSNO"] ?? "NN"; } }

        public static string InstanceName { get { return ConfigurationManager.AppSettings["HL7InstanceName"]; } }
        
        public static string[] ConnectionStrings
        {
            get
            {
             return ConfigurationManager.AppSettings.AllKeys.Where(p => p.Contains("ConnectionString")).Select(m => ConfigurationManager.AppSettings[m]).ToArray();
            }
        }

        public static bool FullPasEnabled {
            get { return ConfigurationManager.AppSettings["HL7FullPasEnabled"]!=null && ConfigurationManager.AppSettings["HL7FullPasEnabled"] == "True"; }
        }

        public static string PasDateSourceFormat { get { return ConfigurationManager.AppSettings["PASDateSourceFormat"]; } }
        public static string PasDateTargetFormat { get { return ConfigurationManager.AppSettings["PASDateTargetFormat"]; } }

        public static bool EnableErrorAcknowledgement { get { return ConfigurationManager.AppSettings["EnableErrorAcknowledgement"]!=null
            && ConfigurationManager.AppSettings["EnableErrorAcknowledgement"] == "true";
        }
        }
        
        public static void WriteErrorLog(Exception ex, string currentFile)
        {
            string logfile = currentFile.ToLower().Replace(CsvFileExtension.ToLower(), "log");
            if (!File.Exists(logfile))
            {
                FileStream stream = File.Create(logfile);
                stream.Close();

            }
            using (TextWriter streamWriter = new StreamWriter(logfile))
            {
                streamWriter.Write("Error occured reading main file: \r\n\r\n");
                streamWriter.Write(ex.ToString());
            }
        }
       
        public static void WriteInformationLog(string message, string currentFile)
        {
            var logfile = currentFile;
            if (!File.Exists(logfile))
            {
                var stream = File.Create(logfile);
                stream.Close();
            }

            using (var w = File.AppendText(logfile))
                {
                    Log(message, w);
                }
        }
        private static void Log(string logMessage, TextWriter w)
        {
            w.WriteLine("Log Entry : {0} {1} ", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            w.WriteLine(logMessage);
            w.WriteLine("-------------------------------");
        }
        
        public static void WriteExceptionLine(string[] fields, string reason, string currentFile)
        {
            var logfile = currentFile.ToLower().Replace(CsvFileExtension.ToLower(), "bad");
            if (!File.Exists(logfile))
            {
                var stream = File.Create(logfile);
                stream.Close();
            }
            using (TextWriter streamWriter = new StreamWriter(logfile, true))
            {
                foreach (var field in fields)
                {
                    streamWriter.Write(field + "|");
                }
                streamWriter.Write(reason + "\r\n");
            }
        }

        public static void RecordAuditsToTempDatabase(string tableName,  string[] paramsArray)
        {
            for (int i = 0; i < paramsArray.Length; i++)
            {
                paramsArray[i] = paramsArray[i].Replace("'", "''").Replace(CsvQualifier, "").Trim();
            }

            foreach (var connectionString in ConnectionStrings)
            {
                var conn = new SqlConnection(connectionString);

                var sql = String.Format("INSERT INTO dbo." + tableName + "(" + Hl7InsertColumnsOrder + ", CreatedDate) VALUES('{0}','{1}','{2}','{3}', '{4}','{5}','{6}')", paramsArray[0], paramsArray[1], paramsArray[2], paramsArray[3], paramsArray[4], paramsArray[5], DateTime.Now);

                var cmd = new SqlCommand(sql, conn);

                var insertColumns = CsvInsertColumnsOrder.Split(',');

                for (var i = 0; i < insertColumns.Length; i++)
                {
                    cmd.Parameters.AddWithValue("@" + insertColumns[i].Trim(), paramsArray[i]);
                }

                cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                conn.Open();

                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        public static void SetFolderAsCompleted(string directoryPath)
        {
            WriteInformationLog("Folder Processed at " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), directoryPath + "\\completed.log");
        }

    }

}
