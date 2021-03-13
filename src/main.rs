#[macro_use] extern crate serde_derive;
mod utils;
#[path="./models/cli.rs"] mod cli;
use std::{collections::HashMap, fs::File, io::{BufRead, BufReader, Error, Write}};

use chrono::Duration;
use cli::Cli;
use structopt::StructOpt;
#[path="./models/ftp_config.rs"] mod ftp_config;
#[path="./enums/enum_parser_type.rs"] mod enum_parser_type;
#[path="./models/input_param.rs"] mod input_param;
mod models {
    pub(crate) mod parser_gsm;
    pub(crate) mod parser_umts;
}

fn test() {
    let mut data:Vec<&str> = Vec::<&str>::new();
    let xyz = ["dffs","fdsfdsf","fdsfdsfs"];

    for x in xyz.iter() {
      let val = format!("hello {}", x); // creating new string
      data.push(&val); // getting string slice!
    }
    
    for m in data {
        println!("{}",m);
    }
  }


fn main() -> Result<(),Box<dyn std::error::Error>>{
    
    test();


    
    let pm_file = BufReader::new(File::open("/Users/stefanvelinov/PycharmProjects/RustXmlParsers/2G_PmNames.txt")?).lines().collect::<Vec<_>>();

    let counter_name = BufReader::new(File::open("/Users/stefanvelinov/PycharmProjects/RustXmlParsers/2G_CounterNames.txt")?).lines();
    let mut hash : HashMap::<String,String> = HashMap::new();
    let split = counter_name
        .map(|n| 
            n.unwrap()
            .split('|')
            .map(str::to_owned)
            .collect::<Vec<_>>());


    // 1. Get input params from console
    let input_param = Cli::from_args_safe();

    let mut  obj_cli = match input_param {
        Ok(obj)=> obj,
        Err(e) => {
            println!("Error: {} ",e);
            Cli { parser_type:"GSM".to_string(), timestamp:Some("2021-02-01".to_string())}
        }
    };

   // 2. Set timestamp = Yesterday if ot's None
    if obj_cli.timestamp.is_none() {
        let yesterday = chrono::Utc::now() + Duration::days(-1);
        obj_cli.timestamp = Some(yesterday.format("%Y-%m-%d").to_string());
    }
    println!("{:?}",obj_cli);

    // 3. Match obj with console configuration  
    match  obj_cli.parser_type.as_str(){
        "GSM" => models::parser_gsm::start_gsm_parser(),
        "UMTS" => models::parser_umts::start_umts_parser(),
        _ => panic!("No such parser type")
    }

}
