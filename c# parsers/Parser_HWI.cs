using Integreted_Service_NDT.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Integreted_Service_NDT.Enums;

public abstract class Parser_HWI : Parser
{
    public Parser_HWI(DateTime date, string[] networkElementNames) : base(date, networkElementNames)
    {
    }

    public override IEnumerable<IobjPM> Agregate15minToHour(IEnumerable<IobjPM> lstPM_15min) => null;

    public override string DbAccessClass => "Write_DB_HWI";

    public override string FileProtocolType => "FTP";

    public override bool NeedAgregate => false;

    public override ParserVendor ParserVendor => ParserVendor.Huawei;

    public override IList<string> CheckAndGetUploadedFiles(string path)
    {

        var filesForParser = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).ToList();


        if (filesForParser.Count == 0)
            throw new Exception("There aren't any files! Check source (FTP/SFTP...) for valid files or check module  _iAccessRemoteFile.GetFiles()");


        if (filesForParser.Any(n => n.Contains(".tar.gz")))
            return filesForParser.Where(n => n.Contains(".tar.gz")).ToList();

        filesForParser.RemoveAll(n => n.Contains("0000+0200-0000+0200") || n.Contains("0000+0300-0000+0300") || n.Contains("0000+0100-0000+0100"));

        if (TimeResolution == TimeResolution.Hourly && filesForParser.All(n => n.Contains(".xml")))
        {
            var hour = Date.Hour.ToString("00");
            return filesForParser.Where(n => n.Contains("." + hour)).ToList();
        }


        //if (filesForParser.Count != _parser.ExpextedFiles())
        //    throw new Exception($"Expected number of files is {_parser.ExpextedFiles()}. Input files are {filesForParser.Count}! ");


        return filesForParser;
    }

    public string PmNamesPath => Path.Combine(this.ParserPath, "PmNames.txt");
    public string CounterNamesPath => Path.Combine(this.ParserPath, "CounterNames.txt");

    public abstract Dictionary<string, dynamic> GetSuppFiles();

    private List<string> CheckForDate(string values)
    {
        //"2017-1-30 01:00:48"
        var pattern = "\\d{4}-\\d{1,2}-\\d{1,2} \\d{2}:\\d{2}:\\d{2}";   //2017-01-29 05:05:03
        var pattern1 = "\\+\\d{2}:\\d{2}";  //+02:00

        var splitValues = new List<string>();

        if (Regex.IsMatch(values, pattern))
        {
            var date = Regex.Match(values, pattern).Value.Replace(" ", "_");
            var resultValue = Regex.Replace(values, pattern, date);

            if (Regex.IsMatch(values, pattern1))
                resultValue = Regex.Replace(resultValue, pattern1, "");

            resultValue = resultValue.Replace("\"", "");
            splitValues.AddRange(resultValue.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList());

            for (int i = 0; i < splitValues.Count; i++)
            {
                if (splitValues[i].Contains("_"))
                    splitValues[i] = splitValues[i].Replace("_", " ");
            }
            return splitValues;
        }

        else
        {
            splitValues.AddRange(values.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList());
            return splitValues;
        }

    }

    public List<PM_Huawei> GetPmNames(ParsersTypeEnum technology, string conectionString, string suppFilePath)
    {
        var pmInfo = new List<PM_Huawei>();

        var pmNames = File.ReadAllLines(suppFilePath).Skip(1).ToList();
        foreach (var pmName in pmNames)
        {
            pmInfo.Add(new PM_Huawei() { NamePM = pmName, Technology = technology, ConnectionString = conectionString });
        }

        return pmInfo;
    }

    public Dictionary<string, string> GetCounterNames(string suppFilePath)
    {
        var counters = new Dictionary<string, string>();

        foreach (var item in File.ReadAllLines(suppFilePath).ToList())
        {
            var split = item.Split('|');

            counters.Add(split[1], split[2]);
        }

        return counters;
    }

}
