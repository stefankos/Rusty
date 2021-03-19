use std::{convert::TryInto, error::Error, fs::{File, read_to_string}, io::Read};
use boolinator::Boolinator;
use roxmltree::{Document, Node};

use crate::pm::Pm;
use crate::utils;





pub fn start_gsm_parser() -> Result<(),Box<dyn Error>>{
    let conf_common_path= "parser2G_config_huawei.json";
       
    let conf = utils::ftp_dl_unzip_xml_huawei(conf_common_path)?;

   // parse_xml_gsm(&conf.path_unzip);

    parse_xml_gsm("/Users/stefanvelinov/PycharmProjects/RustXmlParsers/xml/");

    Ok(())
}

fn parse_xml_gsm(_path_unzip:&str) -> Result<Vec<Pm>, Box<dyn Error>>{
    todo!()
}

fn vec_to_df(pms:&Vec<Pm>) {
    todo!()
}

// Document.getElementById
#[test]
fn get_element_by_id() {
    let data1 = read_xml();

    
   
    let doc = Document::parse(&data1).unwrap();
    let site_id = get_attribute_text(&doc, "managedElement", "userLabel");
    
    let nodes: Vec<Node> = doc.descendants().filter(|n| n.has_tag_name("measInfo")).collect();
    for node in nodes {
        let children= node.children();

        for child in children {
            if child.has_attribute("measObjLdn")
            {
                let obj = child.attribute("measObjLdn");
                // let cgi_data = match obj{
                //     Some(x)=> 
                //         {
                //             match x.contains("CGI=") {
                //                 true => cgi_bsc_transponse(x),
                //                 false => cgi_bsc_transponse(x)
                //             }
                            
                //         }
                //     None => "sadsa"
                // };

              
                              
                let counter_values = child.children()
                    .find_map(|n|
                        match n.has_tag_name("measResults") {
                            true => n.text(),
                            false => None
                        })
                    .map(|n| utils::split_text(n, " "));
            
            }

            if child.has_tag_name("measTypes") {
                let counter_numbers= child.text()
                    .map(|n| utils::split_text(n, " "));
         
            }
            if child.has_attribute("endTime") {
                let end_time = child.attribute("endTime");
            }
        }

    }
    println!("fdsfs ");
}

fn read_xml() ->String{
    let path = "/Users/stefanvelinov/PycharmProjects/RustXmlParsers/xml/A20210310.0800+0200-0900+0200_VN4527.xml";
    read_to_string(path).unwrap()

}

fn get_attribute_text(doc:&Document, tag:&str, attribute:&str) ->Vec<String> {
    let nodes: Vec<Node> = doc.descendants().filter(|n| n.has_tag_name(tag)).collect();
    let mut result = nodes.into_iter().map(|n|
        match n.attribute(attribute) {
            Some(x) => x.to_string(),
            None => "".to_string()
        }
    
    ).collect::<Vec<String>>();
    result.retain(|x| !x.is_empty());
    result
}

fn get_attribute_text_from_node(node:&Node, attribute:&str) ->String {
    match node.attribute(attribute) {
            Some(x) => x.to_string(),
            None => "".to_string()
    }
    
    
}