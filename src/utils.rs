use std::{error::Error, io::{BufRead, Read, Write}};
use std::fs::File;
use std::io::BufReader;
use ftp::{FtpStream};
use std::path::Path;
use crate::{ftp_config::FtpConf, input_param::InputParam};
use flate2::read::GzDecoder;
use tar::Archive;
use regex::Regex;

pub fn obj_to_json<T: serde::Serialize>(file_path:&str, obj:T) -> Result<(),serde_json::Error>{

    let mut file = File::create(file_path)?;

    serde_json::to_writer(&mut file, &obj)?;

    Ok(())
}

pub fn json_to_obj<T: serde::Deserialize,>(path: &str) -> Result<T, Box<dyn Error>> {
    // Open the file in read-only mode with buffer.
    let file = File::open(path)?;
    let reader = BufReader::new(file);

    // Read the JSON contents of the file as an instance of `User`.
    let obj = serde_json::from_reader(reader)?;

    // Return the `User`.
    Ok(obj)
}

pub(crate) fn ftp_dl_unzip_xml_huawei(conf_common_path: &str) -> Result<InputParam,Box<dyn Error>>{
    // Read Configuration 
    let conf_ftp_path = "ftp_config_huawei.json";
    let conf_common =  json_to_obj::<InputParam>(conf_common_path).expect("Wrong config path!");
    println!("{:?}",conf_common);
    let conf_ftp = json_to_obj::<FtpConf>(conf_ftp_path).expect("Ftp config not found!");
    println!("{:?}",conf_ftp);

    // Clean Dirs
    for dir in ["/Users/stefanvelinov/PycharmProjects/RustXmlParsers/test/",
                     "/Users/stefanvelinov/PycharmProjects/RustXmlParsers/targz/"].iter() {
        del_all_files_in_dir(dir)?;
    }

    // Dl files from ftp
    get_zip_from_ftp(&conf_common.path_zip, &conf_ftp, &conf_common.ftp_files_pattern)?;

    // Unzip files
    let files = std::fs::read_dir(&conf_common.path_zip)?;
    for file in files {
        let file_path = file?.path();
        
        match unzip(&file_path.to_str().unwrap(), &conf_common.path_unzip) {
            Ok(())=>println!("File {} has been unziped", file_path.to_str().unwrap()),
            Err(e)=> println!("Error: {}", e)
        }
    }   

    Ok(conf_common)
}


fn get_zip_from_ftp(path_zip:&str, ftp_conf:&FtpConf, file_patterns:&[String]) -> Result<(), Box<dyn Error>> {
    for ftp_ip_port in &ftp_conf.ip{
        let mut ftp_stream = FtpStream::connect(ftp_ip_port)?;
        ftp_stream.login(&ftp_conf.user,&ftp_conf.pass)?;
        let working_dir = &ftp_conf.work_dir;
        ftp_stream.cwd(working_dir)?;
        let files = ftp_stream.nlst(Some(working_dir))?;

        for file in files {
            let only_file = Path::new(&file).file_name().unwrap().to_str().unwrap();

            let mut is_match_file = false;

            for file_pattern in file_patterns {
                let pattern = Regex::new(&file_pattern)?;
                if pattern.is_match(only_file) {
                    is_match_file = true;
                    break;
                }
            }

            if is_match_file {
                let cursor = ftp_stream.simple_retr(&file)?;
                       
                let mut zip_file = File::create(Path::new(path_zip).with_file_name(&only_file))?;
                
                let result = zip_file.write_all(&cursor.into_inner());
    
                match result {
                    Ok(())=>println!("File {} has benn downloaded!", file),
                    Err(e)=>println!("File download error: {}",e)
                }
            }
            
        }
    }

    Ok(())
  
}

fn unzip(tar_gz_path: &str, unzip_dir_path: &str) ->Result<(),Box<dyn Error>> {
    let tar_gz = File::open(tar_gz_path)?;
    let tar = GzDecoder::new(tar_gz);
    let mut archive = Archive::new(tar);
    archive.unpack(unzip_dir_path)?;
   
    Ok(())
}

pub fn del_all_files_in_dir(dir_path:&str) -> Result<(),String>{

    let paths = std::fs::read_dir(dir_path).unwrap();
    
    for file in paths{
        let path = file.unwrap().path();
        let file_str = path.to_str().unwrap();

        match std::fs::remove_dir_all(file_str) {
            Ok(_)=>(),
            Err(_e)=> ()
        }
        match std::fs::remove_file(file_str) {
            Ok(_)=>(),
            Err(_e)=>()
        }
                
    }

    match std::fs::read_dir(dir_path).unwrap().next().is_none() {
        true =>Ok(()),
        false => Err(format!("Dir {} is not empty!", dir_path))
    }

}

// pub fn reaf_text_file_in_vec(file_path: &str) -> Vec::<String> {
//     let file = std::fs::File::open(file_path).expect("No such file {}", file_path);
//     let buff = BufReader::new(file);
//     buff.lines()
//         .map(|n| n.expext)
// }

fn lines_from_file(filename: impl AsRef<Path>) -> std::io::Result<Vec<String>> {
    BufReader::new(File::open(filename)?).lines().collect()
}

fn lines_from_file1(filename: &str) -> std::io::Result<Vec<String>> {

    let file = BufReader::new(File::open(filename)?);
    Ok(file.lines().map(|x| x.unwrap()).collect())
}

