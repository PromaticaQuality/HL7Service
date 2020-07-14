using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace HL7.Library
{
    public class PatientProperties
    {
        public PatientProperties(string hl7Message, string format)
        {
            IsValid = false;
            Process(hl7Message, format);
        }

        //Patient properties
        public string Rid { get; set; }

        public string PasId { get; set; }

        public string FirstName { get; set; }

        public string Surname { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string NhsNo { get; set; }

        public string PatientAddress { get; set; }

        public string PatientPostCode { get; set; }

        public string PatientCity { get; set; }

        public string PatientRegion { get; set; }
        public string PatientGender { get; set; }
        public string PatientTitle { get; set; }

        public string PhoneNumber { get; set; }

        public string GPName { get; set; }

        public string GPCode { get; set; }
        public string GPDoctorCode { get; set; }
        public string GPDoctorName { get; set; }

        public string GPAddress { get; set; }

        public string GPPostCode { get; set; }

        public string CurrentLocationPointOfCare { get; set; }
        public string CurrentLocationRoom { get; set; }
        public string CurrentLocationBed { get; set; }
        public string CurrentLocationUnitCode { get; set; }
        
        public string PreviousLocationPointOfCare { get; set; }
        public string PreviousLocationRoom { get; set; }
        public string PreviousLocationBed { get; set; }
        public string PreviousLocationUnitCode { get; set; }
        
        public string MergePreviousId { get; set; }

        public string MergePreviousFirstName { get; set; }
        public string MergePreviousSurname { get; set; }

        public string RawMessageContent { get; set; }
        

        public string Error { get; set; }
        public bool IsValid { get; set; }

        private void Process(string hl7Message, string dateFormat)
        {
            RawMessageContent = hl7Message;

            try
            {
                //Read the hl7 message header to an array
                var sr = new StringReader(hl7Message);
                string line = null;

                while ((line = sr.ReadLine()) != null) // 0|3||4
                {
                    var values = line.Split("|"[0]);

                    if (values[0].ToUpper().IndexOf("PID", StringComparison.Ordinal) == 0)
                    {
                        //Identifiers that could be different for different hospitals
                        //NhsNo = values[3].Split('^')[0];

                        ProcessIdentifiers(values[3]);

                        ProcessName(values[5]);

                        ProcessDob(values[7], dateFormat);
                        PatientTitle = values[5].Split('^').LastOrDefault();
                        PatientGender = values[8];

                        ProcessAddress(values[11]);

                        ProcessPhoneNumber(values[13]);

                        IsValid = !String.IsNullOrEmpty(this.PasId);
                        if (String.IsNullOrEmpty(this.PasId))
                        {
                            Error = "Record doesn't have PAS Identifier.";
                        }
                    }

                    if (values[0].ToUpper().IndexOf("PV1", StringComparison.Ordinal) == 0)
                    {
                        ProcessTransfer(line);
                    }
                    if (values[0].ToUpper().IndexOf("PD1", StringComparison.Ordinal) == 0)
                    {
                        ProcessGPDetails(line);
                    }
                    if (values[0].ToUpper().IndexOf("MRG", StringComparison.Ordinal) == 0)
                    {
                        ProcessMerge(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                ConfigSettings.WriteInformationLog("Exception occurred while processing message: " + hl7Message + "\r Error: " + ex.Message, ConfigSettings.Hl7ErrorLogPath);
            }

            Error = "There is no patient record in the message";
        }
        private void ProcessPhoneNumber(string source)
        {
            PhoneNumber = source;
        }

        private void ProcessAddress(string source)
        {
            if (string.IsNullOrEmpty(source)) return;

            var addressParts = source.Split('^');

            if (addressParts.Length < 1) return;

            if (addressParts.Length > 0)
            {
                var streetAddressLine1 = addressParts[0];
                PatientAddress = (string.IsNullOrEmpty(streetAddressLine1) ? "" : streetAddressLine1 + ", ");
            }

            if (addressParts.Length > 1)
            {
                var streetAddressLine2 = addressParts[1];

                PatientAddress += (string.IsNullOrEmpty(streetAddressLine2) ? "" : streetAddressLine2 + ", ");
            }

            if (addressParts.Length > 2)
            {
                var city = addressParts[2];
                PatientCity = city;
            }
            if (addressParts.Length > 3)
            {
                PatientRegion = addressParts[3];
            }
            if (addressParts.Length > 4)
            {
                PatientPostCode = addressParts[4];
            }

        }
        private void ProcessGPDetails(string source)
        {
            var gpDetails = source.Split('|')[3].Split('^');
            if (gpDetails.Length > 0)
            {
                GPName = gpDetails[0];
            }
            if (gpDetails.Length > 2)
            {
                GPCode = gpDetails[2];
            }
            var doctorDetails = source.Split('|')[4].Split('^');
            if (doctorDetails.Length > 0)
            {
                GPDoctorCode = doctorDetails[0];
            }
            if (doctorDetails.Length > 1)
            {
                GPDoctorName = doctorDetails[1];
            }
        }

        private void ProcessMerge(string source)
        {
            if (ConfigSettings.MRGHasFirstSegmentAsPasID)
            {
                MergePreviousId = source.Split('|')[1].Split('^')[0];
                MergePreviousFirstName = source.Split('|')[1].Split('^')[7];
            }
            else
            {
                MergePreviousId = source.Split('|')[1];
                ProcessPreviousName(source.Split('|')[7]);
            }
        }

        private void ProcessTransfer(string source)
        {
            var currentLocation = source.Split('|')[3];
            var priorLocation = source.Split('|')[6];

            CurrentLocationUnitCode = currentLocation.Split('^')[4];
            PreviousLocationUnitCode = priorLocation.Split('^')[4];
        }
      

        private void ProcessIdentifiers(string segment)
        {
            /**
            From the sample 6009312345^^^1^NN^01~7112345^^^1^DIS^""~1312345^^^1^BRI^""~1712345M^^^1^BCH^""~212345R^^^1^BOC^""~612345D^^^1^BDH^""~E0712345^^^1^A\T\E^""
             
             you would get:
             

            6009312345 NN
            7112345 DIS
            1312345 BRI
            1712345M BCH
            212345R BOC
            612345D BDH
            E0712345 A\T\E 

            (We would expect you to convert the \T\ escape char to a &)

            For reference the number types break down as follows:

            NN – NHS Number
            DIS – District Number
            BRI – Bristol Royal Infirmary
            BCH – Bristol Children’s Hospital
            BOC – Bristol Oncology Centre
            BDH – Bristol Dental Hospital
            A&E - Accident and Emergency

            HL7 2.4 onwards says that a ~ character is used as a repeating separator.  The sample contains 7 repeating sets of the following data:

            Number^Not supported^Not supported^Number Status^Number Type^Trace status if Number type is NN, if type is FT then a description of the type is given~

            That becomes
            IDNumber^^^1^IDCode^NN Status
            Repeated using ~ to delimit the repeats

            **/

            //Split the string by the ~ delimeter
            var chunks = segment.Split("~"[0]);

            foreach (string[] set in chunks.Select(chunk => chunk.Split("^"[0])))
            {
                //The segment is the code which Identifies the NHS Number (e.g. NN or NHS )
                if(set.Length>ConfigSettings.Hl7IdentifiersIndex && set[ConfigSettings.Hl7IdentifiersIndex]== ConfigSettings.Hl7PasidSegmentName && PasId==null)
                {
                    PasId = set[0];
                }

                //The segment is the code which Identifies the PAS ID (e.g. HOSP or PAS or XYZ )
                if (set.Length > ConfigSettings.Hl7IdentifiersIndex && (set[ConfigSettings.Hl7IdentifiersIndex] == ConfigSettings.Hl7NhsNoSegmentName) || set[ConfigSettings.Hl7IdentifiersIndex].Replace(" ","") == ConfigSettings.Hl7NhsNoSegmentName)
                {
                    NhsNo = set[0];
                }
            }
        }

        private void ProcessName(string segment)
        {
            //expected: VADAR^DARTH^FRANK^""^LORD^""^C
            //we want: f:DARTH s:VADER

            var names = segment.Split("^"[0]);
            FirstName = names[1];
            Surname = names[0];
        }

        private void ProcessPreviousName(string segment)
        {
            //expected: VADAR^DARTH^FRANK^""^LORD^""^C
            //we want: f:DARTH s:VADER

            var names = segment.Split("^"[0]);
            MergePreviousFirstName = names[1];
            MergePreviousSurname = names[0];

        }


        private void ProcessDob(string segment, string format)
        {
            DateTime date;
            if(DateTime.TryParseExact(segment, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                DateOfBirth = date;
            }
        }
    }
}
