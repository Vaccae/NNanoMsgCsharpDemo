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

namespace nanomsgclient
{
    public partial class frmClient : Form
    {
        private bool _isconnect = false;

        private Lazy<PairSocket> pairSocket;

        private Lazy<BusSocket> busSocket;

        private Lazy<RequestSocket> requestSocket;

        private Lazy<SubscribeSocket> subscribeSocket;

        private Lazy<RespondentSocket> respondentSocket;

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

        public frmClient()
        {
            InitializeComponent();

            CheckForIllegalCrossThreadCalls = false;

            _tbMsg = tbMsg;

            //加载扩展协议
            cmbtype.Items.Clear();
            foreach (string name in Enum.GetNames(typeof(ENanoType)))
            {
                cmbtype.Items.Add(name);
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                switch ((ENanoType)cmbtype.SelectedIndex)
                {
                    case ENanoType.PAIR:
                        pairSocket = new Lazy<PairSocket>(() => new PairSocket());
                        pairSocket.Value.Connect("tcp://localhost:8001");
                        break;
                    case ENanoType.BUS:
                        busSocket = new Lazy<BusSocket>(() => new BusSocket());
                        busSocket.Value.Bind("tcp://*:8002");
                        busSocket.Value.Connect("tcp://localhost:8002");
                    
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
                        break;
                    case ENanoType.REQREP:
                        requestSocket = new Lazy<RequestSocket>(() => new RequestSocket());
                        requestSocket.Value.Connect("tcp://localhost:8003");
                        break;
                    case ENanoType.PUBSUB:
                        subscribeSocket = new Lazy<SubscribeSocket>(() => new SubscribeSocket());
                        //一定要指定订阅的主题前缀，指定为空订阅所有主题,否则收不到
                        subscribeSocket.Value.Subscribe("PUBSUB");

                        subscribeSocket.Value.Connect("tcp://localhost:8004");

                        new Task(() =>
                        {
                            while (subscribeSocket.IsValueCreated)
                            {
                                Thread.Sleep(1000);
                                //接收数据
                                byte[] buffer = subscribeSocket.Value.Receive();
                                string recvstr = Encoding.UTF8.GetString(buffer);
                                TextShow(recvstr);
                            }
                        }).Start();
                        break;
                    case ENanoType.SURVEY:
                        respondentSocket = new Lazy<RespondentSocket>(() => new RespondentSocket());
                        respondentSocket.Value.Connect("tcp://localhost:8005");
                        new Task(() =>
                        {
                            while (respondentSocket.IsValueCreated)
                            {
                                Thread.Sleep(1000);
                                //接收数据
                                byte[] buffer = respondentSocket.Value.Receive();
                                string recvstr = Encoding.UTF8.GetString(buffer);
                                TextShow(recvstr);
                                //发送数据
                                string sendstr = "已收到!";
                                respondentSocket.Value.Send(Encoding.UTF8.GetBytes(sendstr));
                                TextShow("发送返回数据：" + sendstr);
                            }
                        }).Start();
                        break;
                }
            }
            catch (Exception ex)
            {
                TextShow(ex.Message);
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (tbInput.Text == "")
            {
                TextShow("请输入发送信息");
                return;
            }
            string inputstr = tbInput.Text;
            try
            {
                switch ((ENanoType)cmbtype.SelectedIndex)
                {
                    case ENanoType.PAIR:
                        if (pairSocket.IsValueCreated)
                        {
                            //发送数据
                            pairSocket.Value.Send(Encoding.UTF8.GetBytes(inputstr));
                            TextShow(inputstr);
                            //接收数据
                            byte[] buffer = pairSocket.Value.Receive();
                            TextShow(Encoding.UTF8.GetString(buffer));
                        }
                        else
                        {
                            TextShow("PAIR未创建！");
                        }
                        break;
                    case ENanoType.BUS:
                        if (busSocket.IsValueCreated)
                        {
                            //发送数据
                            busSocket.Value.Send(Encoding.UTF8.GetBytes(inputstr));
                            
                        }
                        break;
                    case ENanoType.REQREP:
                        if (requestSocket.IsValueCreated)
                        {
                            //发送数据
                            requestSocket.Value.Send(Encoding.UTF8.GetBytes(inputstr));

                            //接收数据
                            byte[] buffer = requestSocket.Value.Receive();
                            TextShow("REQREP模式返回数据：" + Encoding.UTF8.GetString(buffer));
                        }
                        else
                        {
                            TextShow("REQREP未创建！");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                TextShow(ex.Message);
            }
        }
    }
}
