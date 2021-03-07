extern crate serde;
extern crate serde_json;
#[macro_use] extern crate serde_derive;
use std::fs::File;
use serde::de::{self, Deserialize, Deserializer};

#[derive(Serialize,Deserialize,Debug)]
struct Input {
    parser_type:String,
    timestamp:String,
    path_ftp: Vec<String>,
    path_app:String,
    path_zip:String,
    path_log:String,
    path_unzip:String
}


fn main() -> Result<(), Error> {
    let parser = Input{
        parser_type: String::from("2G"),
        timestamp: String::from("2020-01-01"),
        path_ftp: vec!["dasdas".to_string(),"dsadada".to_string()],
        path_app: String::from(r#"c:\dsad"#),
        path_zip: String::from(r#"c:\dsad"#),
        path_log: String::from(r#"c:\dsad"#),
        path_unzip: String::from(r#"c:\dsad"#)
    };

    
    let serialized = serde_json::to_string(&parser).unwrap();

    
    let mut file = File::create("ConfParser2G.txt")?;

    serde_json::to_writer(&mut file, &parser)?;

   

    println!("Serialized = {}",serialized);

    Ok(())

}
