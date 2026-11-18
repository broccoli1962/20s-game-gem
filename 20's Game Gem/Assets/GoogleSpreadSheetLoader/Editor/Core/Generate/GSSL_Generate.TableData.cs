using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GoogleSpreadSheetLoader.Setting;
using Newtonsoft.Json;
using TableData;
using UnityEditor;
using UnityEngine;

namespace GoogleSpreadSheetLoader.Generate
{
    public static partial class GSSL_Generate
    {
        public static void GenerateTableScripts(List<SheetData> sheets)
        {
            var tableScriptSavePath = GSSL_Path.GetPath(ePath.TableScript);
            var dataScriptSavePath = GSSL_Path.GetPath(ePath.DataScript);
            
            foreach (var sheet in sheets)
            {
                var tableTitle = sheet.title;
                var dataClassName = tableTitle + "Data";
                var tableClassName = tableTitle + "Table";
                var dataFilePath = dataScriptSavePath + dataClassName + ".cs";
                var tableFilePath = tableScriptSavePath + tableClassName + ".cs";

                var variableDeclarations = new List<string>();

                var sheetRows = JsonConvert.DeserializeObject<List<List<string>>>(sheet.data);

                var headerRowIndex = GSSL_Setting.HeaderRowIndex;
                if (sheetRows == null || sheetRows.Count <= headerRowIndex + 1) continue;

                var setData = "";
                var headers = sheetRows[headerRowIndex];
                var usedFieldNames = new Dictionary<string, string>();
                var namingErrors = new List<string>();
                for (var i = 0; i < headers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(headers[i]) || !headers[i].Contains("-"))
                        continue;

                    var splitHeader = headers[i].Split('-');
                    if (splitHeader.Length < 2) continue;

                    var columnName = splitHeader[0].Trim();
                    // private 은 _snake_case, public 은 PascalCase. 시트 표기가 무엇이든 여기서 통일한다.
                    var varName = ToSnakeCase(columnName);
                    var propName = ToPascalCase(varName);
                    var varType = ConvertToCSharpType(splitHeader[1].Trim());

                    if (!ValidateFieldName(columnName, varName, propName, dataClassName, usedFieldNames, out var namingError))
                    {
                        namingErrors.Add(namingError);
                        continue;
                    }

                    variableDeclarations.Add($"    public {varType} {propName} => _{varName};\n");
                    variableDeclarations.Add($"    [SerializeField] private {varType} _{varName};\n\n");

                    if (varType == "string")
                    {
                        // 문자열의 경우, 셀이 비어있거나 행의 길이가 짧아도 안전하게 빈 문자열로 처리
                        setData += $"\t\t_{varName} = data.Count > {i} ? data[{i}] : string.Empty;\n";
                    }
                    else if (CheckEnumType(varType))
                    {
                        // Enum 및 기타 타입은 인덱스 범위와 빈 문자열을 체크한 뒤에만 파싱
                        setData += $"\t\tif (data.Count > {i} && !string.IsNullOrEmpty(data[{i}]))\n";
                        setData += "\t\t{\n";
                        setData += $"\t\t\t_{varName} = {varType}.Parse<{varType}>(data[{i}]);\n";
                        setData += "\t\t}\n";
                    }
                    else
                    {
                        // 숫자/기타 기본형도 인덱스와 빈 문자열을 체크해서 안전하게 처리
                        setData += $"\t\tif (data.Count > {i} && !string.IsNullOrEmpty(data[{i}]))\n";
                        setData += "\t\t{\n";
                        setData += $"\t\t\t_{varName} = {varType}.Parse(data[{i}]);\n";
                        setData += "\t\t}\n";
                    }
                }

                // 이름이 하나라도 깨지면 반쪽짜리 클래스를 쓰지 않고 해당 시트를 통째로 건너뛴다
                if (namingErrors.Count > 0)
                {
                    Debug.LogError($"[GSSL_Generate] {tableTitle} 시트의 컬럼명 정규화 실패로 생성을 건너뜁니다.\n - "
                                   + string.Join("\n - ", namingErrors));
                    continue;
                }

                setData = "\tpublic void SetData(List<string> data)\n\t{\n" + $"{setData}" + "\t}\n";

                var dataClassTemplate = $"using System;\n"
                                           + $"using System.Collections.Generic;\n"
                                           + "using TableData;\n"
                                           + "using UnityEngine;\n"
                                           + "\n"
                                           + "[Serializable]\n"
                                           + $"public partial class {dataClassName} : {nameof(IData)}\n{{\n"
                                           + string.Join("", variableDeclarations)
                                           + string.Join("", setData)
                                           + "}\n";

                File.WriteAllText(dataFilePath, dataClassTemplate);

                setData = "\tpublic void SetData(List<List<string>> data)\n\t{\n"
                          + $"\t\tdataList = new List<{dataClassName}>();\n"
                          + $"\t\tforeach (var item in data)\n"
                          + $"\t\t{{\n\t\t\t{dataClassName} newData = new();\n"
                          + $"\t\t\tnewData.SetData(item);\n"
                          + $"\t\t\tdataList.Add(newData);\n\t\t}}"
                          + "\n\t}\n";

                var tableTemplate = $"using System.Collections.Generic;\n"
                                       + "using TableData;\n"
                                       + "using UnityEngine;\n"
                                       + "\n"
                                       + $"[CreateAssetMenu(fileName = \"{tableClassName}\", menuName = \"Tables/{tableClassName}\")]\n"
                                       + $"public class {tableClassName} : ScriptableObject, {nameof(ITable)}\n{{\n"
                                       + $"    public List<{dataClassName}> dataList = new List<{dataClassName}>();\n\n"
                                       + string.Join("", setData)
                                       + "}\n";

                File.WriteAllText(tableFilePath, tableTemplate);
            }
        }

        public static void GenerateTableData(List<SheetData> listSheet)
        {
            var dataSavePath = GSSL_Path.GetPath(ePath.TableData);
            foreach (var sheet in listSheet)
            {
                var tableAssetPath = dataSavePath + sheet.title + "Table.asset";
                var tableClassName = sheet.title + "Table";

                var sheetRows = JsonConvert.DeserializeObject<List<List<string>>>(sheet.data);

                var headerRowIndex = GSSL_Setting.HeaderRowIndex;
                if (sheetRows == null || sheetRows.Count <= headerRowIndex + 1) continue;

                var tableType = FindTypeByName(tableClassName);
                if (tableType == null)
                {
                    Debug.LogError($"Failed to find type: {tableClassName}");
                    continue;
                }
                
                // 기존 파일이 있으면 삭제
                if (AssetDatabase.AssetPathExists(tableAssetPath))
                {
                    AssetDatabase.DeleteAsset(tableAssetPath);
                }
                
                var tableAsset = ScriptableObject.CreateInstance(tableType);
                if (tableAsset == null)
                {
                    Debug.LogError($"Failed to create instance of {tableClassName}");
                    continue;
                }

                sheetRows.RemoveRange(0, headerRowIndex + 1);

                ((ITable)tableAsset).SetData(sheetRows);

                AssetDatabase.CreateAsset(tableAsset, tableAssetPath);
            }
        }

        private static string ConvertToCSharpType(string type)
        {
            return type switch
            {
                "int" => "int",
                "float" => "float",
                "bool" => "bool",
                "long" => "long",
                "double" => "double",
                "string" => "string",
                
                _ => type,
            };
        }
        
        private static bool CheckEnumType(string type)
        {
            return type switch
            {
                "int" => false,
                "float" => false,
                "bool" => false,
                "long" => false,
                "double" => false,
                "string" => false,
                
                _ => true,
            };
        }

        /// <summary>
        /// 시트 컬럼명을 snake_case로 정규화한다.
        /// 약어 예외는 두지 않는다 (ID -> id, HP -> hp, ItemID -> item_id).
        /// </summary>
        private static string ToSnakeCase(string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName)) return string.Empty;

            var trimmed = columnName.Trim();
            var builder = new StringBuilder(trimmed.Length + 4);

            for (var i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];

                if (c == '_' || c == ' ' || c == '-')
                {
                    AppendSeparator(builder);
                    continue;
                }

                if (char.IsUpper(c) && builder.Length > 0)
                {
                    var prev = trimmed[i - 1];
                    var nextIsLower = i + 1 < trimmed.Length && char.IsLower(trimmed[i + 1]);

                    // lower -> Upper 경계, 그리고 연속 대문자 뒤에 소문자가 오는 경계 (HTTPServer -> http_server)
                    if (!char.IsUpper(prev) || nextIsLower)
                    {
                        AppendSeparator(builder);
                    }
                }

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString().Trim('_');
        }

        private static void AppendSeparator(StringBuilder builder)
        {
            if (builder.Length > 0 && builder[builder.Length - 1] != '_')
            {
                builder.Append('_');
            }
        }

        /// <summary>
        /// snake_case 이름을 public 프로퍼티용 PascalCase로 바꾼다.
        /// </summary>
        private static string ToPascalCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase)) return string.Empty;

            var builder = new StringBuilder(snakeCase.Length);
            var upperNext = true;

            foreach (var c in snakeCase)
            {
                if (c == '_')
                {
                    upperNext = true;
                    continue;
                }

                builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 정규화 결과가 C# 멤버로 성립하는지 검사한다.
        /// 정규화를 거치면 서로 다른 컬럼이 같은 이름으로 접힐 수 있으므로(ItemID / item_id),
        /// 컴파일 에러 대신 어떤 컬럼이 문제인지 여기서 알려준다.
        /// </summary>
        private static bool ValidateFieldName(string columnName, string fieldName, string propertyName,
            string dataClassName, Dictionary<string, string> usedFieldNames, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrEmpty(fieldName))
            {
                error = $"'{columnName}' -> 정규화 결과가 비어 있습니다.";
                return false;
            }

            if (char.IsDigit(fieldName[0]))
            {
                error = $"'{columnName}' -> '{fieldName}' 는 숫자로 시작해 변수명이 될 수 없습니다.";
                return false;
            }

            if (usedFieldNames.TryGetValue(fieldName, out var previousColumn))
            {
                error = $"'{columnName}' 와 '{previousColumn}' 이 같은 이름('{fieldName}')으로 정규화됩니다.";
                return false;
            }

            if (propertyName == dataClassName)
            {
                error = $"'{columnName}' -> 프로퍼티명 '{propertyName}' 이 클래스명과 같습니다.";
                return false;
            }

            if (propertyName == nameof(IData.SetData))
            {
                error = $"'{columnName}' -> 프로퍼티명 '{propertyName}' 이 생성 메서드명과 충돌합니다.";
                return false;
            }

            usedFieldNames.Add(fieldName, columnName);
            return true;
        }

        private static Type FindTypeByName(string typeName)
        {
            // 모든 로드된 어셈블리에서 검색
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        Debug.Log($"Found type {typeName} in assembly {assembly.FullName}");
                        return type;
                    }
                    
                    // 어셈블리 내의 모든 타입을 검색
                    var types = assembly.GetTypes();
                    foreach (var t in types)
                    {
                        if (t.Name == typeName)
                        {
                            Debug.Log($"Found type {typeName} in assembly {assembly.FullName} by name search");
                            return t;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 일부 어셈블리에서 GetTypes()가 실패할 수 있으므로 무시
                    Debug.LogWarning($"Failed to get types from assembly {assembly.FullName}: {ex.Message}");
                }
            }
            
            Debug.LogError($"Type not found: {typeName}");
            return null;
        }
    }
}
