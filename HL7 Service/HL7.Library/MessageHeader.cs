using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HL7.Library
{
    public class MessageHeader
    {
        public MessageHeader(string hl7Message)
        {
            ProcessHeader(hl7Message);
        }
        public string EncodingCharaters { get; set; }
        public string SendingApplication { get; set; }
        public string SendingFacility { get; set; }
        public string ReceivingApplication { get; set; }
        public string ReceivingFacility { get; set; }
        public string DateTimeOfMessage { get; set; }
        public string MessageType { get; set; }
        public string MessageControlID { get; set; }
        public string VersionID { get; set; }

        private bool _IsValid = false;
        public bool IsValid
        {
            get { return _IsValid; }
            set { _IsValid = value; }
        }

        public string Error {get; set;}

        public string Header { get; set; }

        /// <summary>
        /// Processes the header into its components
        /// </summary>
        /// <param name="hl7Message"></param>
        private void ProcessHeader(string hl7Message)
        {
            try
            {
                //Read the hl7 message header to an array
                StringReader sr = new StringReader(hl7Message);
                Header = sr.ReadLine();
                string[] values = Header.Split("|"[0]);

                if (values[0].ToUpper().IndexOf("MSH") == -1)
                {
                    //If this message does not begin with MSH, this is not a message header
                    Error = "The message header does not begin with MSH and is corrupt";
                }
                else
                {
                    //Dump the messages into the values
                    EncodingCharaters = values[1];
                    SendingApplication = values[2];
                    SendingFacility = values[3];
                    ReceivingApplication = values[4];
                    ReceivingFacility = values[5];
                    DateTimeOfMessage = values[6];
                    MessageType = values[8].Split('^')[1];
                    MessageControlID = values[9];
                    VersionID = values[11];

                    IsValid = true;

                }
            }
            catch(Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
