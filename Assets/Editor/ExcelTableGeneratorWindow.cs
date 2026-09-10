using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>将 Excel 工作簿转换为带数据的 C# 表类，并生成唯一的 TableMgr。</summary>
public sealed class ExcelTableGeneratorWindow : EditorWindow
{
    private const string ExcelDirectoryPrefKey = "ExcelTableGenerator.ExcelDirectory";
    private const string OutputDirectoryPrefKey = "ExcelTableGenerator.OutputDirectory";
    private const string ManagerFileName = "TableMgr.cs";

    private static readonly HashSet<string> SupportedTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "int", "string", "int[]"
    };

    private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
        "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    };

    private string excelDirectory;
    private string outputDirectory;
    private Vector2 scrollPosition;

    private sealed class FieldDefinition
    {
        public string Name;
        public string Type;
        public string Comment;
    }

    private sealed class SheetDefinition
    {
        public string ClassName;
        public string SourceName;
        public List<FieldDefinition> Fields;
        public List<List<string>> Rows;
    }

    [MenuItem("Tools/Excel Table Converter")]
    private static void Open()
    {
        GetWindow<ExcelTableGeneratorWindow>("Excel 转表");
    }

    private void OnEnable()
    {
        string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        string defaultExcelDirectory = Path.Combine(projectDirectory, "Excel");
        string legacyExcelDirectory = Path.Combine(Application.dataPath, "Editor", "Excel");
        excelDirectory = EditorPrefs.GetString(ExcelDirectoryPrefKey, defaultExcelDirectory);

        if (string.Equals(Path.GetFullPath(excelDirectory), Path.GetFullPath(legacyExcelDirectory), StringComparison.OrdinalIgnoreCase))
        {
            excelDirectory = defaultExcelDirectory;
        }

        outputDirectory = EditorPrefs.GetString(OutputDirectoryPrefKey, Path.Combine(Application.dataPath, "HotResorces", "Scripts", "Excel"));
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Excel 转 C# 表", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("每个 xlsx 的工作表都会生成一个类：文件名_工作表名.cs。第 1 行字段名、第 2 行类型（int/string/int[]）、第 3 行注释、第 4 行起为数据。首列必须为 int，作为查询 Key。", MessageType.Info);
        DrawDirectoryField("Excel 目录", ref excelDirectory, "选择包含 xlsx 文件的目录");
        DrawDirectoryField("生成目录", ref outputDirectory, "选择 Assets 下且不在 Editor 文件夹内的目录");
        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(excelDirectory) || string.IsNullOrWhiteSpace(outputDirectory)))
        {
            if (GUILayout.Button("生成表代码", GUILayout.Height(32)))
            {
                Generate();
            }
        }
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("生成结果", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• 每页生成：A_B.cs（行数据类 + 内部索引表）");
        EditorGUILayout.LabelField("• 全部页面生成：TableMgr.cs（所有读取入口）");
        EditorGUILayout.LabelField("• 用法：TableMgr.Instance.GetA_B(id)，或 TryGetA_B(id, out row)");
        EditorGUILayout.EndScrollView();
    }

    private static void DrawDirectoryField(string label, ref string value, string title)
    {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.TextField(label, value);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string selectedDirectory = EditorUtility.OpenFolderPanel(title, string.IsNullOrWhiteSpace(value) ? Application.dataPath : value, string.Empty);
            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                value = selectedDirectory;
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void Generate()
    {
        try
        {
            string normalizedExcelDirectory = Path.GetFullPath(excelDirectory);
            string normalizedOutputDirectory = Path.GetFullPath(outputDirectory);
            ValidateDirectories(normalizedExcelDirectory, normalizedOutputDirectory);

            string[] excelFiles = Directory.GetFiles(normalizedExcelDirectory, "*.xlsx", SearchOption.AllDirectories)
                .Where(path => !Path.GetFileName(path).StartsWith("~$", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (excelFiles.Length == 0)
            {
                throw new InvalidOperationException("Excel 目录及其子目录中没有找到 .xlsx 文件。");
            }

            List<SheetDefinition> sheets = new List<SheetDefinition>();
            HashSet<string> classNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (string excelFile in excelFiles)
            {
                foreach (SheetDefinition sheet in ReadWorkbook(excelFile))
                {
                    if (!classNames.Add(sheet.ClassName))
                    {
                        throw new InvalidOperationException(string.Format("生成类名重复：{0}。请调整 Excel 文件名或分页名。", sheet.ClassName));
                    }
                    if (string.Equals(sheet.ClassName, "TableMgr", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("工作表生成的类名不能为 TableMgr。");
                    }
                    sheets.Add(sheet);
                }
            }

            Directory.CreateDirectory(normalizedOutputDirectory);
            int removedFileCount = DeleteObsoleteGeneratedFiles(normalizedOutputDirectory, sheets);
            foreach (SheetDefinition sheet in sheets)
            {
                File.WriteAllText(Path.Combine(normalizedOutputDirectory, sheet.ClassName + ".cs"), GenerateSheetSource(sheet), new UTF8Encoding(false));
            }
            File.WriteAllText(Path.Combine(normalizedOutputDirectory, ManagerFileName), GenerateManagerSource(sheets), new UTF8Encoding(false));

            EditorPrefs.SetString(ExcelDirectoryPrefKey, normalizedExcelDirectory);
            EditorPrefs.SetString(OutputDirectoryPrefKey, normalizedOutputDirectory);
            AssetDatabase.Refresh();
            Debug.Log(string.Format("[ExcelTableGenerator] 已从 {0} 个 Excel 的 {1} 个分页生成表代码，清理旧表 {2} 个：{3}", excelFiles.Length, sheets.Count, removedFileCount, normalizedOutputDirectory));
        }
        catch (Exception exception)
        {
            Debug.LogError("[ExcelTableGenerator] 生成失败：\n" + exception.Message);
        }
    }

    private static int DeleteObsoleteGeneratedFiles(string outputDirectory, IEnumerable<SheetDefinition> sheets)
    {
        HashSet<string> expectedFileNames = new HashSet<string>(sheets.Select(sheet => sheet.ClassName + ".cs"), StringComparer.OrdinalIgnoreCase)
        {
            ManagerFileName
        };
        int removedFileCount = 0;
        foreach (string filePath in Directory.GetFiles(outputDirectory, "*.cs", SearchOption.TopDirectoryOnly))
        {
            if (expectedFileNames.Contains(Path.GetFileName(filePath)) || !IsGeneratedTableSource(File.ReadAllText(filePath)))
            {
                continue;
            }

            string projectRelativePath = FileUtil.GetProjectRelativePath(filePath);
            if (string.IsNullOrEmpty(projectRelativePath) || !AssetDatabase.DeleteAsset(projectRelativePath))
            {
                throw new IOException("无法删除过期的生成表文件：" + filePath);
            }
            removedFileCount++;
        }
        return removedFileCount;
    }

    private static bool IsGeneratedTableSource(string source)
    {
        return source.Contains("// <auto-generated>") &&
               (source.Contains("Excel Table Converter") || source.Contains("所有表数据必须通过 TableMgr 读取"));
    }

    private static void ValidateDirectories(string excelDirectoryPath, string outputDirectoryPath)
    {
        if (!Directory.Exists(excelDirectoryPath))
        {
            throw new DirectoryNotFoundException("Excel 目录不存在：" + excelDirectoryPath);
        }

        string assetsDirectory = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedOutput = outputDirectoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!normalizedOutput.StartsWith(assetsDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedOutput, assetsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("生成目录必须位于项目 Assets 目录内，才能被 Unity 编译。");
        }

        string relativeOutput = normalizedOutput.Substring(assetsDirectory.Length).Replace('\\', '/').Trim('/');
        if (relativeOutput.Equals("Editor", StringComparison.OrdinalIgnoreCase) || relativeOutput.StartsWith("Editor/", StringComparison.OrdinalIgnoreCase) || relativeOutput.Contains("/Editor/"))
        {
            throw new InvalidOperationException("生成目录不能位于 Editor 文件夹内，否则表数据无法在运行时使用。");
        }
    }

    private static IEnumerable<SheetDefinition> ReadWorkbook(string excelFilePath)
    {
        using (FileStream stream = File.OpenRead(excelFilePath))
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
        {
            XNamespace workbookNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace relationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            XNamespace packageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
            XDocument workbook = LoadXml(archive, "xl/workbook.xml");
            XDocument relationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");
            Dictionary<string, string> relationTargets = relationships.Root.Elements(packageRelationshipNamespace + "Relationship")
                .ToDictionary(element => (string)element.Attribute("Id"), element => (string)element.Attribute("Target"), StringComparer.Ordinal);
            List<string> sharedStrings = ReadSharedStrings(archive, workbookNamespace);
            string workbookName = ToIdentifier(Path.GetFileNameWithoutExtension(excelFilePath), "Excel 文件名");

            foreach (XElement sheetElement in workbook.Descendants(workbookNamespace + "sheet"))
            {
                string sheetName = (string)sheetElement.Attribute("name");
                string relationId = (string)sheetElement.Attribute(relationshipNamespace + "id");
                string target;
                if (string.IsNullOrWhiteSpace(sheetName) || string.IsNullOrWhiteSpace(relationId) || !relationTargets.TryGetValue(relationId, out target))
                {
                    throw new InvalidOperationException("无法读取工作簿分页定义：" + excelFilePath);
                }

                string worksheetPath = ResolveWorksheetPath(target);
                yield return ReadSheet(LoadXml(archive, worksheetPath), workbookNamespace, workbookName, sheetName, excelFilePath, sharedStrings);
            }
        }
    }

    private static XDocument LoadXml(ZipArchive archive, string entryPath)
    {
        ZipArchiveEntry entry = archive.GetEntry(entryPath);
        if (entry == null)
        {
            throw new InvalidOperationException("xlsx 缺少必要文件：" + entryPath);
        }
        using (Stream entryStream = entry.Open())
        {
            return XDocument.Load(entryStream);
        }
    }

    private static string ResolveWorksheetPath(string target)
    {
        string normalizedTarget = target.Replace('\\', '/');
        if (normalizedTarget.StartsWith("/", StringComparison.Ordinal))
        {
            return normalizedTarget.TrimStart('/');
        }
        return "xl/" + normalizedTarget.TrimStart('/');
    }

    private static List<string> ReadSharedStrings(ZipArchive archive, XNamespace spreadsheetNamespace)
    {
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
        {
            return new List<string>();
        }
        using (Stream stream = entry.Open())
        {
            XDocument document = XDocument.Load(stream);
            return document.Descendants(spreadsheetNamespace + "si")
                .Select(item => string.Concat(item.Descendants(spreadsheetNamespace + "t").Select(text => text.Value)))
                .ToList();
        }
    }

    private static SheetDefinition ReadSheet(XDocument document, XNamespace spreadsheetNamespace, string workbookName, string sheetName, string excelFilePath, List<string> sharedStrings)
    {
        Dictionary<int, Dictionary<int, string>> values = new Dictionary<int, Dictionary<int, string>>();
        int maximumRow = 0;
        int maximumColumn = 0;
        foreach (XElement cell in document.Descendants(spreadsheetNamespace + "c"))
        {
            string cellReference = (string)cell.Attribute("r");
            int row;
            int column;
            if (!TryParseCellReference(cellReference, out row, out column))
            {
                continue;
            }

            if (!values.ContainsKey(row))
            {
                values.Add(row, new Dictionary<int, string>());
            }
            values[row][column] = ReadCellValue(cell, spreadsheetNamespace, sharedStrings);
            maximumRow = Math.Max(maximumRow, row);
            maximumColumn = Math.Max(maximumColumn, column);
        }

        if (maximumRow < 3 || maximumColumn == 0)
        {
            throw new InvalidOperationException(string.Format("{0} 的分页 {1} 至少需要前三行表头。", excelFilePath, sheetName));
        }

        List<FieldDefinition> fields = new List<FieldDefinition>();
        for (int column = 1; column <= maximumColumn; column++)
        {
            string name = GetCellValue(values, 1, column).Trim();
            string type = GetCellValue(values, 2, column).Trim();
            string comment = GetCellValue(values, 3, column).Trim();
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(type) && string.IsNullOrEmpty(comment))
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            {
                throw new InvalidOperationException(string.Format("{0} 的分页 {1} 第 {2} 列字段名或类型为空。", excelFilePath, sheetName, column));
            }
            if (!SupportedTypes.Contains(type))
            {
                throw new InvalidOperationException(string.Format("{0} 的分页 {1} 字段 {2} 使用了未支持的类型 {3}。当前支持：int、string、int[]。", excelFilePath, sheetName, name, type));
            }
            fields.Add(new FieldDefinition { Name = ToIdentifier(name, "字段名"), Type = type, Comment = comment });
        }

        if (fields.Count == 0)
        {
            throw new InvalidOperationException(string.Format("{0} 的分页 {1} 没有有效字段。", excelFilePath, sheetName));
        }
        if (fields[0].Type != "int")
        {
            throw new InvalidOperationException(string.Format("{0} 的分页 {1} 的第一列必须是 int，作为表 Key。", excelFilePath, sheetName));
        }
        if (fields.Select(field => field.Name).Distinct(StringComparer.Ordinal).Count() != fields.Count)
        {
            throw new InvalidOperationException(string.Format("{0} 的分页 {1} 存在重复字段名。", excelFilePath, sheetName));
        }

        List<List<string>> rows = new List<List<string>>();
        HashSet<int> keys = new HashSet<int>();
        for (int row = 4; row <= maximumRow; row++)
        {
            List<string> rowValues = new List<string>();
            bool hasValue = false;
            for (int column = 1; column <= fields.Count; column++)
            {
                string value = GetCellValue(values, row, column);
                rowValues.Add(value);
                hasValue |= !string.IsNullOrWhiteSpace(value);
            }
            if (!hasValue)
            {
                continue;
            }

            int key;
            if (!int.TryParse(rowValues[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out key))
            {
                throw new InvalidOperationException(string.Format("{0} 的分页 {1} 第 {2} 行的 Key 不是有效 int：{3}", excelFilePath, sheetName, row, rowValues[0]));
            }
            if (!keys.Add(key))
            {
                throw new InvalidOperationException(string.Format("{0} 的分页 {1} 存在重复 Key：{2}", excelFilePath, sheetName, key));
            }
            for (int column = 0; column < fields.Count; column++)
            {
                ValidateValue(rowValues[column], fields[column], excelFilePath, sheetName, row);
            }
            rows.Add(rowValues);
        }

        return new SheetDefinition
        {
            ClassName = workbookName + "_" + ToIdentifier(sheetName, "分页名"),
            SourceName = Path.GetFileName(excelFilePath) + "/" + sheetName,
            Fields = fields,
            Rows = rows
        };
    }

    private static string GetCellValue(Dictionary<int, Dictionary<int, string>> values, int row, int column)
    {
        Dictionary<int, string> rowValues;
        string value;
        return values.TryGetValue(row, out rowValues) && rowValues.TryGetValue(column, out value) ? value ?? string.Empty : string.Empty;
    }

    private static string ReadCellValue(XElement cell, XNamespace spreadsheetNamespace, List<string> sharedStrings)
    {
        string type = (string)cell.Attribute("t");
        if (type == "inlineStr")
        {
            return string.Concat(cell.Descendants(spreadsheetNamespace + "t").Select(text => text.Value));
        }

        string value = (string)cell.Element(spreadsheetNamespace + "v") ?? string.Empty;
        if (type == "s")
        {
            int sharedStringIndex;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sharedStringIndex) || sharedStringIndex < 0 || sharedStringIndex >= sharedStrings.Count)
            {
                throw new InvalidOperationException("xlsx 中的共享字符串索引无效：" + value);
            }
            return sharedStrings[sharedStringIndex];
        }
        return value;
    }

    private static bool TryParseCellReference(string reference, out int row, out int column)
    {
        row = 0;
        column = 0;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }
        int index = 0;
        while (index < reference.Length && char.IsLetter(reference[index]))
        {
            column = column * 26 + (char.ToUpperInvariant(reference[index]) - 'A' + 1);
            index++;
        }
        return index > 0 && index < reference.Length && int.TryParse(reference.Substring(index), NumberStyles.Integer, CultureInfo.InvariantCulture, out row);
    }

    private static void ValidateValue(string value, FieldDefinition field, string excelFilePath, string sheetName, int row)
    {
        if (field.Type == "string" || string.IsNullOrWhiteSpace(value))
        {
            return;
        }
        if (field.Type == "int")
        {
            int number;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                throw new InvalidOperationException(string.Format("{0} 的分页 {1} 第 {2} 行字段 {3} 不是有效 int：{4}", excelFilePath, sheetName, row, field.Name, value));
            }
            return;
        }
        foreach (string part in SplitIntArray(value))
        {
            int number;
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                throw new InvalidOperationException(string.Format("{0} 的分页 {1} 第 {2} 行字段 {3} 的数组元素不是有效 int：{4}", excelFilePath, sheetName, row, field.Name, part));
            }
        }
    }

    private static string GenerateSheetSource(SheetDefinition sheet)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine();
        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("// 来源：" + EscapeComment(sheet.SourceName));
        builder.AppendLine("// 请勿手动修改；请在 Unity 中通过 Tools/Excel Table Converter 重新生成。");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine("public sealed class " + sheet.ClassName);
        builder.AppendLine("{");
        foreach (FieldDefinition field in sheet.Fields)
        {
            builder.AppendLine("    public " + field.Type + " " + field.Name + "; // " + EscapeComment(field.Comment));
        }
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("internal sealed class " + sheet.ClassName + "Table");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly Dictionary<int, " + sheet.ClassName + "> rows = new Dictionary<int, " + sheet.ClassName + ">()");
        builder.AppendLine("    {");
        foreach (List<string> row in sheet.Rows)
        {
            builder.Append("        [").Append(row[0]).Append("] = new ").Append(sheet.ClassName).AppendLine();
            builder.AppendLine("        {");
            for (int index = 0; index < sheet.Fields.Count; index++)
            {
                builder.Append("            ").Append(sheet.Fields[index].Name).Append(" = ").Append(FormatValue(row[index], sheet.Fields[index].Type)).AppendLine(",");
            }
            builder.AppendLine("        },");
        }
        builder.AppendLine("    };");
        builder.AppendLine();
        builder.AppendLine("    public " + sheet.ClassName + " Get(int key)");
        builder.AppendLine("    {");
        builder.AppendLine("        " + sheet.ClassName + " row;");
        builder.AppendLine("        if (!rows.TryGetValue(key, out row))");
        builder.AppendLine("        {");
        builder.AppendLine("            throw new KeyNotFoundException(\"表 " + sheet.ClassName + " 中不存在 Key：\" + key);");
        builder.AppendLine("        }");
        builder.AppendLine("        return row;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    public bool TryGet(int key, out " + sheet.ClassName + " row)");
        builder.AppendLine("    {");
        builder.AppendLine("        return rows.TryGetValue(key, out row);");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string GenerateManagerSource(List<SheetDefinition> sheets)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("// 所有表数据必须通过 TableMgr 读取。请勿手动修改。");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine("public sealed class TableMgr");
        builder.AppendLine("{");
        builder.AppendLine("    public static readonly TableMgr Instance = new TableMgr();");
        builder.AppendLine();
        foreach (SheetDefinition sheet in sheets)
        {
            builder.AppendLine("    private readonly " + sheet.ClassName + "Table " + ToMemberName(sheet.ClassName) + " = new " + sheet.ClassName + "Table();");
        }
        builder.AppendLine();
        builder.AppendLine("    private TableMgr() { }");
        foreach (SheetDefinition sheet in sheets)
        {
            string memberName = ToMemberName(sheet.ClassName);
            builder.AppendLine();
            builder.AppendLine("    public " + sheet.ClassName + " Get" + sheet.ClassName + "(int key)");
            builder.AppendLine("    {");
            builder.AppendLine("        return " + memberName + ".Get(key);");
            builder.AppendLine("    }");
            builder.AppendLine();
            builder.AppendLine("    public bool TryGet" + sheet.ClassName + "(int key, out " + sheet.ClassName + " row)");
            builder.AppendLine("    {");
            builder.AppendLine("        return " + memberName + ".TryGet(key, out row);");
            builder.AppendLine("    }");
        }
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string FormatValue(string value, string type)
    {
        if (type == "string")
        {
            return "\"" + EscapeString(value) + "\"";
        }
        if (type == "int")
        {
            return string.IsNullOrWhiteSpace(value) ? "0" : value;
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            return "new int[0]";
        }
        return "new int[] { " + string.Join(", ", SplitIntArray(value).ToArray()) + " }";
    }

    private static IEnumerable<string> SplitIntArray(string value)
    {
        return value.Split(new[] { ',', '，', ';', '；', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim());
    }

    private static string ToIdentifier(string value, string valueType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(valueType + "不能为空。");
        }
        StringBuilder builder = new StringBuilder();
        foreach (char character in value.Trim())
        {
            if (char.IsLetterOrDigit(character) || character == '_')
            {
                builder.Append(character);
            }
            else
            {
                builder.Append('_');
            }
        }
        if (builder.Length == 0)
        {
            throw new InvalidOperationException(valueType + "无法转换为 C# 标识符：" + value);
        }
        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }
        string identifier = builder.ToString();
        return CSharpKeywords.Contains(identifier) ? "@" + identifier : identifier;
    }

    private static string ToMemberName(string className)
    {
        if (className.StartsWith("@", StringComparison.Ordinal))
        {
            className = className.Substring(1);
        }
        return char.ToLowerInvariant(className[0]) + className.Substring(1) + "Table";
    }

    private static string EscapeString(string value)
    {
        return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private static string EscapeComment(string value)
    {
        return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
    }
}
