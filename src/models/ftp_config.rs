#[derive(Serialize,Deserialize,Debug)]
pub struct FtpConf {
    pub(crate) user:String,
    pub(crate) pass:String,
    pub(crate) ip: Vec<String>,
    pub(crate) work_dir:String,
}