using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;
using System.Net;

namespace HL7.TestSender
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private TcpClient _Client;
        private NetworkStream stream;
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.ShowDialog();
            this.txtFile.Text = this.openFileDialog1.FileName;
           

        }

        public static string GetIP4Address()
        {
            string v4Address = String.Empty;

            foreach (IPAddress IPA in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (IPA.AddressFamily == AddressFamily.InterNetwork)
                {
                    v4Address = IPA.ToString();
                    break;
                }
            }

            return v4Address;
        }


        private void btnSend_Click(object sender, EventArgs e)
        {

            this.txtResponse.Text = "";

            try
            {
                if (!string.IsNullOrEmpty(this.txtFile.Text))
                {

                    FileStream fs = new FileStream(this.txtFile.Text, FileMode.Open, FileAccess.Read);
                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, (int) fs.Length);

                    stream.Write(data, 0, data.Length);

                    // Buffer to store the response bytes.
                    data = new Byte[256];

                    // String to store the response ASCII representation.
                    String responseData = String.Empty;

                    // Read the first batch of the TcpServer response bytes.
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    this.txtResponse.Text = responseData;
                }
                else
                {
                    Stream fs = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(txtResponse.Text));
                    var data = new byte[fs.Length];
                    fs.Read(data, 0, (int)fs.Length);

                    stream.Write(data, 0, data.Length);

                    // Buffer to store the response bytes.
                    data = new Byte[256];

                    // String to store the response ASCII representation.
                    String responseData = String.Empty;

                    // Read the first batch of the TcpServer response bytes.
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    this.txtResponse.Text = responseData;
                }
            }
            catch (Exception ex)
            {
                this.txtResponse.Text = "ERROR:\r\n" + ex.Message;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //_Client = new TcpClient(this.txtIPAddress.Text, int.Parse(this.txtPort.Text));
            //stream = _Client.GetStream();

            txtIPAddress.Text = GetIP4Address();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _Client = new TcpClient(this.txtIPAddress.Text, int.Parse(this.txtPort.Text));
            stream = _Client.GetStream();
            
            button1.Text = "Connected.";
            button1.BackColor = System.Drawing.Color.DarkGreen;
            button1.ForeColor = System.Drawing.Color.White;
        }


    }
}
