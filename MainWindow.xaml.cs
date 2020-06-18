using MahApps.Metro.Controls;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Win32;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SpeechScore
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public WaveIn waveSource = null;
        public WaveFileWriter waveFile = null;
        private string audioFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "pronscore.wav");
        private string referenceText;

        private string subscriptionKey = "input your subscriptionKey"; // Replace this with your subscription key 
        private string region = "eastasia";// Replace this with the region corresponding to your subscription key, e.g. westus, eastasia, centralindia 

       private string language = "en-us";

        int currentOIndex = 0;

        List<String> examEN = new List<string> { "Try out pronunciation assessment", "play tennis piano ride club term board would like well  all that's all worry worry about  teach then" };
        List<String> examCN = new List<string> { "欢迎来到微软技术中心" };

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 开始录音回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveFile != null)
            {
                waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                waveFile.Flush();

            }
        }



        /// <summary>
        /// 录音结束回调函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            if (waveSource != null)
            {
                waveSource.Dispose();
                waveSource = null;
            }

            if (waveFile != null)
            {
                waveFile.Dispose();
                waveFile = null;
            }
        }
        public void selecta(Color l, RichTextBox richTextBox1, int selectLength, TextPointer tpStart, TextPointer tpEnd)
        {
            TextRange range = richTextBox1.Selection;
            range.Select(tpStart, tpEnd);
            //高亮选择         

            range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(l));
            range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            //return tpEnd.GetNextContextPosition(LogicalDirection.Forward);//将指针移到
        }
        /// <summary>
        /// 高亮显示
        /// </summary>
        /// <param name="l">设置颜色,color.FromRgb(rgb的值)</param>
        /// <param name="richBox">richBox</param>
        /// <param name="keyword">需要高亮显示的文字</param>
        /// 

        private TextPointer position = null;
        public void ChangeColor(Color l, RichTextBox richBox, string keyword,float score)
        {
            //设置文字指针为Document初始位置           
            //richBox.Document.FlowDirection     
            if(position == null)
               position = richBox.Document.ContentStart;
            while (position != null)
            {
                //向前搜索,需要内容为Text       
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    //拿出Run的Text        
                    string text = position.GetTextInRun(LogicalDirection.Forward).ToLower();
                    //可能包含多个keyword,做遍历查找           
                    int index = 0;
                    index = text.IndexOf(keyword, 0);
                    if (index != -1)
                    {
                        TextPointer start = position.GetPositionAtOffset(index);
                        TextPointer end = start.GetPositionAtOffset(keyword.Length);
                        TextRange range = richBox.Selection;
                        range.Select(start, end);
                        //高亮         
                        range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(l));
                        range.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

                        Run run = new Run(score+"", end);
                        var scoreRange = new TextRange(end,end.GetPositionAtOffset(5));
                        scoreRange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(l));
                        scoreRange.ApplyPropertyValue(Run.BaselineAlignmentProperty, BaselineAlignment.Superscript); //上标

                        position = end.GetNextContextPosition(LogicalDirection.Forward);//将指针移到//找到了keyword的位置就将指针移到高亮显示的文字的末尾作为下一次搜索的开始
                        break;
                    }

                }
               //文字指针向前偏移   
               position = position.GetNextContextPosition(LogicalDirection.Forward);

            }
        }
        string StringFromRichTextBox(RichTextBox rtb)
        {
            TextRange textRange = new TextRange(rtb.Document.ContentStart,rtb.Document.ContentEnd);
            // The Text property on a TextRange object returns a string
            // representing the plain text content of the TextRange.
            return textRange.Text;
        }

        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            //referenceText =  StringFromRichTextBox(ReferenceText);
            position = null;
            if (string.IsNullOrWhiteSpace(referenceText))
            {
                MessageBox.Show("Reference text cannot be empty！");
                return;
            }

            Run r = new Run(referenceText);
            Paragraph para = new Paragraph();
            para.Inlines.Add(r);
            ReferenceText.Document.Blocks.Clear();
            ReferenceText.Document.Blocks.Add(para);

            position = null;
            StartBut.Visibility = Visibility.Collapsed;
            StopBut.Visibility = Visibility.Visible;
            waveSource = new WaveIn();
            waveSource.WaveFormat = new WaveFormat(16000, 16, 1); // 16bit,16KHz,Mono的录音格式

            waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
            waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);

            waveFile = new WaveFileWriter(audioFile, waveSource.WaveFormat);

            waveSource.StartRecording();
           
            progressRing.IsActive = true;

        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                waveSource.StopRecording();
                // Close Wave(Not needed under synchronous situation)
                if (waveSource != null)
                {
                    waveSource.Dispose();
                    waveSource = null;
                }

                if (waveFile != null)
                {
                    waveFile.Dispose();
                    waveFile = null;
                }

                //var audioFile = @"GoodMorning.wav"; // The audio file in 16khz 16bit PCM format 
                //var referenceText = "Good Morning!";
                var pronScoreParamsJson = $"{{\"ReferenceText\":\"{referenceText}\",\"GradingSystem\":\"HundredMark\",\"Dimension\":\"Comprehensive\",\"EnableMiscue\":\"True\"}}";
                var pronScoreParamsBytes = Encoding.UTF8.GetBytes(pronScoreParamsJson);
                var pronScoreParams = Convert.ToBase64String(pronScoreParamsBytes);
                
                var request = (HttpWebRequest)HttpWebRequest.Create($"https://{region}.stt.speech.microsoft.com/speech/recognition/interactive/cognitiveservices/v1?language={language}&pronunciationScoreParams={pronScoreParams}");
                request.SendChunked = true;
                request.Accept = @"application/json;text/xml";
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                request.ContentType = @"audio/wav; codecs=audio/pcm; samplerate=16000";
                request.Headers["Ocp-Apim-Subscription-Key"] = subscriptionKey;
                request.AllowWriteStreamBuffering = false;
                using (var fs = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
                {     /* 
                   * Open a request stream and write 1024 byte chunks in the stream one at a time. 
                   */
                    byte[] buffer = null;
                    int bytesRead = 0;
                    using (Stream requestStream = request.GetRequestStream())
                    {   /* 
                    * Read 1024 raw bytes from the input audio file
                    */
                        buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            requestStream.Write(buffer, 0, bytesRead);
                        }
                        // Flush         
                        requestStream.Flush();
                    }
                }
                var response = request.GetResponse();
                using (var responseStream = response.GetResponseStream())
                using (var streamReader = new StreamReader(responseStream))
                {
                    var responseJsonText = streamReader.ReadToEnd(); // The result in JSON format, with pronunciation score 

                    ScoreResult result = JsonConvert.DeserializeObject<ScoreResult>(responseJsonText);

                    if (null != result && "Success" == result.RecognitionStatus)
                    {
                        //PronScore.Text = result.NBest[0].PronScore.ToString();
                        //AccuracyScore.Text = result.NBest[0].AccuracyScore.ToString();
                        //FluencyScore.Text = result.NBest[0].FluencyScore.ToString();
                        //CompletionScore.Text = result.NBest[0].CompletionScore.ToString();

                        //generatePronScoreTable();generatePhoneScoreTable();


                        NBestItem nBestItem = result.NBest[0];


                        var pronScore = JsonConvert.SerializeObject(nBestItem);

                        PornScoreWebBrowser.InvokeScript("generatePronScoreTable", pronScore);
                        PornScoreWebBrowser.InvokeScript("generatePhoneScoreTable", pronScore);
                        PornScoreWebBrowser.Visibility = Visibility.Visible;
                        List<WordsItem> witems = result.NBest[0].Words;
                        for (int i = 0; i < witems.Count; i++)
                        {
                            WordsItem w = witems[i];
                            if (w.AccuracyScore <= 60.0)
                            {
                                ChangeColor(Colors.Red, ReferenceText, w.Word, w.AccuracyScore);
                            }
                            else if (w.AccuracyScore <= 70.0)
                            {
                                ChangeColor(Colors.Orange, ReferenceText, w.Word, w.AccuracyScore);
                            }
                            else
                            {
                                ChangeColor(Colors.Green, ReferenceText, w.Word, w.AccuracyScore);
                            }
                        }

                       

                    }
                    else {
                        if (null != result)
                            MessageBox.Show(result.RecognitionStatus);

                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally {
                progressRing.IsActive = false;
                StartBut.Visibility = Visibility.Visible;
                StopBut.Visibility = Visibility.Collapsed;
            }


        }


        private void ClearBut_Click(object sender, RoutedEventArgs e)
        {
            ReferenceText.Document.Blocks.Clear();
            referenceText = string.Empty;
        }

        private async void TTSBut_Click(object sender, RoutedEventArgs e)
        {
            //if (string.IsNullOrWhiteSpace(referenceText)) {
            //    referenceText = StringFromRichTextBox(ReferenceText);
            //}
            if (string.IsNullOrWhiteSpace(referenceText))
                return;
            var config = SpeechConfig.FromSubscription(subscriptionKey, region);
            //config.SpeechSynthesisVoiceName = "zh-TW-Yating-Apollo";

            using (var synthesizer = new SpeechSynthesizer(config))
            {
               
                using (var result = await synthesizer.SpeakTextAsync(referenceText))
                {
                    if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                        MessageBox.Show($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                           MessageBox.Show($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                        }
                    }
                }
            }

        }

        /// <summary>  
        /// 修改注册表信息来兼容当前程序  
        ///   
        /// </summary>  
        static void SetWebBrowserFeatures(int ieVersion)
        {
            // don't change the registry if running in-proc inside Visual Studio  
            if (LicenseManager.UsageMode != LicenseUsageMode.Runtime)
                return;
            //获取程序及名称  
            var appName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            //得到浏览器的模式的值  
            UInt32 ieMode = GeoEmulationModee(ieVersion);
            var featureControlRegKey = @"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\";
            //设置浏览器对应用程序（appName）以什么模式（ieMode）运行  
            Registry.SetValue(featureControlRegKey + "FEATURE_BROWSER_EMULATION",
                appName, ieMode, RegistryValueKind.DWord);
            // enable the features which are "On" for the full Internet Explorer browser  
            Registry.SetValue(featureControlRegKey + "FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION",
                appName, 1, RegistryValueKind.DWord);


        }
        /// <summary>  
        /// 获取浏览器的版本  
        /// </summary>  
        /// <returns></returns>  
        static int GetBrowserVersion()
        {
            int browserVersion = 0;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree,
                System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion");
                if (null == version)
                {
                    version = ieKey.GetValue("Version");
                    if (null == version)
                        throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }
            //如果小于7  
            if (browserVersion < 7)
            {
                throw new ApplicationException("不支持的浏览器版本!");
            }
            return browserVersion;
        }
        /// <summary>  
        /// 通过版本得到浏览器模式的值  
        /// </summary>  
        /// <param name="browserVersion"></param>  
        /// <returns></returns>  
        static UInt32 GeoEmulationModee(int browserVersion)
        {
            UInt32 mode = 11000; // Internet Explorer 11. Webpages containing standards-based !DOCTYPE directives are displayed in IE11 Standards mode.   
            switch (browserVersion)
            {
                case 7:
                    mode = 7000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE7 Standards mode.   
                    break;
                case 8:
                    mode = 8000; // Webpages containing standards-based !DOCTYPE directives are displayed in IE8 mode.   
                    break;
                case 9:
                    mode = 9000; // Internet Explorer 9. Webpages containing standards-based !DOCTYPE directives are displayed in IE9 mode.                      
                    break;
                case 10:
                    mode = 10000; // Internet Explorer 10.  
                    break;
                case 11:
                    mode = 11000; // Internet Explorer 11  
                    break;
            }
            return mode;
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {

                SetWebBrowserFeatures(10);
                LoadExam();
                PornScoreWebBrowser.Navigate(new Uri(System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, @"pornscore.html")));
            }
            catch {
            }
        }

       
        private void LoadExam() {

            if("zh-cn".Equals(language))
                referenceText = examCN[0];
            else
                referenceText = examEN[0];

            Run r = new Run(referenceText);
            Paragraph para = new Paragraph();
            para.Inlines.Add(r);
            ReferenceText.Document.Blocks.Clear();
            ReferenceText.Document.Blocks.Add(para);
        }


       

        private void PrevBut_Click(object sender, RoutedEventArgs e)
        {
            if ("zh-cn".Equals(language) && examCN.Count > 0)
                referenceText = examCN[currentOIndex > 0 ? --currentOIndex : 0];
            else
                referenceText = examEN[currentOIndex > 0 ? --currentOIndex : 0];

            Run r = new Run(referenceText);
            Paragraph para = new Paragraph();
            para.Inlines.Add(r);
            ReferenceText.Document.Blocks.Clear();
            ReferenceText.Document.Blocks.Add(para);

            PornScoreWebBrowser.Visibility = Visibility.Collapsed;
        }

        private void NextPro_Click(object sender, RoutedEventArgs e)
        {
            if ("zh-cn".Equals(language) && examCN.Count > 0)
                referenceText = examCN[currentOIndex < (examCN.Count - 1) ? ++currentOIndex : (examCN.Count - 1)];
            else
                referenceText = examEN[currentOIndex < (examEN.Count - 1) ? ++currentOIndex : (examEN.Count - 1)];

                Run r = new Run(referenceText);
                Paragraph para = new Paragraph();
                para.Inlines.Add(r);
                ReferenceText.Document.Blocks.Clear();
                ReferenceText.Document.Blocks.Add(para);

                PornScoreWebBrowser.Visibility = Visibility.Collapsed;
        }

        private void LanguageBut_Click(object sender, RoutedEventArgs e)
        {
            if ("Chinese".Equals(LanguageBut.Content))
            {
                language = "zh-cn";
                LanguageBut.Content = "English";
                currentOIndex = 0;
            }
            else {
                language = "en-us";
                LanguageBut.Content = "Chinese";
                currentOIndex = 0;
            }

            LoadExam();
        }
    }
}
