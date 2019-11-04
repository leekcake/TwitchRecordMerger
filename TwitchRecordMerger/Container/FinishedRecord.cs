using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TwitchRecordMerger.Container
{
    public class FinishedRecord
    {
        /// <summary>
        /// 레코딩 파일이 저장되어 있는 경로
        /// </summary>
        public string RecordPath;

        /// <summary>
        /// 녹화대상의 아이디 (lilac_unicorn_)
        /// </summary>
        public string Id;
        /// <summary>
        /// 녹화대상의 닉네임 (유닉혼)
        /// </summary>
        public string NickName;

        /// <summary>
        /// 이 녹화본이 가지고 있는 총 페이즈
        /// </summary>
        public int PulseCount;

        /// <summary>
        /// 생성된 일자 정보
        /// </summary>
        public string Created;

        /// <summary>
        /// 이 녹화본이 가지고 있는 모든 청크의 갯수, 버려진 청크는 제외됩니다.
        /// </summary>
        public int TotalChunkCount;

        /// <summary>
        /// 레코드 시스템의 문제로 인해 마지막 페이즈에 호스팅이 남는 경우가 있음
        /// 이 경우 그 페이즈는 3개 이상의 파일이 남지 않으므로, 이를 플래그 삼아 혹시 모를 상황에 대비
        /// </summary>
        public bool LastPulseIsTooShort = false;

        /// <summary>
        /// 마지막 페이즈가 (짧아서) 무시되었는지의 여부를 반환합니다
        /// </summary>
        public bool LastPulseIgnored = false;

        /// <summary>
        /// 페이즈가 무시되면서 버려진 청크의 갯수입니다.
        /// </summary>
        public int DroppedChunkCount = 0;

        public FinishedRecord(string path)
        {
            RecordPath = path;

            var id = Path.GetFileName(path);
            //lilac_unicorn__20191031_210615-Finished

            var splited = id.Split('_');
            Created = splited[splited.Length - 2] + "_" + splited[splited.Length - 1];
            //20191031_210615-Finished
            Created = Created.Replace("-Finished", "");
            //20191031_210615

            id = id.Substring(0, id.LastIndexOf('_'));
            //lilac_unicorn__20191031
            id = id.Substring(0, id.LastIndexOf('_'));
            //lilac_unicorn_

            Id = id;
            NickName = GlobalStore.RECORD_ID_TO_NICKNAME[id];

            for(int i = 0; i < int.MaxValue; i++)
            {
                if (!File.Exists(Path.Combine(path, $"{id}_{Created}-{i}.0")))
                {
                    if(i == 0)
                    {
                        //????????
                        throw new ArgumentException("No valid record file on provided path? corrupted or bad directory?");
                    }
                    PulseCount = i;
                    break;
                }
            }

            TotalChunkCount = Directory.GetFiles(path).Length;

            LastPulseIsTooShort = !File.Exists(Path.Combine(path, $"{id}_{Created}-{PulseCount-1}.3"));

            if(LastPulseIsTooShort && GlobalStore.IgnoreLastShortPulse)
            {
                LastPulseIgnored = true;
                PulseCount -= 1;
                var save = TotalChunkCount;
                for (int i = 0; i < int.MaxValue; i++)
                {
                    if (!File.Exists(Path.Combine(path, $"{id}_{Created}-{PulseCount}.{i}")))
                    {
                        break;
                    }
                    TotalChunkCount--;
                }
                DroppedChunkCount = save - TotalChunkCount;
            }
        }

        public int GetChunkCountForPulse(int pulse)
        {
            var count = 0;
            for (int i = 0; i < int.MaxValue; i++)
            {
                if (!File.Exists( GetPathWithPulseAndChunk(pulse, i) ))
                {
                    break;
                }
                count++;
            }
            return count;
        }

        public string GetPathWithPulseAndChunk(int pulse, int chunk)
        {
            return Path.Combine(RecordPath, $"{Id}_{Created}-{pulse}.{chunk}");
        }

        private string LastPulseIsTooShortString
        {
            get
            {
                if( !LastPulseIsTooShort )
                {
                    return "";
                }
                if( LastPulseIgnored )
                {
                    return $" (마지막 페이즈가 무시됨, {DroppedChunkCount}개의 청크가 버려집니다)";
                }
                return " (마지막 페이즈가 짧음)";
            }
        }

        public string ToSimpleString()
        {
            return $"{NickName}님의 {Created}에 시작된 방송";
        }

        public override string ToString()
        {
            return $"{NickName}님의 {Created}에 시작된 방송, {PulseCount}개의 페이즈, 총 {TotalChunkCount}개의 청크{LastPulseIsTooShortString}";
        }
    }
}
