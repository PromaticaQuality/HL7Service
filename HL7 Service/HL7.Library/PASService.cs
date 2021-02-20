using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HL7.Library
{
    public class PasService
    {
        Thread csvImportThread;
        readonly Thread hl7ImportThread = null;
        Thread bakImportThread;

        public void InitialisePasService()
        {
            try
            {
                if (ConfigSettings.PasImportType.Contains("HL7"))
                {
                    InitialiseHl7Import();
                }

                if (ConfigSettings.PasImportType.Contains("CSV"))
                {
                    csvImportThread = new Thread(InitialiseCsvImport);
                    csvImportThread.IsBackground = true;
                    csvImportThread.Start();
                }

                if (!ConfigSettings.PasImportType.Contains("BAK")) return;
                var timer = new Timer(CreateBakThread, null, TimeSpan.Zero, TimeSpan.FromHours(ConfigSettings.BakIntervalHours));
            }
            catch (Exception ex)
            {
                LogEvent("Error Occurred: " + ex.Message);
            }
        }

        private void CreateBakThread(object state)
        {
            bakImportThread = new Thread(InitialiseBakService);
            bakImportThread.Start();
        }

        private static void InitialiseBakService()
        {
            var sourcePath = ConfigSettings.BakSourcePath;
            var destinationPath = ConfigSettings.BakDestinationPath;
            var destinationFilePrefix = string.IsNullOrEmpty(ConfigSettings.BakDestinationFilePrefix) ? "Backups" : ConfigSettings.BakDestinationFilePrefix;
            var dateTime = DateTime.Now;

            if (!Directory.Exists(sourcePath) || !Directory.Exists(destinationPath)) return;

            var destinationFileName = Path.Combine(destinationPath, string.Format(destinationFilePrefix + "_" + dateTime.ToString("ddMMyyyy_HHmmss") + ".zip"));
            var zipperCommandFile = Environment.CurrentDirectory + @"\Apps\7za.exe";
            var arguments = "7za a \"" + destinationFileName + "\" -r \"" + sourcePath + "\"";
            Execute(zipperCommandFile, arguments);
        }

        public static void Execute(string processFileName, string fileargs)
        {
            var proc1 = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = processFileName,
                Arguments = fileargs,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(proc1);
        }

        #region "HL7 PAS Helpers"

        private TcpListener serverTcpListener;

        private Thread listeningThread;

        private TcpClient tcpClient;

        protected void InitialiseCsvImport()
        {
            while (true)
            {
                new ImportPas().ExecuteCSVImport();
                Thread.Sleep(int.Parse(ConfigSettings.CsvImportIntervalMins) * 60 * 1000); // sleep for next n milli seconds
            }
        }

        protected void InitialiseHl7Import()
        {

            ValidateDatabase();

            int port;
            if (int.TryParse(ConfigSettings.Hl7TcpPort, out port))
            {
                if (port > 0 && port <= 65535)
                {
                    try
                    {
                        serverTcpListener = new TcpListener(IPAddress.Any, port);
                        LogEvent("Waiting for connection");
                        this.listeningThread = new Thread(ListenForClients);
                        this.listeningThread.Start();
                    }
                    catch (Exception ex)
                    {
                        LogEvent("There was a problem opening the port.  Ensure the port is not in use or choose a different one." + ex.Message);
                    }
                }
                else
                {
                    LogEvent("The TCP port must be between 1 and 65535");
                }
            }
            else
            {
                LogEvent("The TCP port must be an integer");
            }
        }

        private void ListenForClients()
        {
            this.serverTcpListener.Start();

            while (true)
            {
                var acceptTcpClient = serverTcpListener.AcceptTcpClient();

                LogEvent("Connected.");

                var clientThread = new Thread(HandleClientComm);
                clientThread.Start(acceptTcpClient);
                Thread.Sleep(50);
            }
        }

        private void HandleClientComm(object client)
        {
            tcpClient = (TcpClient)client;

            var clientStream = tcpClient.GetStream();

            var message = new byte[4096];
            var encoding = new UTF8Encoding();

            while (true)
            {
                var completeMessage = new StringBuilder();
                var bytesRead = 0;
                try
                {
                    // Incoming message may be larger than the buffer size.
                    do
                    {
                        bytesRead = clientStream.Read(message, 0, 4096);
                        completeMessage.AppendFormat("{0}", Encoding.ASCII.GetString(message, 0, bytesRead));
                    }
                    while (clientStream.DataAvailable);
                }
                catch
                {
                    //a socket error has occured
                    LogEvent("Client Disconnected.");
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the serverTcpListener
                    LogEvent("The client has disconnected from the serverTcpListener");
                    break;
                }

                LogEvent("Message received...");

                if (completeMessage.Length <= 0) continue;

                var hl7Messages = completeMessage.ToString().Split(new string[] { "MSH|" }, StringSplitOptions.None).ToList();

                hl7Messages.RemoveAll(p => p.Trim().Length < 1);

                if (hl7Messages.Count() > 1)
                {
                    foreach (var msg in hl7Messages)
                    {
                        try
                        {
                            ProcessMessage("MSH|" + msg.Trim(), clientStream, true);
                        }
                        catch (Exception ex)
                        {
                            LogEvent("Error Occurred : " + ex.Message);
                            LogEvent("Error Data : " + msg);
                            continue;
                        }
                        Thread.Sleep(50);
                    }

                    LogEvent("Sending response...");

                    LogEvent("Connecting to Sender.");
                    //if successful send a response
                    clientStream.Write(encoding.GetBytes("Processed " + hl7Messages.Count() + " messages."), 0, encoding.GetBytes("Processed " + hl7Messages.Count() + " messages.").Length);

                    LogEvent("Aknowledgement Sent: Processed " + hl7Messages.Count() + " messages.");
                }
                else
                {
                    ProcessMessage(completeMessage.ToString(), clientStream);
                }
            }

            tcpClient.Close();

        }

        public void TestDebug()
        {
            var hl7 =
                "MSH|^~\\&|MEDWAY^5.0.7.6|RXH|ROUTE|ROUTE|20201221141957838+0000||ADT^A31^ADT_A05|RXH_11135385|P|2.4|11135385||\"\"|\"\"|GBR|UNICODE UTF-8|EN||\"\"\r\nEVN|A31|20201221141956740+0000||||20201221141956740+0000\r\nPID|1||4078323^\"\"^^RXH^HOSP~\"\"^\"\"^^NHS^NHS||Epmamon^Rdstest^^^Mr^^L||19551203|9|||1 North Road^^^BRIGHTON^BN1 1YA^GBR^CurrentMAIN^^^^^20201221&\"\"~1 North Road^^^BRIGHTON^BN1 1YA^GBR^MAIN^^^^^20201221&\"\"||07999123456^PH^MOD~012345777777^PH^PRN~demo@email.com^Internet^NET|01254777777^PH^WPN||W|D2|||||A|||||||\"\"|N|\"\"|NSTS03\r\nPD1|||Regency Surgery^^G81656|G9048407^Wilson^SM^^^Dr^^^^^^^GP\r\nNK1|1|NextofKin|04|1 North Road,BRIGHTON,BN1 1YA|012345777777|07999123456|NK\r\nNK1|2|Daughter|09a|1 North Road,BRIGHTON,BN1 1YA|012345777777|07999123456|EC\r\nROL|G81656|UP|FHCP|G9048407^Wilson^SM^^^Dr^^^^^^^GP|||||||Regency Surgery^4 Old Steine^Brighton^East Sussex^BN1 1FZ^GBR^^\"\"\r\n";
            ProcessMessage(hl7, null, null);
        }

        private void ProcessMessage(string data, NetworkStream networkStream, bool? isBulkUpload = null)
        {
            var encoding = new UTF8Encoding();
            //Process data

            var message = new Message(data, ConfigSettings.PasDateSourceFormat);

            try
            {
                LogEvent("Processing:\n" + data);

                if (message.IsValid)
                {

                    //Send the raw data
                    LogEvent(data);

                    MergePatientDataToDatabase(message);

                    if (isBulkUpload == true) return;

                    LogEvent("Sending response...");

                    LogEvent("Connecting to Sender.");

                    if (networkStream != null)
                    {
                        //if successful send a response
                        networkStream.Write(encoding.GetBytes(message.AcknowledgeMessage), 0, message.AcknowledgeMessage.Length);
                    }

                    LogEvent("Aknowledgement Sent: " + message.AcknowledgeMessage);
                }
                else
                {
                    LogEvent("Error: " + message.Error);

                    if (networkStream != null)
                    {
                        networkStream.Write(encoding.GetBytes(message.ErrorAcknowledgeMessage), 0, message.ErrorAcknowledgeMessage.Length);
                    }

                    LogEvent("Aknowledgement Sent for Error Message: " + message.ErrorAcknowledgeMessage);
                }
            }
            catch (Exception ex)
            {
                LogEvent("Error:" + ex.Message);
            }
        }

        // DB Codes
        private void MergePatientDataToDatabase(Message message)
        {
            var patientProperties = message.PID;

            if (ConfigSettings.InstanceName.ToLower().Equals("webtracker"))
            {
                foreach (var connectionString in ConfigSettings.ConnectionStrings)
                {
                    var pasProcedureName = ConfigSettings.FullPasEnabled ? "dbo.PAS_ProcessPatientImportsInfo" : "dbo.PAS_ProcessPatientImports";
                    var conn = new SqlConnection(connectionString);
                    try
                    {
                        var cmd = new SqlCommand(pasProcedureName, conn)
                        {
                            CommandType = System.Data.CommandType.StoredProcedure
                        };
                        cmd.Parameters.AddWithValue("@Pas_ID", patientProperties.PasId);
                        cmd.Parameters.AddWithValue("@Sname", patientProperties.Surname);
                        cmd.Parameters.AddWithValue("@Fname", patientProperties.FirstName);
                        cmd.Parameters.AddWithValue("@Dob", patientProperties.DateOfBirth.HasValue ? patientProperties.DateOfBirth.Value.ToString(ConfigSettings.PasDateTargetFormat) : "");
                        cmd.Parameters.AddWithValue("@Pas_Nhs_no", patientProperties.NhsNo);
                        cmd.Parameters.AddWithValue("@RID", patientProperties.Rid);
                        cmd.Parameters.AddWithValue("@MessageType", "HL7");
                        cmd.Parameters.AddWithValue("@IsMerged", (message.Header.MessageType == "A40" && !string.IsNullOrEmpty(patientProperties.MergePreviousId)));
                        cmd.Parameters.AddWithValue("@ProcessType", message.Header.MessageType);
                        cmd.Parameters.AddWithValue("@PreviousPASID", patientProperties.MergePreviousId);
                        cmd.Parameters.AddWithValue("@PreviousFirstname", patientProperties.MergePreviousFirstName);
                        cmd.Parameters.AddWithValue("@PreviousLastname", patientProperties.MergePreviousSurname);

                        if (ConfigSettings.FullPasEnabled)
                        {
                            cmd.Parameters.AddWithValue("@PatientAddress", patientProperties.PatientAddress ?? "");
                            cmd.Parameters.AddWithValue("@PatientCity", patientProperties.PatientCity ?? "");
                            cmd.Parameters.AddWithValue("@PatientRegion", patientProperties.PatientRegion ?? "");
                            cmd.Parameters.AddWithValue("@PatientPostcode", patientProperties.PatientPostCode ?? "");
                            cmd.Parameters.AddWithValue("@PatientGender", patientProperties.PatientGender ?? "");
                            cmd.Parameters.AddWithValue("@PatientTitle", patientProperties.PatientTitle ?? "");
                            cmd.Parameters.AddWithValue("@GPName", patientProperties.GPName ?? "");
                            cmd.Parameters.AddWithValue("@GPCode", patientProperties.GPCode ?? "");
                            cmd.Parameters.AddWithValue("@GPAddress", patientProperties.GPAddress ?? "");
                            cmd.Parameters.AddWithValue("@GPPostcode", patientProperties.GPPostCode ?? "");
                            cmd.Parameters.AddWithValue("@GPDoctorName", patientProperties.GPDoctorName ?? "");
                            cmd.Parameters.AddWithValue("@GPDoctorCode", patientProperties.GPDoctorCode ?? "");
                            //@AdmittedDate,@DischargeDate,@ChargeIndicator,@AttendingDoctor,@ReferringDoctor,@ConsultingDoctor, @MobileNumber, @PhoneNumber, @WorkPhoneNumber, @Email, @WorkEmail
                            cmd.Parameters.AddWithValue("@AdmittedDate", patientProperties.AdmissionDate ?? DateTime.Parse("01 Jan 1900"));
                            cmd.Parameters.AddWithValue("@DischargeDate", patientProperties.DischargedDate ?? DateTime.Parse("01 Jan 1900"));
                            cmd.Parameters.AddWithValue("@ChargeIndicator", patientProperties.ChargePriceIndicator ?? "");
                            cmd.Parameters.AddWithValue("@AttendingDoctor", patientProperties.AttendingDoctor ?? "");
                            cmd.Parameters.AddWithValue("@ReferringDoctor", patientProperties.ReferringDoctor ?? "");
                            cmd.Parameters.AddWithValue("@ConsultingDoctor", patientProperties.ConsultingDoctor ?? "");
                            cmd.Parameters.AddWithValue("@MobileNumber", patientProperties.MobileNumber ?? "");
                            cmd.Parameters.AddWithValue("@PhoneNumber", patientProperties.PhoneNumber ?? "");
                            cmd.Parameters.AddWithValue("@WorkPhoneNumber", patientProperties.WorkPhoneNumber ?? "");
                            cmd.Parameters.AddWithValue("@Email", patientProperties.Email ?? "");
                            cmd.Parameters.AddWithValue("@WorkEmail", patientProperties.WorkEmail ?? "");
                        }
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                        LogEvent(conn.Database + ": Database updated successfully");
                    }
                    catch (Exception ex)
                    {
                        LogEvent(conn.Database + ": An error occured saving to the database: " + ex.Message);
                    }
                }
            }

        }

        public void ValidateDatabase()
        {

            foreach (var connection in ConfigSettings.ConnectionStrings)
            {
                var conn = new SqlConnection(connection);
                try
                {
                    var cmd = new SqlCommand("SELECT TOP 1 * FROM PAS", conn);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    LogEvent(conn.Database + ":  Database validated");
                }
                catch (Exception ex)
                {
                    LogEvent(conn.Database + ": An error occured connecting to the database: " + ex.Message);
                }
            }
        }

        private static void LogEvent(string message)
        {
            Console.WriteLine(message);
            string fileName = ConfigSettings.Hl7ErrorLogPath;
            if (message.ToLower().Contains("error"))
            {
                fileName = fileName.Replace("logs_", "errorlogs_");
            }
            ConfigSettings.WriteInformationLog(message, fileName);
        }

        protected void AbortHl7Import()
        {

            tcpClient.Close();
            listeningThread.Abort();
            serverTcpListener.Stop();

        }

        #endregion

        protected void AbortCsvImport()
        {
            csvImportThread.Abort();
            hl7ImportThread.Abort();
        }

        public void StopServiceThreads()
        {
            if (listeningThread != null && listeningThread.IsAlive)
            {
                AbortHl7Import();
            }
            if (csvImportThread != null && csvImportThread.IsAlive)
            {
                AbortCsvImport();
            }
        }

        public void ExecuteHl7BulkUpload()
        {
            try
            {
                if (ConfigSettings.Hl7BulkUploadDirectory != null)
                {
                    if (!Directory.Exists(ConfigSettings.Hl7BulkUploadDirectory))
                    {
                        Directory.CreateDirectory(ConfigSettings.Hl7BulkUploadDirectory);
                    }

                    var files = Directory.GetFiles(ConfigSettings.Hl7BulkUploadDirectory, "*").OrderByDescending(d => new FileInfo(d).CreationTime);

                    if (files.Any(p => p.EndsWith("completed.log")))
                    {
                        Thread.Sleep(5000);
                        LogEvent("Completed folders cannot be processed again, please make sure files are not processed and delete completed.log from the folder to process.");
                        return;
                    }

                    if (!Directory.Exists(ConfigSettings.Hl7BulkUploadDirectory + "\\Processed"))
                    {
                        Directory.CreateDirectory(ConfigSettings.Hl7BulkUploadDirectory + "\\Processed");
                    }

                    foreach (var file in files)
                    {
                        ProcessHl7MessageFile(file);

                        if (Directory.Exists(ConfigSettings.Hl7BulkUploadDirectory + "\\Processed"))
                        {
                            File.Move(file, ConfigSettings.Hl7BulkUploadDirectory + "\\Processed\\" + Path.GetFileName(file));
                        }

                        Thread.Sleep(5);
                    }

                    ConfigSettings.SetFolderAsCompleted(ConfigSettings.Hl7BulkUploadDirectory);
                }
                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                LogEvent("Exception Occurred during BulkUpload" + ex.Message);
            }
        }


        public void ProcessHl7MessageFile(string fileName)
        {

            GetConnectedNetworkStream();

            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var data = new byte[fs.Length];
            fs.Read(data, 0, (int)fs.Length);

            stream.Write(data, 0, data.Length);

            Thread.Sleep(50);
            // Buffer to store the response bytes.
            //data = new Byte[256];

            // String to store the response ASCII representation.
            //String responseData = String.Empty;

            // Read the first batch of the TcpServer response bytes.
            //Int32 bytes = networkStream.Read(data, 0, data.Length);
            //responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            //this.txtResponse.Text = responseData;

        }

        private TcpClient client;
        private NetworkStream stream;

        private string GetConnectedNetworkStream()
        {
            var v4Address = String.Empty;

            foreach (var ipa in Dns.GetHostAddresses(Dns.GetHostName()).Where(ipa => ipa.AddressFamily == AddressFamily.InterNetwork))
            {
                v4Address = ipa.ToString();
                break;
            }

            client = new TcpClient(v4Address, int.Parse(ConfigSettings.Hl7TcpPort));
            stream = client.GetStream();

            return v4Address;
        }
    }
}
