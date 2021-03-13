use crate::enum_parser_type::EnumParserType;
use structopt::StructOpt;
use std::error::Error;
use strum::IntoEnumIterator;
use std::convert::AsRef;
use std::string::ToString;


fn check_str(src: &str) -> Result<String,Box<dyn Error>> {

    let is_valid_parser_type = EnumParserType::iter().any(|x| x.as_ref() == src);
    
    let valid_parser_types = 
        EnumParserType::iter().map(|x| x.to_string()).collect::<Vec<String>>().join(", ");

    match is_valid_parser_type {
        true =>  Ok(src.to_string()),
        false => Err(format!("Valid values are {}!",valid_parser_types).into())
    }
}


#[derive(StructOpt,Debug)]
///Parse Huawei xml files into database. Just pass -h for help.
pub struct Cli {
    ///Could be: 2G,3G,4G or 5G
    #[structopt(short = "p", long = "parser", parse(try_from_str = check_str))]
    pub parser_type:String,
    ///Please use following date format: YYYY-MM-DD
    #[structopt(short = "time", long )]
    pub timestamp:  Option<String>
}
