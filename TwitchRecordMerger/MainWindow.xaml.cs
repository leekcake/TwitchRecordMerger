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
using TwitchRecordMerger.Container;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TwitchRecordMerger
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<FinishedRecord> finishedRecords = new ObservableCollection<FinishedRecord>();
        private bool InterceptFlag = false;
        private bool InConvert = false;

        public MainWindow()
        {
            InitializeComponent();

            FinishedRecordListBox.ItemsSource = finishedRecords;
        }

        private void LoadRecordTempFolderButton_Click(object sender, RoutedEventArgs e)
        {
            finishedRecords.Clear();
            var paths = Directory.GetDirectories(GlobalStore.RECORD_TEMP_FOLDER);
            Array.Sort(paths);
            foreach (var path in paths)
            {
                if (Path.GetFileName(path).EndsWith("-Finished"))
                {
                    finishedRecords.Add( new FinishedRecord(path) );
                }
            }
        }

        private double ChunkToGB(double chunk)
        {
            return Math.Round(chunk * 100.0 / 1024.0, 2);
        }

        private void StartConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if(InConvert)
            {
                if( MessageBox.Show("현재 작업이 진행중입니다, 중단합니까?", "트위치 방송 머져", MessageBoxButton.YesNo) == MessageBoxResult.Yes )
                {
                    InterceptFlag = true;
                }
                return;
            }
            InterceptFlag = false;
            InConvert = true;
            
            LogTextBox.Text = "";
            LoadRecordTempFolderButton.IsEnabled = false;
            StartConvertButton.Content = "변환 작업 중단";

            int totalChunk = 0;
            foreach(var record in finishedRecords)
            {
                totalChunk += record.TotalChunkCount;
            }

            //상태바 수정
            TotalStatusProgressBar.Minimum = 0;
            TotalStatusProgressBar.Value = 0;
            TotalStatusProgressBar.Maximum = totalChunk;

            TotalChunkProgressTextBlock.Text = $"모든 청크 {TotalStatusProgressBar.Maximum}개({ChunkToGB(TotalStatusProgressBar.Maximum)}GB) 중" +
                $" {TotalStatusProgressBar.Value}개({ChunkToGB(TotalStatusProgressBar.Value)}GB)의 청크가 전달되었습니다.";

            new Task(() =>
            {
                ConvertProcess();
            }).Start();
        }

        private void SetupCurrentStatusProgressBar(FinishedRecord record)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentStatusProgressBar.Minimum = 0;
                CurrentStatusProgressBar.Value = 0;
                CurrentStatusProgressBar.Maximum = record.TotalChunkCount;
            });
        }

        private void OneChunkSent()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentStatusProgressBar.Value += 1;
                TotalStatusProgressBar.Value += 1;
                TotalChunkProgressTextBlock.Text = $"모든 청크 {TotalStatusProgressBar.Maximum}개({ChunkToGB(TotalStatusProgressBar.Maximum)}GB) 중" +
                $" {TotalStatusProgressBar.Value}개({ChunkToGB(TotalStatusProgressBar.Value)}GB)의 청크가 전달되었습니다.";
            });
        }

        private void UpdateCurrentProcess(int pulse, int pulseCurrent, int pulseTotal, int recordCurrent, int recordTotal)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentStatusTextBlock.Text = $"페이즈 {pulse+1}, 현재 페이즈 청크 {pulseCurrent}/{pulseTotal}개, 총 청크 {recordCurrent}/{recordTotal}개 전달됨";
            });
        }

        public async Task ConvertProcess()
        {
            InsertLog("변환 작업이 시작되었습니다");
            int current = 1;
            foreach(var record in finishedRecords)
            {
                if(InterceptFlag)
                {
                    InsertLog("변환 작업의 중단요청이 수락되었습니다.");
                    break;
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TotalStatusTextBlock.Text = $"{record.ToSimpleString()}을 합치고 있습니다... ({current}/{finishedRecords.Count})";
                });
                SetupCurrentStatusProgressBar(record);
                await ConvertProcessWithRecord(record);
                current++;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadRecordTempFolderButton.IsEnabled = true;
                StartConvertButton.Content = "일괄 변환 시작하기";
            });
            InsertLog("변환 작업이 완료되었습니다");
        }

        public async Task ConvertProcessWithRecord(FinishedRecord record)
        {
            InsertLog($"{record.ToSimpleString()}의 변환이 시작되었습니다");
            int recordCurrent = 0;

            var basePath = Path.Combine(GlobalStore.RECORD_OUTPUT_FOLDER, record.NickName);
            for(int pulse = 0; pulse < record.PulseCount; pulse++)
            {
                InsertLog($"{record.ToSimpleString()}의 {pulse} 페이즈 변환이 시작되었습니다");
                var pulseCurrent = 0;
                var pulseTotal = record.GetChunkCountForPulse(pulse);
                UpdateCurrentProcess(pulse, pulseCurrent, pulseTotal, recordCurrent, record.TotalChunkCount);

                var path = Path.Combine(basePath, $"{record.Created}-{pulse}.ts.mp4");

                var ffmpeg = new Process();
                ffmpeg.StartInfo.CreateNoWindow = true;
                ffmpeg.StartInfo.UseShellExecute = false;
                ffmpeg.StartInfo.RedirectStandardInput = true;
                ffmpeg.StartInfo.RedirectStandardError = true;
                ffmpeg.StartInfo.RedirectStandardOutput = true;
                ffmpeg.StartInfo.FileName = "ffmpeg";
                ffmpeg.StartInfo.Arguments = $"-y -analyzeduration {1024 * 1024 * 300} -probesize {1024 * 1024 * 300} -i pipe: -c copy \"{path}\"";
                ffmpeg.OutputDataReceived += Ffmpeg_DataReceived;
                ffmpeg.ErrorDataReceived += Ffmpeg_DataReceived;
                ffmpeg.Start();
                ffmpeg.BeginErrorReadLine();
                ffmpeg.BeginOutputReadLine();

                for(int i = 0; i < pulseTotal; i++)
                {
                    var stream = File.Open(record.GetPathWithPulseAndChunk(pulse, i), FileMode.Open);

                    var buffer = new byte[1024 * 16];
                    while(true)
                    {
                        if(InterceptFlag)
                        {
                            InsertLog("변환 작업의 중단이 감지되었습니다. 현재 작업을 중단하고 있습니다");
                            try
                            {
                                ffmpeg.Kill();
                                ffmpeg.Close();
                                InsertLog("ffmpeg가 강제 중단됨!");
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.ToString());
                            }
                            try
                            {
                                stream.Close();
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.ToString());
                            }
                            
                            try
                            {
                                File.Delete(path);
                                InsertLog("변환중이던 파일이 삭제되었습니다!");
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.ToString());
                            }
                            return;
                        }

                        var readed = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (readed == 0) break;
                        await ffmpeg.StandardInput.BaseStream.WriteAsync(buffer, 0, readed);
                        
                    }
                    pulseCurrent++;
                    recordCurrent++;
                    UpdateCurrentProcess(pulse, pulseCurrent, pulseTotal, recordCurrent, record.TotalChunkCount);
                    OneChunkSent();
                    stream.Close();
                }
                await ffmpeg.StandardInput.BaseStream.FlushAsync();
                ffmpeg.StandardInput.Close();

                ffmpeg.WaitForExit();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FFMpegResultView.Text = "";
                });

                if(ffmpeg.ExitCode != 0) {
                    InsertLog($"{record.ToSimpleString()}의 {pulse}페이즈 변환중 ffmpeg가 ${ffmpeg.ExitCode}를 반환했습니다. 결과물을 확인해주세요! 필요하다면 구글 드라이브에서 복원해야 합니다");
                }
                InsertLog($"{record.ToSimpleString()}의 {pulse}페이즈 변환이 완료되었습니다");
            }

            if(!GlobalStore.READONLY_MODE)
            {
                InsertLog($"{record.ToSimpleString()}의 원본 폴더가 삭제되었습니다");
                var dir = new DirectoryInfo(record.RecordPath);
                dir.Delete(true);
            }
            InsertLog($"{record.ToSimpleString()}의 변환이 종료되었습니다");
        }

        private void InsertLog(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogTextBox.Text += DateTime.Now.ToString() + ": " + text + "\r\n";
                LogTextBox.ScrollToEnd();
            });
        }

        private void Ffmpeg_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(e.Data);
            Application.Current.Dispatcher.Invoke(() =>
            {
                FFMpegResultView.Text += e.Data + "\r\n";
                FFMpegResultView.ScrollToEnd();
            });
        }
    }
}
