using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Diagnostics;

namespace FTP_Client_Project
{
    public partial class Form1 : Form
    {
        TcpClient clientSocket = new TcpClient(); // 소켓
        NetworkStream stream = default(NetworkStream);
        string message = string.Empty;
        private int PORT = 21; // 포트 정보
        // private string USER_NAME = string.Empty;
        private static string CONNECT_STATUS = "DISCONNECT"; // 연결상태
        private string Id = string.Empty;
        private string PASSWORD = string.Empty;

        public Form1()
        {
            InitializeComponent();
            // 트리뷰 이벤트 핸들러 연결
            myDirectory.BeforeExpand += treeView1_BeforeExpand;
            myDirectory.NodeMouseClick += treeView1_NodeMouseClick;
            ftpDirectory.BeforeExpand += treeView2_BeforeExpand;
            //ftpDirectory.NodeMouseClick += treeView2_NodeMouseClick;
            //listView1.ItemDrag += new ItemDragEventHandler(listView_ItemDrag);
            //listView2.ItemDrag += new ItemDragEventHandler(listView_ItemDrag);
            //listView1.DragEnter += new DragEventHandler(listView_DragEnter);
            //listView2.DragEnter += new DragEventHandler(listView_DragEnter);
            //listView1.DragDrop += new DragEventHandler(listView_DragDrop);
            //listView2.DragDrop += new DragEventHandler(listView_DragDrop);
            //listView1.DragOver += new DragEventHandler(listView_DragOver);
            //listView2.DragOver += new DragEventHandler(listView_DragOver);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitForm();
            this.Text = "Client";
            txtStatus.Text = "대기중...";

            // 내 드라이브의 모든 폴더 경로 가져오기
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            Console.WriteLine("My_allDrives",allDrives);
            

            foreach (DriveInfo dname in allDrives)
            {
                if (dname.DriveType == DriveType.Fixed)
                {
                    TreeNode rootNode = new TreeNode(dname.Name);
                    myDirectory.Nodes.Add(rootNode);
                    Fill(rootNode);
                }
            }
            //첫번째 노드 확장
            myDirectory.Nodes[0].Expand();

            //ListView 보기 속성 설정
            listView1.View = View.Details;
            listView2.View = View.Details;

            //ListView Details 속성을 위한 헤더 추가
            listView1.Columns.Add("이름", listView1.Width / 4, HorizontalAlignment.Left);
            listView1.Columns.Add("수정한 날짜", listView1.Width / 4, HorizontalAlignment.Left);
            listView1.Columns.Add("유형", listView1.Width / 4, HorizontalAlignment.Left);
            listView1.Columns.Add("크기", listView1.Width / 4, HorizontalAlignment.Left);

            listView2.Columns.Add("이름", listView2.Width / 4, HorizontalAlignment.Left);
            listView2.Columns.Add("수정한 날짜", listView2.Width / 4, HorizontalAlignment.Left);
            listView2.Columns.Add("유형", listView2.Width / 4, HorizontalAlignment.Left);
            listView2.Columns.Add("크기", listView2.Width / 4, HorizontalAlignment.Left);

            //행 단위 선택 가능
            listView1.FullRowSelect = true;
            listView2.FullRowSelect = true;
        }

        // 디렉토리 정보를 Treeview에 뿌려주기
        private void Fill(TreeNode dirNode)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(dirNode.FullPath);
                //드라이브의 하위 폴더 추가
                foreach (DirectoryInfo dirItem in dir.GetDirectories())
                {
                    TreeNode newNode = new TreeNode(dirItem.Name);
                    dirNode.Nodes.Add(newNode);
                    newNode.Nodes.Add("*");
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }
        /// <summary>
        /// 트리가 확장되기 전에 발생하는 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes[0].Text == "*")
            {
                e.Node.Nodes.Clear();
                Fill(e.Node);
            }
        }
        /// <summary>
        /// 트리가 닫히기 전에 발생하는 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes[0].Text == "*")
            {
                e.Node.Nodes.Clear();
                Fill(e.Node);
            }
        }
        private void treeView2_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes[0].Text == "*")
            {
                e.Node.Nodes.Clear();
                Fill(e.Node);
            }
        }
        /// <summary>
        /// 트리가 닫히기 전에 발생하는 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView2_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes[0].Text == "*")
            {
                e.Node.Nodes.Clear();
                Fill(e.Node);
            }
        }
        /// <summary>
        /// 트리를 마우스로 클릭할 때 발생하는 이벤트
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            SettingMyListVeiw(e.Node.FullPath);
        }
        /// <summary>
        /// Client 아래 ListView를 그린다.
        /// </summary>
        /// <param name="sFullPath"></param>
        private void SettingMyListVeiw(string sFullPath)
        {
            try
            {
                //기존의 파일 목록 제거
                listView1.Items.Clear();
                //현재 경로를 표시

                localTextBox.Text = sFullPath;
                DirectoryInfo dir = new DirectoryInfo(sFullPath);
                int DirectCount = 0;
                //하부 데렉토르 보여주기
                foreach (DirectoryInfo dirItem in dir.GetDirectories())
                {
                    //하부 디렉토리가 존재할 경우 ListView에 추가
                    //ListViewItem 객체를 생성
                    ListViewItem lsvitem = new ListViewItem();
                    //생성된 ListViewItem 객체에 똑같은 이미지를 할당
                    lsvitem.Text = dirItem.Name;
                    //아이템을 ListView(listView1)에 추가
                    listView1.Items.Add(lsvitem);
                    listView1.Items[DirectCount].SubItems.Add(dirItem.CreationTime.ToString());
                    listView1.Items[DirectCount].SubItems.Add("폴더");
                    listView1.Items[DirectCount].SubItems.Add(dirItem.GetFiles().Length.ToString() + " files");
                    DirectCount++;
                }
                //디렉토리에 존재하는 파일목록 보여주기
                FileInfo[] files = dir.GetFiles();
                int Count = 0;
                foreach (FileInfo fileinfo in files)
                {
                    listView1.Items.Add(fileinfo.Name);
                    if (fileinfo.LastWriteTime != null)
                    {
                        listView1.Items[Count].SubItems.Add(fileinfo.LastWriteTime.ToString());
                    }
                    else
                    {
                        listView1.Items[Count].SubItems.Add(fileinfo.CreationTime.ToString());
                    }
                    listView1.Items[Count].SubItems.Add(fileinfo.Attributes.ToString());
                    listView1.Items[Count].SubItems.Add(fileinfo.Length.ToString());
                    Count++;
                }
            }
            catch (Exception ex)
            {
                return;
            }
            myDirectory.Nodes[0].Expand();
        }


        teststsetsttsetsetsetset

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

            // ID와 PW 전송
            string credentials = idBox.Text + "$" + pwBox.Text;
            byte[] buffer = Encoding.Unicode.GetBytes(credentials);
            stream.Write(buffer, 0, buffer.Length);
            stream.Flush();

            // 서버 응답 받기
            buffer = new byte[1024];
            int bytes = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.Unicode.GetString(buffer, 0, bytes);

            if (response == "Authentication successful")
            {
                MessageBox.Show("인증성공");
                txtStatus.Text = "연결됨";
                CONNECT_STATUS = "CONNECT";
                DisplayText("서버에 연결 되었습니다.");

                // 폴더 목록 수신
                
                buffer = new byte[1024];
                bytes = stream.Read(buffer, 0, buffer.Length);
                //string folderList = Encoding.Unicode.GetString(buffer, 0, bytes);
                //List<string> folderAndFileList = JsonConvert.DeserializeObject<List<string>>(folderList);
                //PopulateTreeView(ftpDirectory, folderList);
                string resultJson = Encoding.UTF8.GetString(buffer, 0, bytes);
                List<DriveInfo> drives = JsonConvert.DeserializeObject<List<DriveInfo>>(resultJson);

                Debug.WriteLine("folderList: " , resultJson);
                DisplayText("폴더 목록:\n" + resultJson);


                Thread t_handler = new Thread(GetMessage);
                t_handler.IsBackground = true;
                t_handler.Start();
            }
            else
            {
                MessageBox.Show("인증 실패: " + response);
                clientSocket.Close();
                clientSocket = null;
                txtStatus.Text = "대기중...";
                CONNECT_STATUS = "DISCONNECT";
            }
        }

        private void PopulateTreeView(System.Windows.Forms.TreeView treeView, string json)
        {
            // JSON 문자열을 리스트로 변환
            List<string> paths = JsonConvert.DeserializeObject<List<string>>(json);
            if (paths == null || paths.Count == 0)
                return;

            // 트리뷰 초기화
            treeView.Nodes.Clear();

            // 트리뷰에 루트 노드 추가
            TreeNode rootNode = null;
            foreach (string path in paths)
            {
                string[] parts = path.Split('\\');
                if (parts.Length == 0)
                    continue;

                if (rootNode == null)
                {
                    // 첫 번째 경로에서 루트 노드 생성
                    rootNode = new TreeNode(parts[0]);
                    treeView.Nodes.Add(rootNode);
                }

                TreeNode currentNode = rootNode;

                for (int i = 1; i < parts.Length; i++)
                {
                    // 현재 노드의 하위 노드 중 동일한 이름을 가진 노드가 있는지 확인
                    TreeNode[] foundNodes = currentNode.Nodes.Find(parts[i], false);

                    if (foundNodes.Length == 0)
                    {
                        // 해당 이름의 하위 노드가 없으면 새로 생성
                        TreeNode newNode = new TreeNode(parts[i])
                        {
                            Name = parts[i]
                        };
                        currentNode.Nodes.Add(newNode);
                        currentNode = newNode;
                    }
                    else
                    {
                        // 해당 이름의 하위 노드가 있으면 그 노드를 현재 노드로 설정
                        currentNode = foundNodes[0];
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (CONNECT_STATUS.Equals("DISCONNECT"))
                {
                    Connect();
                    Id = idBox.Text.Trim();
                    PASSWORD = pwBox.Text.Trim();
                    //ReceiveFolderList();


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
        }

        // 폴더 및 파일 목록 수신 함수
        public string ReceiveData(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            // 데이터 크기 수신
            byte[] dataSize = new byte[4];
            stream.Read(dataSize, 0, 4);
            int size = BitConverter.ToInt32(dataSize, 0);

            // 실제 데이터 수신
            byte[] dataBytes = new byte[size];
            stream.Read(dataBytes, 0, size);

            string data = Encoding.UTF8.GetString(dataBytes);
            return data;
        }

        private void PopulateTreeView(List<string> folderAndFileList)
        {
            ftpDirectory.Nodes.Clear();
            foreach (string path in folderAndFileList)
            {
                TreeNode node = new TreeNode(path);
                ftpDirectory.Nodes.Add(node);
            }
        }

        // 폴더 및 파일 경로에 따라 TreeNode를 생성하는 재귀 함수
        private TreeNode CreateTreeNode(string path)
        {
            TreeNode node = new TreeNode(Path.GetFileName(path));
            if (Directory.Exists(path))
            {
                try
                {
                    string[] subDirs = Directory.GetDirectories(path);
                    foreach (string subDir in subDirs)
                    {
                        node.Nodes.Add(CreateTreeNode(subDir));
                    }
                    string[] files = Directory.GetFiles(path);
                    foreach (string file in files)
                    {
                        node.Nodes.Add(new TreeNode(Path.GetFileName(file)));
                    }
                }
                catch (Exception ex)
                {
                    // 예외 처리 (예: 접근 권한 없음)
                }
            }
            return node;
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
            // richTextBox1.Invoke((MethodInvoker)delegate { richTextBox1.ScrollToCaret(); });  // 스크롤을 젤 밑으로.
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisConnect();

            Application.ExitThread();
            Environment.Exit(0);
        }
    }
}
