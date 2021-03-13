using Integreted_Service_NDT.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Integreted_Service_NDT.Enums;

public class Parser3G : Parser_HWI
{
    public Parser3G(DateTime date, string[] networkElementNames) : base(date, networkElementNames)
    {
        this.SupportFiles = GetSuppFiles();
    }

    public override int Thread => 8;

    public override string ConectionString => @"Data Source=10.31.143.73;Initial Catalog=HWI;User ID=topopt;Password=70p0p7";

    public override string Schema => "PM";

    public override string LogPath => $@"D:\MEDIATION\HWI_PM_3G\Log\Log{DateTime.Now.ToString("yyyyMMdd_HHmm")}.txt";

    public override string ParserPath => @"D:\MEDIATION\HWI_PM_3G";

    public override string RowFiles => @"D:\MEDIATION\HWI_PM_3G\Row_Files";

    public override Dictionary<string, dynamic> SupportFiles { get; set; }

    public override string ZipFilesPath => @"D:\MEDIATION\HWI_PM_3G\ZipFiles\";

    public override bool IsZip => true;

    public override IDictionary<string, string> MaskAndRemotePath
    {
        get
        {
            var dic = new Dictionary<string, string>();

            dic.Add($@"LTE_SITES.*_{Date.ToString("yyyyMMdd")}\.tar\.gz", "/export/home/sysm/opt/oss/server/var/fileint/pmneexport/CompressedPMData");

            foreach (var networkElementName in NetworkElementNames)
            {
                dic.Add($@"A_ZIPED.*{networkElementName}.*_{Date.ToString("yyyyMMdd")}\.tar\.gz", "/export/home/sysm/opt/oss/server/var/fileint/pmneexport/CompressedPMData");
            }
            return dic;

        }
    }


    public override Dictionary<string, dynamic> GetSuppFiles()
    {
        var output = new Dictionary<string, dynamic>();

        //var pmFile = File.ReadAllLines(ParserPath + "\\PM_RNC.txt").ToList();
        //output.Add("PM_Name", pmFile);


        //var lstMapCounters = File.ReadAllLines(ParserPath + "\\RNC_Level.txt").ToList();

        var counters = new Dictionary<string, string>();

        //foreach (var item in lstMapCounters)
        //{
        //    var split = item.Split('|');

        //    counters.Add(split[1], split[2]);
        //}


        //output.Add("Counter_Name", counters);


        //PM Names
        var pmNames3G = GetPmNames(ParsersTypeEnum.Parser3G, this.ConectionString, this.PmNamesPath);
        output.Add(SupportFilesType.PM_Name.ToString(), pmNames3G);

        //Counter Names
        var counters3G = GetCounterNames(this.CounterNamesPath);
        output.Add(SupportFilesType.Counter_Name.ToString(), counters3G);

        return output;
    }


    public override IList<IobjPM> Do_Parse(string file)
    {
        var lstPM = new ConcurrentBag<IobjPM>();

        XElement xmlMeasurment = XElement.Load(file);

        var measData = xmlMeasurment.Elements()
            .Where(n => n.Name.LocalName == "measData");

        Parallel.ForEach(measData, measurements =>
        {
            var networkElement = measurements
                    .Descendants().Where(n => n.Name.LocalName == "managedElement")
                    .Attributes().Where(n => n.Name.LocalName == "userLabel").FirstOrDefault().Value;

            var measInfo = measurements.Descendants().Where(n => n.Name.LocalName == "measInfo");

            foreach (var measure in measInfo)
            {
                try
                {
                    var requestedPMs = new List<string>();
                    var supportParams = new List<PM_Huawei>();


                    if (SupportFiles.ContainsKey(SupportFilesType.PM_Name.ToString()))
                    {
                        supportParams = SupportFiles[SupportFilesType.PM_Name.ToString()] as List<PM_Huawei>;
                        requestedPMs = supportParams.Select(n => n.NamePM).ToList();
                    }

                    if (!requestedPMs.Any(n => n == measure.Attribute("measInfoId").Value))
                        continue;

                    var desendentMeasure = measure.Descendants();

                    var timestamp = desendentMeasure.Where(n => n.Name.LocalName == "granPeriod").FirstOrDefault().Attribute("endTime").Value;


                    var pm = new PM_Huawei();
                    pm.NamePM = "PM_" + measure.Attribute("measInfoId").Value;


                    if (networkElement.Contains("RNC"))
                        pm.NameNE = networkElement;
                    else
                        pm.NameNE = "SRAN";

                    pm.Timestamp = timestamp;
                    pm.Granularity = 60;

                    var objectPM = desendentMeasure.Where(n => n.Name.LocalName == "measValue");
                    var counterValuesString = String.Empty;



                    //Counter numbers
                    pm.CounterNumbers = desendentMeasure.Where(n => n.Name.LocalName == "measTypes")
                       .SelectMany(n => n.Value.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).ToList();

                    var counterNames = new List<string>();


                    //MeasObjLdn
                    foreach (var obj in objectPM)
                    {
                        var getMeasObjLdn = obj.Attributes().Where(n => n.Name.LocalName == "measObjLdn").FirstOrDefault().Value;
                        pm.MesurmentsObj.Add(getMeasObjLdn);

                        var cgiObject = new CGI_Object();


                        var variousCI = new List<string>();

                        if (getMeasObjLdn.Contains("Cell Name"))
                        {
                            if (getMeasObjLdn.Length >= getMeasObjLdn.IndexOf("Cell Name") + 17)
                                variousCI.Add(getMeasObjLdn.Substring(getMeasObjLdn.IndexOf("Cell Name") + 10, 6));
                        }
                        if (getMeasObjLdn.Contains("Local cell name"))
                            variousCI.Add(getMeasObjLdn.Substring(getMeasObjLdn.IndexOf("Local cell name") + 16, 6));
                        if (getMeasObjLdn.Contains("Local Cell ID"))
                        {
                            var match = Regex.Match(getMeasObjLdn, "(Local Cell ID=)(\\d+)");
                            if (match.Success)
                                variousCI.Add(match.Groups[2].ToString());
                        }
                        if (getMeasObjLdn.Contains("CellID"))
                        {
                            var match = Regex.Match(getMeasObjLdn, ".*CellID=(\\d.*),.*");
                            if (match.Success)
                                variousCI.Add(match.Groups[1].ToString());
                        }

                        var ci = variousCI.Where(n => !string.IsNullOrEmpty(n) && !n.Contains("N/A")).FirstOrDefault();
                        if (string.IsNullOrEmpty(ci))
                            cgiObject.CI = " ";
                        else
                            cgiObject.CI = ci;

                        if (getMeasObjLdn.StartsWith("RNC"))
                            pm.NameNE = getMeasObjLdn.Substring(0, 4);


                        if (getMeasObjLdn.Contains("Name"))
                            cgiObject.NodeB = getMeasObjLdn.Substring(getMeasObjLdn.IndexOf("Name") + 5, 6);
                        else if (Regex.IsMatch(getMeasObjLdn, "(^[A-Z]{2}\\d{4})"))
                            cgiObject.NodeB = Regex.Match(getMeasObjLdn, "(^[A-Z]{2}\\d{4})").Value;


                        pm.CgiObjects.Add(cgiObject);

                        counterValuesString = obj.Descendants().Where(n => n.Name.LocalName == "measResults").FirstOrDefault().Value;

                        var counterValuesSplit = counterValuesString.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        //Values
                        var counterValues = new List<string>();
                        foreach (var value in counterValuesSplit)
                        {
                            value.Trim();
                            if (value != "NIL")
                            {
                                pm.CounterValues.Add(value);
                            }
                            else
                            {
                                pm.CounterValues.Add(null);
                            }
                        }

                        if (pm.MesurmentsObj.Count * pm.CounterNumbers.Count != pm.CounterValues.Count)
                        {
                            throw new Exception($"Metod DoParse: For {pm.NamePM} there are {pm.MesurmentsObj.Count} objects " +
                                   $"with {pm.CounterNames.Count} counters and {pm.CounterValues.Count} values!!!");
                        }

                    }

                    lstPM.Add(pm);
                }
                catch (Exception ex)
                {
                    var pm = measure.Attribute("measInfoId").Value;
                    var files = file;
                    var test = ex.ToString();
                    throw new Exception($"Error: Method Do_Parse: {ex.Message}");

                }
            }
        });
        return lstPM.ToList();
    }

    public override IList<string> CheckAndGetUploadedFiles(string path)
    {
        List<string> filesForParser = Directory.GetFiles(path, "*.*").ToList();

        //Zip Phase
        if (filesForParser.Any(n => n.Contains(".tar.gz")))
        {
            //filesForParser.RemoveAll(n => n.Contains("RNC"));

            //foreach (var element in NetworkElementNames)
            //{
            //    filesForParser.AddRange(Directory.GetFiles(path, $"*{element}*.*"));
            //}
            return filesForParser.Where(n => n.Contains(".tar.gz")).ToList();
        }


        foreach (var element in NetworkElementNames)
        {
            var files = Directory.GetFiles(Path.Combine(path, element), "*.*");
            filesForParser.AddRange(files);
        }



        if (filesForParser.Count == 0)
            throw new Exception("There aren't any files! Check source (FTP/SFTP...) for valid files or check module  _iAccessRemoteFile.GetFiles()");




        filesForParser.RemoveAll(n => n.Contains("0000+0200-0000+0200") || n.Contains("0000+0300-0000+0300") || n.Contains("0000+0100-0000+0100"));

        if (TimeResolution == TimeResolution.Hourly && filesForParser.All(n => n.Contains(".xml")))
        {
            var hour = Date.Hour;
            return filesForParser.Where(n => n.Contains("." + hour.ToString())).ToList();
        }


        //if (filesForParser.Count != _parser.ExpextedFiles())
        //    throw new Exception($"Expected number of files is {_parser.ExpextedFiles()}. Input files are {filesForParser.Count}! ");


        return filesForParser;
    }
}

