use std::error::Error;

use crate::utils;



pub fn start_gsm_parser() -> Result<(),Box<dyn Error>>{
    let conf_common_path= "parser2G_config_huawei.json";
       
    let conf = utils::ftp_dl_unzip_xml_huawei(conf_common_path)?;

   // parse_xml_gsm(&conf.path_unzip);

    parse_xml_gsm("/Users/stefanvelinov/PycharmProjects/RustXmlParsers/xml/");

    Ok(())
}

fn parse_xml_gsm(_path_unzip:&str){
    todo!()
}