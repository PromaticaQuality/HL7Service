using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;

//using HL7.Library;

namespace HL7.Library
{
    public class ImportPas
    {
        public ImportPas()
        {

        }

        private string _CurrentFile;
        
        private int _RecordCount = 0;

        public void ExecuteCSVImport()
        {
            string text = null;
            char delimeter = ConfigSettings.CsvDelimeter;
            try
            {
                foreach (string file in Directory.GetFiles(ConfigSettings.CsvImportPath, "*." + ConfigSettings.CsvFileExtension))
                {
                    _CurrentFile = file;
                    using (TextReader streamReader = new StreamReader(file))
                    {
                        //the first line contains headers
                        string[] fields;
                        while ((text = streamReader.ReadLine()) != null)
                        {
                            fields = new string[ConfigSettings.CsvFieldsCount];

                            var currentfields = text.Split(delimeter);
                            for (int i = 0; i < currentfields.Length; i++)
                            {
                                fields[i] = currentfields[i];
                            }

                            ProcessFields(fields);
                        }
                    }
                    FinishFile();
                }
            }
            catch (Exception ex)
            {
                ConfigSettings.WriteErrorLog(ex, _CurrentFile);
            }
        }

        private void FinishFile()
        {
            FileInfo fi = new FileInfo(_CurrentFile);
            string destinationFile = ConfigSettings.CsvProcessedPath + "\\" + Path.GetFileNameWithoutExtension(fi.Name) + "_" + DateTime.Now.ToString("ddMMyyyy_HHmm") + new FileInfo(fi.Name).Extension;
            File.Move(_CurrentFile, destinationFile);
            ConfigSettings.WriteInformationLog(string.Format("Successfully processed file '{0}'. Total records : {1}", fi.Name, _RecordCount.ToString()), ConfigSettings.Hl7ErrorLogPath);
            _RecordCount = 0;
        }

        private void ProcessFields(string[] fields)
        {
            //Make sure the PASID is not null
            if (fields[0] == null || fields[0] == "")
            {
                ConfigSettings.WriteExceptionLine(fields, "Invalid PAS ID", _CurrentFile);
                return;
            }
            try
            {
                AddRecordToDatabase(fields);
            }
            catch (Exception ex)
            {
                ConfigSettings.WriteExceptionLine(fields, ex.Message, _CurrentFile);
            }
        }

        private void AddRecordToDatabase(string[] paramsArray)
        {
            //Escape the ' for SQL
            for (int i = 0; i < paramsArray.Length; i++)
            {
                if (string.IsNullOrEmpty(paramsArray[i]) && i==8)
                {
                    var addressString = paramsArray[i - 1];

                    if (string.IsNullOrEmpty(addressString)) continue;

                    var addressParts = addressString.Split(',');
                    paramsArray[i - 1] = addressParts[0]+ (string.IsNullOrEmpty(addressParts[1])?"":", "+ addressParts[1]);

                    paramsArray[i] = addressParts[2];

                    if (addressParts.Length > 2)
                    {
                        paramsArray[i + 1] = addressParts[3];
                    }

                    if (addressParts.Length > 3)
                    {
                        paramsArray[i + 2] = addressParts[4];
                    }

                    //if (addressParts.Length ==4)
                    //{
                    //    paramsArray[i] = addressParts[1];
                    //    paramsArray[i + 1] = addressParts[2];
                    //    paramsArray[i + 2] = addressParts[3];
                    //}
                    //else
                    //{
                    //    paramsArray[i] = string.Empty;
                    //    paramsArray[i + 1] = addressParts[1];
                    //    paramsArray[i + 2] = addressParts[2];
                    //}
                    break;
                }

                paramsArray[i] = paramsArray[i].Replace("'", "''").Replace(ConfigSettings.CsvQualifier, "").Trim();

                if (i == ConfigSettings.CsvDateFieldIndex && paramsArray[i] != null)
                {
                    paramsArray[i] = FormatDate(paramsArray[i]);
                }
            }
            foreach (var connectionString in ConfigSettings.ConnectionStrings)
            {
                var conn = new SqlConnection(connectionString);

                var spName = ConfigSettings.FullPasEnabled ? "dbo.PAS_ProcessPatientImportsInfo" : "dbo.PAS_ProcessPatientImports";

                var cmd = new SqlCommand(spName, conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                var insertColumns = ConfigSettings.CsvInsertColumnsOrder.Split(',');

                cmd.Parameters.AddWithValue("MessageType", "CSV");
                for (int i = 0; i < insertColumns.Length; i++)
                {
                    cmd.Parameters.AddWithValue(insertColumns[i].Trim(), (paramsArray[i]??"").Trim());
                }

                conn.Open();
                cmd.ExecuteNonQuery();
                conn.Close();
            }
            _RecordCount++;

        }

        private string FormatDate(string dob)
        {
            return DateTime.ParseExact(dob, ConfigSettings.CsvDateSourceFormat, new NumberFormatInfo()).ToString(ConfigSettings.PasDateTargetFormat);
        }
    }



}
