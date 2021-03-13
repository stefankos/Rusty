using Integreted_Service_NDT.Enums;
using System;
using System.Collections.Generic;


public class Parser5G : Parser4G
{
    public Parser5G(DateTime date, string[] networkElementNames) : base(date, networkElementNames)
    {

    }

    public override string LogPath => $@"D:\MEDIATION\HWI_PM_5G\SiteID_Log\Log{DateTime.Now:yyyyMMdd_HHmm}.txt";

    public override string ParserPath => @"D:\MEDIATION\HWI_PM_5G";

    public override string RowFiles => @"D:\MEDIATION\HWI_PM_5G\SiteID_row_files\";

    public override string ConectionString => @"Data Source=10.31.143.73;Initial Catalog=HWI5G;User ID=topopt;Password=70p0p7";

    public override string ZipFilesPath => @"D:\MEDIATION\HWI_PM_5G\ZipFiles\";


    public override Dictionary<string, dynamic> GetSuppFiles()
    {
        var output = new Dictionary<string, dynamic>();
      
        //PM Names
        var pmNames5G = GetPmNames(ParsersTypeEnum.Parser5G, this.ConectionString, this.PmNamesPath);
        output.Add(SupportFilesType.PM_Name.ToString(), pmNames5G);

        //Counter Names
        var counters5G = GetCounterNames(this.CounterNamesPath);
        output.Add(SupportFilesType.Counter_Name.ToString(), counters5G);

        return output;
    }
}