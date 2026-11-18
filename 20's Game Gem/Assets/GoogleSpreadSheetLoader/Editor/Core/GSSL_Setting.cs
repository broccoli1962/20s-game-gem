using UnityEditor;
using UnityEngine;
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global

namespace GoogleSpreadSheetLoader.Setting
{
    public class GSSL_Setting
    {
        private static readonly string _settingDataAssetName = "SettingData.asset";
        public static bool AdvanceMode => true;
        public static SettingData SettingData => _settingData;
        private static SettingData _settingData;

        // 시트에서 테이블 내용(헤더 행)이 시작하는 행 번호. 1 = 1행부터, 2 = 2행부터.
        public const int MinTableStartRow = 1;
        public const int MaxTableStartRow = 20;

        /// <summary>
        /// 설정된 테이블 시작 행(1-based). 설정이 없거나 범위를 벗어나면 보정합니다.
        /// </summary>
        public static int TableStartRow
        {
            get
            {
                if (!CheckAndCreate())
                    return MinTableStartRow;

                return Mathf.Clamp(_settingData.tableStartRow, MinTableStartRow, MaxTableStartRow);
            }
        }

        /// <summary>
        /// 헤더 행의 0-based 인덱스. 실제 데이터는 이 다음 행부터 시작합니다.
        /// </summary>
        public static int HeaderRowIndex => TableStartRow - 1;
        

        [InitializeOnLoadMethod]
        private static void ResetStaticInstance()
        {
            _settingData = null;
        }

        public static bool CheckAndCreate()
        {
            var settingDataPath = GSSL_Path.GetPath(ePath.SettingData) + _settingDataAssetName;

            if (_settingData == null)
            {
                // 없으면 파일 생성
                if (!AssetDatabase.AssetPathExists(settingDataPath))
                {
                    var obj = ScriptableObject.CreateInstance<SettingData>();
                    AssetDatabase.CreateAsset(obj, settingDataPath);
                }

                _settingData = AssetDatabase.LoadAssetAtPath<SettingData>(settingDataPath);
            }

            return _settingData != null;
        }
    }
}