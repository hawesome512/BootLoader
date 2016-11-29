using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Configuration;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BootLoader
{
        /// <summary>
        /// MainWindow.xaml 的交互逻辑
        /// </summary>
        public partial class MainWindow : Window
        {
                SerialPort sp;
                CancellationTokenSource cancelToken;
                string strPort;
                int nBaud;
                int nInterval;
                int nForm;
                int nJoin;
                public MainWindow()
                {
                        InitializeComponent();
                        checkUpdate();
                        initControls();
                }

                ~MainWindow()
                {
                        if (sp != null)
                        {
                                sp.Dispose();
                        }
                }

                private void initControls()
                {
                        //导入上次通信参数，若没有置空
                        for (int i = 50; i < 100; i += 10)
                        {
                                CBox_Interval.Items.Add(i.ToString());
                        }
                        for (int i = 100; i <= 1000; i += 50)
                        {
                                CBox_Interval.Items.Add(i.ToString());
                        }
                        CBox_Interval.SelectedIndex = CBox_Interval.Items.IndexOf(GetConfig("Interval"));
                        List<string> bauds = new List<string> { "9600", "19200", "33600", "41667" };
                        foreach (var b in bauds)
                        {
                                CBox_Baud.Items.Add(b);
                        }
                        CBox_Baud.SelectedIndex = CBox_Baud.Items.IndexOf(GetConfig("Baud"));
                        var ports = System.IO.Ports.SerialPort.GetPortNames().ToList();
                        foreach (string p in ports)
                        {
                                CBox_Port.Items.Add(p);
                        }
                        CBox_Port.SelectedIndex = ports.IndexOf(GetConfig("COM"));
                        List<string> forms = new List<string> { "原始数据", "AA,AA,Length(n) ~n~ 55,55", "AA,AA,Length(n+1) ~n~ 55,55" };
                        foreach (string f in forms)
                        {
                                CBox_Form.Items.Add(f);
                        }
                        int form;
                        if (int.TryParse(GetConfig("Form"), out form))
                        {
                                CBox_Form.SelectedIndex = form;
                        }
                        for (int i = 1; i <= 10; i++)
                        {
                                CBox_Join.Items.Add(i.ToString());
                        }
                        CBox_Join.SelectedIndex = CBox_Join.Items.IndexOf(GetConfig("Join"));
                }

                private void btn_close_Click(object sender, RoutedEventArgs e)
                {
                        //保存通信设置参数，下次启动用之初始化
                        if ((strPort != null) || (nInterval * nBaud != 0))
                        {
                                SetConfig("COM", strPort);
                                SetConfig("Interval", nInterval.ToString());
                                SetConfig("Baud", nBaud.ToString());
                                SetConfig("Form", nForm.ToString());
                                SetConfig("Join", nJoin.ToString());
                        }
                        this.Close();
                }

                private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
                {
                        this.DragMove();
                }

                #region 自动更新
                void checkUpdate()
                {
                        Task.Factory.StartNew(new Action(() =>
                        {
                                Version version = getVersion(@"http://172.16.65.88:7072/BootLoader/BootLoader最新版本号.txt");
                                Version now = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                        if (version != null && version > now)
                                        {
                                                var result = MsgBox.Show(string.Format("发现可用更新，新版本为:{0}\r\n是否现在更新？", version.ToString()), "更新", MsgBox.Buttons.YesNo, MsgBox.Icons.Question);
                                                if (result == System.Windows.Forms.DialogResult.Yes)
                                                {
                                                        update(version.ToString());
                                                }
                                        }
                                }));
                        }));
                }

                void update(string version)
                {
                        string address = @"http://172.16.65.88:7072/BootLoader";
                        string dir = AppDomain.CurrentDomain.BaseDirectory;
                        if (dir.Contains(" "))
                        {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                        MsgBox.Show(string.Format("软件地址:{0}中存在空格，请删除空格后重试！", dir), "更新失败", MsgBox.Buttons.OK, MsgBox.Icons.Error);
                                }));
                        }
                        else
                        {
                                address = string.Format(@"{0}/BootLoader_{1}.rar", address, version);
                                string args = string.Format("{0} {1}", address, AppDomain.CurrentDomain.BaseDirectory);
                                System.Diagnostics.Process.Start(@"Update\Update.exe", args);
                        }
                        this.Close();
                }

                private static Version getVersion(string url)
                {
                        WebClient client = new WebClient();
                        try
                        {
                                string v = Encoding.ASCII.GetString(client.DownloadData(url));
                                return new Version(v);
                        }
                        catch
                        {
                                return null;
                        }
                }
                #endregion

                /// <summary>
                /// 读配置参数
                /// </summary>
                /// <param name="key"></param>
                /// <returns></returns>
                public string GetConfig(string key)
                {
                        return ConfigurationManager.AppSettings[key];
                }

                /// <summary>
                /// 写配置参数
                /// </summary>
                /// <param name="key"></param>
                /// <param name="value"></param>
                public void SetConfig(string key, string value)
                {
                        Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        cfa.AppSettings.Settings[key].Value = value;
                        cfa.Save();
                }

                /// <summary>
                /// 操作代码
                /// </summary>
                /// <param name="sender"></param>
                /// <param name="e"></param>
                private void ImageButton_Click(object sender, RoutedEventArgs e)
                {
                        if (Load_Button.Content.ToString() == "导入文件")
                        {
                                try
                                {
                                        strPort = CBox_Port.SelectedValue.ToString();
                                        nInterval = int.Parse(CBox_Interval.SelectedValue.ToString());
                                        nBaud = int.Parse(CBox_Baud.SelectedValue.ToString());
                                        nForm = CBox_Form.SelectedIndex;
                                        nJoin = int.Parse(CBox_Join.SelectedValue.ToString());
                                }
                                catch
                                {
                                        State_Text.Text = "先设置参数";
                                        return;
                                }
                                List<byte[]> data = loadFile();
                                //应对OpenFileDialog后【取消】的情况
                                if (data == null)
                                {
                                        return;
                                }
                                cvtData(ref data);
                                Load_Button.Content = "中断导入";
                                //耗时估算：+10第一条等待时间，*1.2考虑通信失败重试的裕度
                                int nWaitTime = (int)(nInterval / 1000f * data.Count * 1.2) + 10;
                                State_Text.Text += string.Format("共{0}条指令，开始下载，预计耗时：{1}m{2}s({3})\r\n进度：0/{4}\r\n", data.Count, nWaitTime / 60, nWaitTime % 60, DateTime.Now.AddSeconds(nWaitTime).ToShortTimeString(), data.Count);
                                Task.Factory.StartNew(new Action(() =>
                                {
                                        cancelToken = new CancellationTokenSource();
                                        ParallelOptions option = new ParallelOptions();
                                        option.CancellationToken = cancelToken.Token;
                                        DateTime dateTime = DateTime.Now;
                                        for (int i = 0; i < data.Count; i++)
                                        {
                                                option.CancellationToken.ThrowIfCancellationRequested();
                                                //第一条指令下位机需擦除寄存器，等待10s
                                                int interval = i == 0 ? 10000 : nInterval;
                                                //通信成功，下位机标志位回复OK[O:79];通信失败，回复NG[N:78]
                                                byte result = 78;
                                                //通信失败，最多可以重复尝试5此，每次等待时间递增n*nInterval,保证成功率
                                                for (int j = 1; j <= 5; j++)
                                                {
                                                        result = sendData(data[i], j*interval);
                                                        //Thread.Sleep(interval);
                                                        //result = 79;
                                                        if (result == 79)
                                                        {
                                                                break;
                                                        }
                                                }
                                                this.Dispatcher.Invoke(new Action(() =>
                                                {
                                                        if (result != 79)
                                                        {
                                                                State_Text.Text += string.Format("导入第{0}条指令失败\r\n", i + 1);
                                                                State_Text.Text += BitConverter.ToString(data[i]) + "\r\n";
                                                                stopLoad(false);
                                                        }
                                                        else
                                                        {
                                                                //处理异常：中断导入后，进度条不会清0
                                                                //因为执行stopLoad后,导入线程要在下个循环的开始时才判断
                                                                if (!cancelToken.IsCancellationRequested)
                                                                {
                                                                        State_Progress.Value = (i + 1) * 100.0 / data.Count;
                                                                }
                                                                State_Text.Text = State_Text.Text.Replace(string.Format("进度：{0}/{1}", i, data.Count), string.Format("进度：{0}/{1}", i + 1, data.Count));
                                                                if (i == data.Count - 1)
                                                                {
                                                                        stopLoad(true);
                                                                        TimeSpan useTime = dateTime - DateTime.Now;
                                                                        State_Text.Text += string.Format("实际耗时：{0:hh\\:mm\\:ss}\r\n", useTime);
                                                                }
                                                        }
                                                }));
                                                if (result != 79)
                                                {
                                                        break;
                                                }
                                        }
                                }));
                        }
                        else
                        {
                                stopLoad(false);
                        }
                }

                /// <summary>
                /// 停止通信
                /// </summary>
                /// <param name="success">true:通信完成停止,false:通信中断</param>
                private void stopLoad(bool success)
                {
                        cancelToken.Cancel();
                        if (sp != null)
                        {
                                sp.Dispose();
                        }
                        State_Text.Text += success ? "导入完成" : "中断导入";
                        State_Progress.Value = 0;
                        Load_Button.Content = "导入文件";
                }

                /// <summary>
                /// 发送数据
                /// </summary>
                /// <param name="snd">数据包</param>
                /// <param name="interval">间隔时间</param>
                /// <returns>通信状态标志位</returns>
                private byte sendData(byte[] snd, int interval)
                {
                        byte[] rcv = new byte[255];
                        try
                        {
                                if (sp == null || !sp.IsOpen)
                                {
                                        sp = new SerialPort(strPort);
                                        sp.BaudRate = nBaud;
                                        sp.WriteTimeout = 1000;
                                        sp.ReadTimeout = 1000;
                                        sp.Open();
                                }
                                sp.Write(snd, 0, snd.Length);
                                Thread.Sleep(interval);
                                sp.Read(rcv, 0, rcv.Length);
                        }
                        catch
                        {
                        }
                        //数据包加AA AA Length后，标志位在3
                        return nForm == 0 ? rcv[0] : rcv[3];
                }

                private List<byte[]> loadFile()
                {
                        List<byte[]> source = new List<byte[]>();
                        System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
                        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                                FileInfo fi = new FileInfo(ofd.FileName);
                                State_Text.Text = string.Format("【{0}】\r\n", ofd.SafeFileName);
                                byte[] sendByte;
                                using (FileStream fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                                {
                                        int fsLen = (int)fs.Length;
                                        sendByte = new byte[fsLen];
                                        fs.Read(sendByte, 0, sendByte.Length);
                                        Encoding ascii = Encoding.ASCII;
                                        string target = ascii.GetString(sendByte);
                                        //每行指令以S开头[S:83]
                                        char[] splitter = ascii.GetChars(new byte[] { 83 });
                                        string[] result = target.Split(splitter);
                                        for (int i = 1; i < result.Length; i++)
                                        {
                                                result[i] = "S" + result[i];//在每一条S19文件开头加‘S’
                                                result[i] = result[i].Replace("\r", string.Empty).Replace("\n", string.Empty);
                                        }

                                        foreach (var str in result)
                                        {
                                                byte[] tmps = ascii.GetBytes(str);
                                                if (tmps.Length == 0)
                                                {
                                                        continue;
                                                }
                                                //0,1位不变(S+Number),以后每位为0-9A-F,16进制位，可以两个合并成一个byte压缩数据长度，减少发送时间
                                                byte[] joinTmps = new byte[(tmps.Length - 2) / 2 + 2];
                                                joinTmps[0] = tmps[0];
                                                joinTmps[1] = (byte)(A2I(tmps[1]));
                                                for (int i = 2; i < tmps.Length; i += 2)
                                                {
                                                        joinTmps[1 + i / 2] = (byte)(A2I(tmps[i]) * 16 + A2I(tmps[i + 1]));
                                                }
                                                source.Add(joinTmps.ToArray());
                                        }
                                }
                        }
                        else
                        {
                                return null;
                        }
                        //文件中第一行指令数据长度都非常大，可以只用固定指令通知下位机开始导入程序
                        //source[0] = new byte[] { 0x53, 0x00, 0x03, 0x00, 0x00, 0xFC };
                        source[source.Count - 1] = new byte[] { 0x53,0x09,0x05,0x00,0x00,0x00,0x00,0xFA};
                        List<byte[]> data = new List<byte[]>();
                        //data.Add(new byte[] { 0x53, 0x00, 0x03, 0x00, 0x00, 0xFC });
                        data.Add(new byte[] { 0x53, 0x00, 0x05,0x00,0x00, 0x00, 0x00, 0xFA });
                        for (int i = 1; i < source.Count; i += nJoin)
                        {
                                byte[] join = source[i];
                                for (int j = 1; j < nJoin && (i + j < source.Count); j++)
                                {
                                        join = join.Concat(source[i + j]).ToArray();
                                }
                                data.Add(join);
                        }
                        return data;
                        //return source;
                }

                private void cvtData(ref List<byte[]> data)
                {
                        //nForm:
                        //0:原始，不处理
                        //1:加AA AA Length~……~55 55→Length不包括自身长度
                        //2:加AA AA Length~……~55 55→Length包括自身长度
                        //多条一起发送时,Length固定为1
                        if (nForm == 0)
                        {
                                return;
                        }
                        byte[] start = new byte[] { 0xAA, 0xAA, 0x0 };
                        byte[] end = new byte[] { 0x55, 0x55 };
                        for (int i = 0; i < data.Count; i++)
                        {
                                start[2] = nJoin > 1 ? (byte)1 : (nForm == 1 ? (byte)data[i].Length : (byte)(data[i].Length + 1));
                                data[i] = start.Concat(data[i]).Concat(end).ToArray();
                        }
                }

                /// <summary>
                /// ASCII码转Int
                /// </summary>
                /// <param name="source"></param>
                /// <returns></returns>
                private int A2I(byte source)
                {
                        //0~9:48~57
                        //A~F:65~70
                        if (source >= 48 && source <= 57)
                        {
                                return source - 48;
                        }
                        else if (source >= 65 && source <= 70)
                        {
                                return source - 55;
                        }
                        else
                        {
                                return -1;
                        }
                }
        }
}
