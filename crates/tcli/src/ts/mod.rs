use once_cell::sync::Lazy;
use reqwest::header::{HeaderMap, HeaderValue, ACCEPT, CONTENT_TYPE};
use reqwest::Client;

pub mod experimental;
pub mod package_manifest;
pub mod package_reference;
pub mod v1;
pub mod version;

pub(in crate::ts) const CM: &str = "https://thunderstore.io/c/";
pub(in crate::ts) const V1: &str = "https://thunderstore.io/api/v1";
pub(in crate::ts) const EX: &str = "https://thunderstore.io/api/experimental";

pub(in crate::ts) static CLIENT: Lazy<Client> = Lazy::new(|| {
    let mut header_map = HeaderMap::new();
    header_map.insert(CONTENT_TYPE, HeaderValue::from_static("application/json"));
    header_map.insert(ACCEPT, HeaderValue::from_static("application/json"));

    Client::builder()
        .default_headers(header_map)
        .build()
        .unwrap()
});
