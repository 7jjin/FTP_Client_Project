using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FTP_Client_Project
{
    public partial class Form1 : Form
    {
        TcpClient clientSocket = new TcpClient(); // 소켓
        NetworkStream stream = default(NetworkStream);
        string message = string.Empty;
        private int PORT = 21; // 포트 정보
        private string USER_NAME = string.Empty;
        private static string CONNECT_STATUS = "DISCONNECT"; // 연결상태
 
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitForm();
            this.Text = "Client";
            ipBox.Text = "TCP Server의 IP 입력";
            idBox.Text = "조진형";
            txtStatus.Text = "대기중...";
        }

        // 초기화
        private void InitForm()
        {
            ipBox.Text = "";
            idBox.Text = "";
            pwBox.Text = "";
            richTextBox1.Text = "";
        }

        // Disconnect 메서드
        private void DisConnect()
        {
            clientSocket = null;
            byte[] buffer = Encoding.Unicode.GetBytes("LeaveChat" + "$");
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();
        }

        // Connect 메서드
        private void Connect()
        {
            clientSocket.Connect(ipBox.Text.ToString(), PORT); // 접속 IP 및 포트
            stream = clientSocket.GetStream();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (CONNECT_STATUS.Equals("DISCONNECT"))
                {
                    Connect();
                    USER_NAME = idBox.Text.Trim();
                    txtStatus.Text = "연결됨";
                    CONNECT_STATUS = "CONNECT";
                }
                else if (CONNECT_STATUS.Equals("CONNECT"))
                {
                    return;
                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("서버가 실행중이 아닙니다.", "연결 실패!");
            }

            message = "채팅 서버에 연결 되었습니다.";
            DisplayText(message);

            byte[] buffer = Encoding.Unicode.GetBytes(USER_NAME + "$");

            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();

            Thread t_handler = new Thread(GetMessage);
            t_handler.IsBackground = true;
            t_handler.Start();
        }

        // 메세지 수신
        private void GetMessage() // 메세지 받기
        {
            while (true)
            {
                stream = clientSocket.GetStream();
                int BUFFERSIZE = clientSocket.ReceiveBufferSize;
                byte[] buffer = new byte[BUFFERSIZE];
                int bytes = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.Unicode.GetString(buffer, 0, bytes);

                DisplayText(message);
            }
        }

        // 연결 끊기
        private void button2_Click(object sender, EventArgs e)
        {
            if (CONNECT_STATUS.Equals("DISCONNECT"))
            {
                MessageBox.Show("연결된 상태가 아닙니다.");
                return;
            }
            else if (CONNECT_STATUS.Equals("CONNECT"))
            {
                DisConnect();
                txtStatus.Text = "대기중...";
                CONNECT_STATUS = "DISCONNECT";
                return;
            }
            else
            { }
        }

        // 전송 버튼 클릭
        //private void btnSendMessage_Click(object sender, EventArgs e)
        //{
        //    txtMessage.Focus();
        //    byte[] buffer = Encoding.Unicode.GetBytes(txtMessage.Text + "$");
        //    stream.Write(buffer, 0, buffer.Length);
        //    stream.Flush();

        //    txtMessage.Text = "";
        //}

        //private void txtMessage_KeyUp(object sender, KeyEventArgs e)
        //{
        //    if (e.KeyCode == Keys.Enter) // 엔터키 눌렀을 때
        //        btnSendMessage_Click(this, e);
        //}

        private void DisplayText(string text) // Server에 메세지 출력
        {
            richTextBox1.Invoke((MethodInvoker)delegate { richTextBox1.AppendText(text + "\r\n"); }); // 데이타를 수신창에 표시, 반드시 invoke 사용. 충돌피함.
            richTextBox1.Invoke((MethodInvoker)delegate { richTextBox1.ScrollToCaret(); });  // 스크롤을 젤 밑으로.
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisConnect();

            Application.ExitThread();
            Environment.Exit(0);
        }
    }
}
