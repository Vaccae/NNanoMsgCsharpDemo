using NNanomsg.Protocols;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace nanomsgcsharp
{
    public partial class frmSrv : Form
    {
        private bool _isconnect = false;

        private Lazy<PairSocket> pairSocket;

        private Lazy<BusSocket> busSocket;

        private Lazy<ReplySocket> replySocket;

        private Lazy<PublishSocket> publishSocket;

        private Lazy<SurveyorSocket> surveyorSocket;


        #region 文本框操作

        //定义文本框
        private static TextBox _tbMsg;

        //定义Action
        private Action<string> TextShowAction = new Action<string>(TextShow);

        //定义更新UI函数
        private static void TextShow(string sMsg)
        {
            //当文本行数大于500后清空
            if (_tbMsg.Lines.Length > 500)
            {
                _tbMsg.Clear();
            }

            string ShowMsg = DateTime.Now + "  " + sMsg + "\r\n";
            _tbMsg.AppendText(ShowMsg);

            //让文本框获取焦点 
            _tbMsg.Focus();
            //设置光标的位置到文本尾 
            _tbMsg.Select(_tbMsg.TextLength, 0);
            //滚动到控件光标处 
            _tbMsg.ScrollToCaret();
        }

        #endregion

        public frmSrv()
        {
            InitializeComponent();

            _tbMsg = tbMsg;

            CheckForIllegalCrossThreadCalls = false;

            //加载扩展协议
            cmbtype.Items.Clear();
            foreach (string name in Enum.GetNames(typeof(ENanoType)))
            {
                cmbtype.Items.Add(name);
            }
        }

        /// <summary>
        /// 连接按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            switch ((ENanoType)cmbtype.SelectedIndex)
            {
                case ENanoType.PAIR:
                    PairSocketSrvConnect();
                    break;
                case ENanoType.BUS:
                    BusSocketSrvConnect();
                    break;
                case ENanoType.REQREP:
                    REQREPSocketSrvConnect();
                    break;
                case ENanoType.PUBSUB:
                    PUBSUBSrvConnect();
                    break;
                case ENanoType.SURVEY:
                    SURVEYSrcConnect();
                    break;
            }
        }

        /// <summary>
        /// SURVEY模式
        /// </summary>
        private void SURVEYSrcConnect()
        {
            try
            {
                surveyorSocket = new Lazy<SurveyorSocket>(() => new SurveyorSocket());
                surveyorSocket.Value.Bind("tcp://*:8005");

                new Task(() =>
                {
                    while (surveyorSocket.IsValueCreated)
                    {
                        Thread.Sleep(1000);

                        //发送数据
                        string sendstr = "SURVEY消息，请查收！";
                        surveyorSocket.Value.Send(Encoding.UTF8.GetBytes(sendstr));
                        TextShow(sendstr);

                        //接收消息
                        byte[] buffer = surveyorSocket.Value.Receive();
                        if (buffer != null)
                        {
                            string recvstr = Encoding.UTF8.GetString(buffer);
                            TextShow(recvstr);
                        }                     
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                TextShow(ex.Message);
            }
        }

        /// <summary>
        /// PUBSUB连接
        /// </summary>
        private void PUBSUBSrvConnect()
        {
            try
            {
                publishSocket = new Lazy<PublishSocket>(() => new PublishSocket());
                publishSocket.Value.Bind("tcp://*:8004");
            }
            catch (Exception ex)
            {
                TextShow(ex.Message);
            }
        }

        /// <summary>
        /// Pair连接
        /// </summary>
        private void PairSocketSrvConnect()
        {
            try
            {
                pairSocket = new Lazy<PairSocket>(() => new PairSocket());
                pairSocket.Value.Bind("tcp://*:8001");

                new Task(() =>
                {
                    while (pairSocket.IsValueCreated)
                    {
                        Thread.Sleep(1000);
                        //接收数据
                        byte[] buffer = pairSocket.Value.Receive();
                        string recvstr = Encoding.UTF8.GetString(buffer);
                        TextShow(recvstr);

                        //发送数据
                        string sendstr = "PAIR模式收到消息：" + recvstr + "。现在是返回的消息！";
                        pairSocket.Value.Send(Encoding.UTF8.GetBytes(sendstr));
                        TextShow(sendstr);
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                TextShow(ex.Message);
            }
        }


        /// <summary>
        /// BUS连接
        /// </summary>
        private void BusSocketSrvConnect()
        {
            try
            {
                busSocket = new Lazy<BusSocket>(() => new BusSocket());
                busSocket.Value.Bind("tcp://*:8002");
                busSocket.Value.Connect("tcp://192.168.3.2:8002");
                new Task(() =>
                {
                    while (busSocket.IsValueCreated)
                    {
                        Thread.Sleep(1000);
                        //接收数据
                        byte[] buffer = busSocket.Value.Receive();
                        string recvstr = Encoding.UTF8.GetString(buffer);
                        TextShow(recvstr);
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                TextShow(ex.Message);
            }
        }


        /// <summary>
        /// REQREP
        /// </summary>
        private void REQREPSocketSrvConnect()
        {
            try
            {
                replySocket = new Lazy<ReplySocket>(() => new ReplySocket());
                replySocket.Value.Bind("tcp://*:8003");
                new Task(() =>
                {
                    while (replySocket.IsValueCreated)
                    {
                        Thread.Sleep(1000);
                        //接收数据
                        byte[] buffer = replySocket.Value.Receive();
                        string recvstr = Encoding.UTF8.GetString(buffer);
                        TextShow(recvstr);
                        //发送数据
                        replySocket.Value.Send(Encoding.UTF8.GetBytes("已收到" + recvstr));
                    }
                }).Start();
            }
            catch (Exception ex)
            {
                TextShow(ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            switch ((ENanoType)cmbtype.SelectedIndex)
            {
                case ENanoType.BUS:
                    string bussgtr = "这是一条BUS模式消息";
                    busSocket.Value.Send(Encoding.UTF8.GetBytes(bussgtr));
                    TextShow(bussgtr);
                    break;
                case ENanoType.PUBSUB:
                    string pubsubgtr = "这是PUB发送的消息，你收到了吗？";
                    //StringBuilder sb = new StringBuilder();
                    //for(int i = 0; i < 10000; i++)
                    //{
                    //    sb.Append("这是PUB发送的消息" + i);
                    //}
                    //pubsubgtr = sb.ToString();
                    //设置发送的前缀代表主题
                    string prestr = "PUBSUB";
                    publishSocket.Value.Send(Encoding.UTF8.GetBytes(prestr + pubsubgtr));
                    TextShow(pubsubgtr);
                    break;
            }
        }
    }
}
