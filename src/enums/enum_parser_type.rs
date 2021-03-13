use strum_macros::EnumIter;
use strum_macros::EnumString;
use strum_macros::AsRefStr;
use strum_macros::Display;

#[derive(Debug,EnumIter, PartialEq,EnumString,AsRefStr,Display)]
pub(crate) enum EnumParserType {
    GSM,UMTS,LTE

}