using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Integreted_Service_NDT.Interfaces;
using System.Xml.Linq;
using Integreted_Service_NDT.Enums;


public class Parser2G : Parser_HWI
{
    public Parser2G(DateTime date, string[] networkElementNames) : base(date, networkElementNames)
    {
        this.SupportFiles = GetSuppFiles();
    }

    public override string LogPath => @"D:\MEDIATION\HWI_PM_2G\BSC_Log\log.txt";

    public override string ParserPath => @"D:\MEDIATION\HWI_PM_2G";

    public override bool IsZip => true;

    public override string RowFiles => @"D:\MEDIATION\HWI_PM_2G\BSC_row_files\";

    public override string ConectionString => @"Data Source=10.31.143.73;Initial Catalog=HWI2G;User ID=topopt;Password=70p0p7";

    public override string Schema => "PM";

    public override Dictionary<string, dynamic> SupportFiles { get; set; }

    public override string ZipFilesPath => @"D:\MEDIATION\HWI_PM_2G\ZipFiles\";

    public override int Thread { get => 8; }

    public override IDictionary<string, string> MaskAndRemotePath
    {
        get
        {
            var dic = new Dictionary<string, string>();

            foreach (var networkElementName in NetworkElementNames)
            {
                // dic.Add($"A_ZIPED_NODEB_25__{networkElementName}_{Date.ToString("yyyyMMdd")}.tar.gz", "/export/home/sysm/opt/oss/server/var/fileint/pmneexport/CompressedPMData");
                dic.Add($".*{networkElementName}_{Date.ToString("yyyyMMdd")}.tar.gz", "/export/home/sysm/opt/oss/server/var/fileint/pmneexport/CompressedPMData");
            }
            return dic;

        }
    }



    private CGI_Object CGI_BSC_Transponse(string measObjLdn)
    {
        string cgi = ""; string cgiTarget = ""; string lac = ""; string ci = ""; string ciTarget = "";
        var pm = new CGI_Object();

        //pm.BSC= measObjLdn.Substring(0, 4);

        int count = CountStringOccurrences(measObjLdn, "CGI");
        if (count == 1)
        {
            cgi = measObjLdn.Substring(measObjLdn.IndexOf("CGI") + 4, 13);
            lac = cgi.Substring(5, 4);
            ci = cgi.Substring(9, 4);

        }

        else
        {
            cgiTarget = measObjLdn.Substring(measObjLdn.IndexOf("CGI") + 4 + 13 + 5, 13);
            cgi = measObjLdn.Substring(measObjLdn.IndexOf("CGI") + 4, 13);
            lac = cgi.Substring(5, 4);
            ci = cgi.Substring(9, 4);
            ciTarget = cgiTarget.Substring(9, 4);
            pm.TargetCI = int.Parse(ciTarget, System.Globalization.NumberStyles.HexNumber).ToString();
        }


        pm.LAC = int.Parse(lac, System.Globalization.NumberStyles.HexNumber);
        pm.CI = int.Parse(ci, System.Globalization.NumberStyles.HexNumber).ToString();

        return pm;
    }

    private int CountStringOccurrences(string text, string pattern)
    {
        // Loop through all instances of the string 'text'.
        int count = 0;
        int i = 0;
        while ((i = text.IndexOf(pattern, i)) != -1)
        {
            i += pattern.Length;
            count++;
        }
        return count;
    }

    public override Dictionary<string, dynamic> GetSuppFiles()
    {
        var output = new Dictionary<string, dynamic>();

        //PM Names
        var pmNames2G = GetPmNames(ParsersTypeEnum.Parser2G, this.ConectionString, this.PmNamesPath);
        output.Add(SupportFilesType.PM_Name.ToString(), pmNames2G);

        //Counter Names
        var counters2G = GetCounterNames(this.CounterNamesPath);
        output.Add(SupportFilesType.Counter_Name.ToString(), counters2G);

        return output;
    }

    public override IList<IobjPM> Do_Parse(string file)
    {
        var lstPM = new List<IobjPM>();

        XElement xmlMeasurment = XElement.Load(file);

        
        var allMesurement = xmlMeasurment.Elements()
            .Where(n => n.Name.LocalName == "measData")
            .Descendants().Where(n => n.Name.LocalName == "measInfo");

        var controler = xmlMeasurment.Elements()
            .Where(n => n.Name.LocalName == "measData")
            .Descendants().Where(n => n.Name.LocalName == "managedElement").FirstOrDefault();

        try
        {
            foreach (var measure in allMesurement)
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
                pm.NameNE = controler.Attribute("userLabel").Value;
                pm.Timestamp = timestamp;
                pm.Granularity = 3600;

                var objectPM = desendentMeasure.Where(n => n.Name.LocalName == "measValue");
                var counterValuesString = String.Empty;

                //Counter numbers
                pm.CounterNumbers = desendentMeasure.Where(n => n.Name.LocalName == "measTypes")
                   .SelectMany(n => n.Value.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)).ToList();


                //MeasObjLdn
                foreach (var obj in objectPM)
                {

                    var getMeasObjLdn = obj.Attributes().Where(n => n.Name.LocalName == "measObjLdn").FirstOrDefault().Value;
                    pm.MesurmentsObj.Add(getMeasObjLdn);

                    if (getMeasObjLdn.Contains("CGI="))
                    {
                        var cgiData = CGI_BSC_Transponse(getMeasObjLdn);
                        pm.CgiObjects.Add(cgiData);
                    }
                    else
                        pm.CgiObjects.Add(new CGI_Object());

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
        }
        catch (Exception ex)
        {
            throw new Exception($"Error: Method Do_Parse: {ex.Message}");
        }



        return lstPM;
    }
   
    public override IEnumerable<IobjPM> Agregate15minToHour(IEnumerable<IobjPM> lstPM_15min) => null;
 
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

    
}

