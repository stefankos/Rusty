using Integreted_Service_NDT;
using Integreted_Service_NDT.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


public class Parser4G_5G : Parser4G
{
    public Parser4G_5G(DateTime date, string[] networkElementNames) : base(date, networkElementNames)
    {
      
    }

    public override string LogPath => $@"{ParserPath}\SiteID_Log\Log{DateTime.Now:yyyyMMdd_HHmm}.txt";
    public override string ParserPath => @"D:\MEDIATION\HWI_PM_4G_5G";
    public override Dictionary<string, dynamic> SupportFiles { get; set; }

    public override string RowFiles => $@"{ParserPath}\SiteID_row_files\";
   
    public override string ZipFilesPath => $@"{ParserPath}\ZipFiles\";

    public string GetConnectionString5G()
    {
        return this.ConectionString.Replace("Catalog=HWI4G", "Catalog=HWI5G");
    }

    public override Dictionary<string, dynamic> GetSuppFiles()
    {
        var output = new Dictionary<string, dynamic>();

        var parser4G = new Parser4G(DateTime.Now, null);

        var parser4Gstr = ParsersTypeEnum.Parser4G.ToString();
        var parser5Gstr = ParsersTypeEnum.Parser5G.ToString();

        //PM Names
        var pmNames4G = GetPmNames(ParsersTypeEnum.Parser4G, this.ConectionString, parser4G.PmNamesPath);
        var pmNames5G = GetPmNames(ParsersTypeEnum.Parser5G, GetConnectionString5G(), this.PmNamesPath);

        output.Add(SupportFilesType.PM_Name.ToString(), pmNames4G.Union(pmNames5G).ToList());

        //Counter Names
        var counters4G = GetCounterNames(parser4G.CounterNamesPath);
        var counters5G = GetCounterNames(this.CounterNamesPath);

        output.Add(SupportFilesType.Counter_Name.ToString(), counters4G.Concat(counters5G).ToDictionary(s => s.Key, s => s.Value));

        return output;
    }


}

