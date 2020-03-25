using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchRecordMerger.Container
{
    public class GlobalStore
    {
        public const string RECORD_TEMP_FOLDER = "E:\\내 드라이브\\트위치 레코드 임시 저장공간";
        public const string RECORD_OUTPUT_FOLDER = "E:\\내 드라이브\\트위치 다시보기";
        //public const string RECORD_OUTPUT_FOLDER = "D:\\영상\\테스트";
        public const bool READONLY_MODE = false; //작업이 완료되더라도 원본을 지우지 않는지의 여부

        public readonly static Dictionary<string, string> RECORD_ID_TO_NICKNAME;

        /// <summary>
        /// 마지막 페이즈가 짧은경우, 무시할지의 여부를 결정합니다
        /// </summary>
        public static bool IgnoreLastShortPulse = true;

        static GlobalStore()
        {
            RECORD_ID_TO_NICKNAME = new Dictionary<string, string>();

            RECORD_ID_TO_NICKNAME["lilac_unicorn_"] = "유닉혼";
            RECORD_ID_TO_NICKNAME["nopetori"] = "노페토리";
            RECORD_ID_TO_NICKNAME["dohwa_0904"] = "도화님";
            RECORD_ID_TO_NICKNAME["jiamang99"] = "쟈망";
            RECORD_ID_TO_NICKNAME["pocari_on"] = "뽀카링";
            RECORD_ID_TO_NICKNAME["e_yeon"] = "이연순";
            RECORD_ID_TO_NICKNAME["roong__"] = "빵룽";
            RECORD_ID_TO_NICKNAME["godevil_09"] = "고악마";
            RECORD_ID_TO_NICKNAME["layered20"] = "이렛";
            RECORD_ID_TO_NICKNAME["andyandy77"] = "바사삭하군요";
            RECORD_ID_TO_NICKNAME["rupin074"] = "뿅아가";
            RECORD_ID_TO_NICKNAME["myosonge"] = "묘송이";
            RECORD_ID_TO_NICKNAME["qorgid108"] = "수백향";
            RECORD_ID_TO_NICKNAME["kimina98"] = "이냐냐";
            RECORD_ID_TO_NICKNAME["flower_soom__"] = "꽃솜";
            RECORD_ID_TO_NICKNAME["pyoleem1"] = "표림";
            RECORD_ID_TO_NICKNAME["jingbear_"] = "징곰";
        }
    }
}
