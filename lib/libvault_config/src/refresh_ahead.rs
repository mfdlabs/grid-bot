// Class that handles the refresh ahead functionality
//
// Runs a thread that refreshes the data at a given interval.
// Also ensures thread safety when accessing the data.

use std::{
    sync::{Arc, Mutex},
    thread,
    time::Duration,
};

/// Struct that handles the refresh ahead functionality
pub struct RefreshAhead<T> {
    data: Arc<Mutex<T>>,
    interval: Duration,
}

impl<T> RefreshAhead<T>
where
    T: Clone + Send + Sync + 'static,
{
    /// Creates a new instance of the refresh ahead struct
    ///
    /// # Arguments
    /// * `data` - The data to refresh
    /// * `refresh_func` - The function to use to refresh the data
    /// * `interval` - The interval to refresh the data at
    pub fn new(data: T, refresh_func: impl Fn() -> T + Send + Sync + 'static, interval: Duration) -> Self {
        let data = Arc::new(Mutex::new(data));
        let data_clone = data.clone();

        thread::spawn(move || loop {
            thread::sleep(interval);
            *data.lock().unwrap() = refresh_func();
        });

        Self {
            data: data_clone,
            interval,
        }
    }

    /// Gets the data
    pub fn get(&self) -> T {
        self.data.lock().unwrap().clone()
    }
}

// Implement the Clone trait for RefreshAhead
impl<T> Clone for RefreshAhead<T>
where
    T: Clone + Send + Sync + 'static,
{
    fn clone(&self) -> Self {
        Self {
            data: self.data.clone(),
            interval: self.interval,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_refresh_ahead() {
        let data = RefreshAhead::new(1, || 2, Duration::from_millis(100));
        assert_eq!(data.get(), 1);
        thread::sleep(Duration::from_millis(200));
        assert_eq!(data.get(), 2);
    }
}