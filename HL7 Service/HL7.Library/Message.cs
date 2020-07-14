using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;

namespace HL7.Library
{
    public class Message
    {
        //constructor takes in a raw HL7 message
        public Message(string hl7Message, string dateFormat)
        {
            this.hl7Message = hl7Message;

            Initialise(dateFormat);
        }

        private void Initialise(string dateFormat)
        {
            InitialiseHeader();

            //Do not continue processing information without a valid header
            if (IsValid)
            {
                InitialisePatientIndentification(dateFormat);
            }
        }

        private void InitialiseHeader()
        {
            Header = new MessageHeader(hl7Message);
            IsValid = Header.IsValid;
            Error = Header.Error;
        }

        private void InitialisePatientIndentification(string dateFormat)
        {
            PID = new PatientProperties(hl7Message, dateFormat);
            IsValid = PID.IsValid;
            Error = PID.Error;
        }

        private string hl7Message;
        public string Hl7Message
        {
            get { return hl7Message; }
            set { hl7Message = value; }
        }

        public string Error { get; set; }
        public bool IsValid { get; set; }

        public MessageHeader Header {get; set;}
        public PatientProperties PID { get; set; }

        public string AcknowledgeMessage
        {
            get { return GetAcknowledgeMentByType("AA"); }
        }

        /*
         * http://www.interfaceware.com/manual/nack_messages.html
        An AE (Application Error) message indicates that there was a problem processing the message. This could be related to the message structure, or the message itself. The sending application must correct this problem before attempting to resend the message.
        An AR (Application Reject) message indicates one of two things: either there is a problem with field 9, field 11 or field 12 of the MSH segment, or there is a problem with the receiving application that is not related to the message or its structure.
         */

        public string ErrorAcknowledgeMessage
        {
            get { return GetAcknowledgeMentByType("AE"); }
        }

        private string GetAcknowledgeMentByType(string ackType)
        {
            var sb = new StringBuilder();

            var ackHeader = Header.Header;
            if (ackHeader != null)
            {
                var ackHeaderSegments = ackHeader.Split('|');
                ackHeaderSegments[2] = ConfigSettings.Hl7AckSourceName;//MSH 1.2
                ackHeaderSegments[8] = "ACK^A01";//MSH 1.8
                ackHeader = string.Join("|", ackHeaderSegments);
            }

            if (ConfigSettings.Hl7DoubleVerticalHeaderSegmentStart)
            {
                sb.Append("\v");
            }

            sb.Append(ackHeader);

            sb.Append(string.Format("\rMSA|{0}|{1}|Message Acknowledged" + ConfigSettings.Hl7AckPipes, ackType, Header.MessageControlID));

            sb.Append("\x1C\r");
            return sb.ToString();
        }
    }
}
