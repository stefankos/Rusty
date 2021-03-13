using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Integreted_Service_NDT.Interfaces;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Integreted_Service_NDT.Enums;
using System.Text.RegularExpressions;
using Integreted_Service_NDT;

public class Parser4G : Parser_HWI
{

    public Parser4G(DateTime date, string[] networkElementNames) : base(date, networkElementNames)
    {
        this.SupportFiles = GetSuppFiles();
    }


    public override string LogPath => $@"D:\MEDIATION\HWI_PM_4G\SiteID_Log\Log{DateTime.Now.ToString("yyyyMMdd_HHmm")}.txt";

    public override string ParserPath => @"D:\MEDIATION\HWI_PM_4G";

    public override bool IsZip => true;

    public override string RowFiles => @"D:\MEDIATION\HWI_PM_4G\SiteID_row_files\";

    public override string ConectionString => @"Data Source=10.31.143.73;Initial Catalog=HWI4G;User ID=topopt;Password=70p0p7";

    public override string Schema => "PM";

    public override string ZipFilesPath => @"D:\MEDIATION\HWI_PM_4G\ZipFiles\";

    public override int Thread => 10;

    public override IDictionary<string, string> MaskAndRemotePath
    {
        get
        {
            var dic = new Dictionary<string, string>();

            foreach (var networkElementName in NetworkElementNames)
            {
                dic.Add($@"LTE_SITES.*_{Date.ToString("yyyyMMdd")}\.tar\.gz", "/export/home/sysm/opt/oss/server/var/fileint/pmneexport/CompressedPMData");
            }
            return dic;

        }
    }

    public override Dictionary<string, dynamic> SupportFiles { get; set; }

    public override IList<IobjPM> Do_Parse(string file)
    {
        var lstPM = new ConcurrentBag<IobjPM>();

        XElement xmlMeasurment = XElement.Load(file);

        var measData = xmlMeasurment.Elements()
            .Where(n => n.Name.LocalName == "measData");



        //Parallel.ForEach(measData, measurements =>
        foreach (var measurements in measData)
        {
            var networkElementName = measurements
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

                    if (Enum.TryParse(this.GetType().Name, out ParsersTypeEnum technology))
                        pm.Technology = technology;

                    pm.NamePM = measure.Attribute("measInfoId").Value;
                    
                    if (supportParams.Count > 0)
                        pm.ConnectionString = supportParams.Where(n => n.NamePM == pm.NamePM).FirstOrDefault().ConnectionString;

                    pm.NamePM = "PM_" + pm.NamePM;

                    pm.NameNE = "SRAN";
                    pm.Timestamp = timestamp;
                    pm.Granularity = 3600;

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

                        if (getMeasObjLdn.Contains("Cell Name"))
                            cgiObject.CI = getMeasObjLdn.Substring(getMeasObjLdn.IndexOf("Cell Name") + 10, 6);
                        else if (getMeasObjLdn.Contains("Local cell name"))
                        {
                            try
                            {
                                cgiObject.CI = getMeasObjLdn.Substring(getMeasObjLdn.IndexOf("Local cell name") + 16, 6);

                            }
                            catch (Exception ex)
                            {
                                cgiObject.CI = "";
                            }

                        }
                        else  //No CellName   The fild has to be not null!!!
                            cgiObject.CI = " ";


                        var regexPat = @"(Name=([A-z]{2}\d{4}))";
                        if (Regex.IsMatch(getMeasObjLdn, regexPat))
                            pm.NameNE = Regex.Match(getMeasObjLdn, regexPat).Groups[2].ToString();

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
                    GC.Collect();
                    Console.WriteLine(string.Format($"Error -  Memory -  {GC.GetTotalMemory(false) / 1024 / 1024} Mbytes"));
                    Console.WriteLine(string.Format($"Error {ex.Message}   {ex.InnerException.Message}"));
                    throw new Exception($"Error: File: {file}");
                }



            }

        }
        //);


        return lstPM.ToList();
    }

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
            var hour = Date.Hour;
            return filesForParser.Where(n => n.Contains("." + hour.ToString())).ToList();
        }


        //if (filesForParser.Count != _parser.ExpextedFiles())
        //    throw new Exception($"Expected number of files is {_parser.ExpextedFiles()}. Input files are {filesForParser.Count}! ");


        return filesForParser;
    }

    //public override Dictionary<string, dynamic> GetSuppFiles()
    //{
    //    var output = new Dictionary<string, dynamic>();


    //    var pmFile = File.ReadAllLines(ParserPath + "\\PM_LTE.txt").Skip(1).ToList();
    //    output.Add("PM_Name", pmFile);

    //    var lstMapCounters = File.ReadAllLines(ParserPath + "\\Counters_LTE.txt").ToList();
    //    var counters = new Dictionary<string, string>();

    //    foreach (var item in lstMapCounters)
    //    {
    //        var split = item.Split('|');

    //        counters.Add(split[1], split[2]);
    //    }

    //    output.Add("Counter_Name", counters);

    //    return output;
    //}


    public override Dictionary<string, dynamic> GetSuppFiles()
    {
        var output = new Dictionary<string, dynamic>();

        //PM Names
        var pmNames4G = GetPmNames(ParsersTypeEnum.Parser5G, this.ConectionString, this.PmNamesPath);
        output.Add(SupportFilesType.PM_Name.ToString(), pmNames4G);

        //Counter Names
        var counters4G = GetCounterNames(this.CounterNamesPath);
        output.Add(SupportFilesType.Counter_Name.ToString(), counters4G);

        return output;
    }


}

