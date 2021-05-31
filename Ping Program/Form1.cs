using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        /* Classes*/

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

        /*Child functions*/

        public void Thread_Ping(object _ipe)
        {
            IPEndPoint ipe = (IPEndPoint)_ipe;
            EndPoint ep = (EndPoint)ipe;

            Socket socket = new Socket(ipe.AddressFamily, SocketType.Raw, ProtocolType.Icmp);

            byte[] data = new byte[1024];
            ICMP packet = new ICMP();

            packet.Type = 0x08; // Echo request
            packet.Code = 0x00;
            packet.Checksum = 0;
            Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, packet.Message, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, packet.Message, 2, 2);
            data = Encoding.ASCII.GetBytes("test packet");
            Buffer.BlockCopy(data, 0, packet.Message, 0, data.Length);
            packet.MessageSize = data.Length + 4;
            int packetsize = packet.MessageSize + 4;

            packet.Checksum = packet.getChecksum();

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);
            socket.SendTo(packet.getBytes(), packetsize, SocketFlags.None, ipe);
            Console.WriteLine($"Pinging {ipe} with {packetsize} bytes of data");

            int sizeof_IPheader;
            for (int i = 0; i < 4; i++)
            {
                DateTime sentAt = DateTime.Now;
                socket.SendTo(packet.getBytes(), packetsize, SocketFlags.None, ipe);

                try
                {
                    byte[] data_recv = new byte[1024];
                    sizeof_IPheader = socket.ReceiveFrom(data_recv, ref ep);
                    DateTime recvdAt = DateTime.Now;

                    ICMP response = new ICMP(data_recv, sizeof_IPheader);

                    long PingRTT = (long)(recvdAt - sentAt).TotalMilliseconds;
                    Console.WriteLine($"Reply from {ep} tims:{PingRTT} ms");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Thread.Sleep(1000);
            }

            
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

        private void btn_start_Click(object sender, EventArgs e)
        {
            //IPAddress ip_address = IPAddress.Parse(txt_host.Text);
            IPAddress ip_address = Dns.GetHostAddresses(txt_host.Text)[1];
            //IPAddress ip_address = Dns.GetHostEntry(txt_host.Text).AddressList[0];
            Console.WriteLine(ip_address);
            IPEndPoint ipe = new IPEndPoint(ip_address, 0);

            thread = new Thread(new ParameterizedThreadStart(Thread_Ping));
            thread.Start(ipe);

            Console.WriteLine("Thread is starting");
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            thread.Abort();
        }

        private void btn_close_Click(object sender, EventArgs e)
        {

        }

    }
}
