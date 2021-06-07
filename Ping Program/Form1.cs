using System;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Ping_Program
{
    public partial class Form1 : Form
    {
        /*Global variables*/
        Thread thread;

        #region ICMP Class
        public class ICMP
        {
            public byte Type;
            public byte Code;
            public UInt16 Checksum;
            public int MessageSize;
            public byte[] Message = new byte[1024];

            public ICMP()
            {
            }

            public ICMP(byte[] data, int size)
            {
                Type = data[20];
                Code = data[21];
                Checksum = BitConverter.ToUInt16(data, 22);
                MessageSize = size - 24;
                Buffer.BlockCopy(data, 24, Message, 0, MessageSize);
            }

            public byte[] getBytes()
            {
                byte[] data = new byte[MessageSize + 9];
                Buffer.BlockCopy(BitConverter.GetBytes(Type), 0, data, 0, 1);
                Buffer.BlockCopy(BitConverter.GetBytes(Code), 0, data, 1, 1);
                Buffer.BlockCopy(BitConverter.GetBytes(Checksum), 0, data, 2, 2);
                Buffer.BlockCopy(Message, 0, data, 4, MessageSize);
                return data;
            }

            public UInt16 getChecksum()
            {
                UInt32 chcksm = 0;
                byte[] data = getBytes();
                int packetsize = MessageSize + 8;
                int index = 0;

                while (index < packetsize)
                {
                    chcksm += Convert.ToUInt32(BitConverter.ToUInt16(data, index));
                    index += 2;
                }
                chcksm = (chcksm >> 16) + (chcksm & 0xffff);
                chcksm += (chcksm >> 16);
                return (UInt16)(~chcksm);
            }
        }
        #endregion

        // delegate function
        public delegate void Add_Items(ListBox lb, string message);
        public void Add_Items_2_Listbox(ListBox lb, string message)
        {
            if (lb.InvokeRequired)
            {
                Add_Items d = new Add_Items(Add_Items_2_Listbox);
                this.Invoke(d, new object[] { lb, message });
            }
            else
            {
                lb.Items.Add(message);
            }
        }

        // Thread
        [Obsolete]
        public void Thread_Ping()
        {
            byte[] data = new byte[1024];
            int recv;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            IPHostEntry iphe = Dns.Resolve(txt_host.Text);
            IPEndPoint ipe = new IPEndPoint(iphe.AddressList[0], 0);
            EndPoint ep = (EndPoint)ipe;

            ICMP packet = new ICMP();
            packet.Type = 0x08; // Echo request
            packet.Code = 0x00;
            packet.Checksum = 0;
            Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, packet.Message, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, packet.Message, 2, 2);
            string packetData = "";
            if (txt_packetData.Text == string.Empty)
            {
                packetData = "Test packages";
            }
            else
            {
                packetData = txt_packetData.Text;
            }
            data = Encoding.ASCII.GetBytes(packetData);
            Buffer.BlockCopy(data, 0, packet.Message, 0, data.Length);
            packet.MessageSize = data.Length + 4;
            int packetsize = packet.MessageSize + 4;
            packet.Checksum = packet.getChecksum();
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 3000);
            Add_Items_2_Listbox(lb_receivedPingData, $">> Pinging {ipe} with {packetsize} bytes of data");

            int repeat = 4;
            if (txtRepeat.Text != string.Empty && int.TryParse(txtRepeat.Text, out int n)) // Check a string is a numberic?
            {
                repeat = int.Parse(txtRepeat.Text);
            }
            for (int i = 1; i <= repeat; i++)
            {
                DateTime sentAt = DateTime.Now;
                socket.SendTo(packet.getBytes(), packetsize, SocketFlags.None, ipe);
                try
                {
                    data = new byte[1024];
                    recv = socket.ReceiveFrom(data, ref ep);
                }
                catch (SocketException)
                {
                    Add_Items_2_Listbox(lb_receivedPingData, "No response from remote host");
                    return;
                }
                ICMP response = new ICMP(data, recv);
                DateTime recvdAt = DateTime.Now;
                long PingRTT = (long)(recvdAt - sentAt).TotalMilliseconds;
                Add_Items_2_Listbox(lb_receivedPingData, $"Reply from {ep} seq: {i}, tims:{PingRTT} ms");
                Thread.Sleep(1000);
            }
            Add_Items_2_Listbox(lb_receivedPingData, string.Empty);
        }

        /* Main functions */

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txt_host.Text = "www.google.com";
        }

        [Obsolete]
        private void btn_start_Click(object sender, EventArgs e)
        {
            thread = new Thread(new ThreadStart(Thread_Ping));
            thread.Start();
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            thread.Abort();
        }
    }
}
