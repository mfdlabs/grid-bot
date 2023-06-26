#[macro_use]
extern crate log;

fn main() {

    env_logger::init();

    savon::gen::gen_write("./wsdl/RCCService.wsdl", "./abc");
}
