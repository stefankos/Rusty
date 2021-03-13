use std::error::Error;

use crate::utils;

fn parse_xml_umts(_path_unzip:&str){
    todo!()
}

pub fn start_umts_parser() -> Result<(),Box<dyn Error>>{
    let conf_common_path= "parser3G_config_huawei.json";
    let conf = utils::ftp_dl_unzip_xml_huawei(conf_common_path)?;
    parse_xml_umts(&conf.path_unzip);
    
    Ok(())
}