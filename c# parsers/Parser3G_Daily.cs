using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Integreted_Service_NDT.Enums;
using Integreted_Service_NDT.Interfaces;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Globalization;

public class Parser3G_Daily : Parser3G
{
    public Parser3G_Daily(DateTime date, string[] networkElementNames) : base(date, networkElementNames)
    {
    }
    public override string ParserPath => @"D:\MEDIATION\HWI_PM_3G_Daily";

    public override string RowFiles => @"D:\MEDIATION\HWI_PM_3G_Daily\Row_Files";

    public override string ZipFilesPath => @"D:\MEDIATION\HWI_PM_3G_Daily\ZipFiles\";

    public override string LogPath => $@"D:\MEDIATION\HWI_PM_3G_Daily\Log\Log{DateTime.Now.ToString("yyyyMMdd_HHmm")}.txt";

    public override IDictionary<string, string> MaskAndRemotePath
    {
        get
        {
            var dic = new Dictionary<string, string>();

            foreach (var networkElementName in NetworkElementNames)
            {
                dic.Add($@"A_ZIPED.*{networkElementName}.*_{Date.ToString("yyyyMMdd")}\.tar\.gz", "/export/home/sysm/opt/oss/server/var/fileint/pmneexport/CompressedPMData");
            }
            return dic;

        }
    }

    public override IList<string> CheckAndGetUploadedFiles(string path)
    {
        List<string> filesForParser = Directory.GetFiles(path, "*.*").ToList();

        //Zip Phase
        if (filesForParser.Any(n => n.Contains(".tar.gz")))
        {
            filesForParser.RemoveAll(n => n.Contains("RNC"));

            foreach (var element in NetworkElementNames)
            {
                filesForParser.AddRange(Directory.GetFiles(path, $"*{element}*.*"));
            }
            return filesForParser.Where(n => n.Contains(".tar.gz")).ToList();
        }


        foreach (var element in NetworkElementNames)
        {
            var files = Directory.GetFiles(Path.Combine(path, element), "*.*");
            filesForParser.AddRange(files);
        }



        if (filesForParser.Count == 0)
            throw new Exception("There aren't any files! Check source (FTP/SFTP...) for valid files or check module  _iAccessRemoteFile.GetFiles()");




        filesForParser = filesForParser.Where(n => n.Contains("0000+0200-0000+0200") ||
                                              n.Contains("0000+0300-0000+0300") ||
                                              n.Contains("0000+0100-0000+0100")).ToList();

        if (TimeResolution == TimeResolution.Hourly && filesForParser.All(n => n.Contains(".xml")))
        {
            var hour = Date.Hour;
            return filesForParser.Where(n => n.Contains("." + hour.ToString())).ToList();
        }

        return filesForParser;
    }

    public override IList<IobjPM> Do_Parse(string file)
    {
        var lstPM = new ConcurrentBag<IobjPM>();

        XElement xmlMeasurment = XElement.Load(file);

        var timestamp = xmlMeasurment.Descendants().Where(n => n.Name.LocalName == "measCollec")
                    .Attributes().Where(n => n.Name.LocalName == "beginTime").FirstOrDefault().Value;

       
        //Add one hour advance cos in dataTable creation there is one hour excluded
        timestamp = timestamp.Replace("T00", "T01");

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
                    var suppFiles = SupportFiles["PM_Name"] as List<string>;

                    for (int i = 0; i < suppFiles.Count; i++)
                    {
                        suppFiles[i] = Regex.Replace(suppFiles[i], "_[A-Z]*", "");
                    }


                    if (!suppFiles.Any(n => n == measure.Attribute("measInfoId").Value))
                        continue;

                    var desendentMeasure = measure.Descendants();

                   

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

                        if (getMeasObjLdn.Contains("CellID"))
                        {
                            var match = Regex.Match(getMeasObjLdn, ".*CellID=(\\d.*),.*");
                            if (match.Success)
                                cgiObject.CI = match.Groups[1].ToString();
                        }
                        else
                            cgiObject.CI = " ";

                        if (getMeasObjLdn.Contains("RNC"))
                            pm.NameNE = getMeasObjLdn.Substring(getMeasObjLdn.IndexOf("RNC"), 4);
                        else
                            pm.NameNE = " ";

                        if (getMeasObjLdn.Contains("/DEST Cell ID:")) 
                        {
                            var startIndex = getMeasObjLdn.IndexOf("/DEST Cell ID:") + 14;

                            cgiObject.TargetCI = getMeasObjLdn.Substring(startIndex , getMeasObjLdn.Count() - startIndex);
                            cgiObject.TargetRNC = getMeasObjLdn.Substring(startIndex - 17, 3);
                        }

                        if (getMeasObjLdn.Contains("UCELL_GCELL:Label="))
                        {
                            var startIndex = getMeasObjLdn.IndexOf("MNC") + 12;

                            cgiObject.TargetCI = getMeasObjLdn.Substring(startIndex, getMeasObjLdn.Count() - startIndex);
                        }



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
}


