#[derive(Serialize,Deserialize,Debug)]
pub struct InputParam {
    pub parser_type:String,
    pub timestamp:String,
    pub ftp_files_pattern: Vec<String>,
    pub path_app:String,
    pub path_zip:String,
    pub path_log:String,
    pub path_unzip:String
}