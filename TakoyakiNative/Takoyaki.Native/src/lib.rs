use std::f32::consts::PI;

/// A simple 1D Kalman Filter for smoothing sensor noise
struct KalmanFilter {
    q: f32, // Process noise covariance
    r: f32, // Measurement noise covariance
    x: f32, // Estimated value
    p: f32, // Estimation error covariance
    k: f32, // Kalman gain
}

impl KalmanFilter {
    fn new(init_x: f32) -> Self {
        Self {
            q: 0.01,
            r: 0.1,
            x: init_x,
            p: 1.0,
            k: 0.0,
        }
    }

    fn update(&mut self, measurement: f32) -> f32 {
        // Prediction update
        self.p = self.p + self.q;

        // Measurement update
        self.k = self.p / (self.p + self.r);
        self.x = self.x + self.k * (measurement - self.x);
        self.p = (1.0 - self.k) * self.p;

        self.x
    }
}

pub struct RhythmEngine {
    kf_x: KalmanFilter,
    kf_y: KalmanFilter,
    kf_z: KalmanFilter,
    
    last_error: f32,
    integral: f32,
    
    mastery: f32,
}

impl RhythmEngine {
    fn new() -> Self {
        Self {
            kf_x: KalmanFilter::new(0.0),
            kf_y: KalmanFilter::new(0.0),
            kf_z: KalmanFilter::new(0.0),
            last_error: 0.0,
            integral: 0.0,
            mastery: 0.0,
        }
    }

    fn update(&mut self, x: f32, y: f32, z: f32, dt: f32, target: f32) -> f32 {
        // 1. Filter raw gyro
        let fx = self.kf_x.update(x);
        let fy = self.kf_y.update(y);
        let fz = self.kf_z.update(z);
        
        // 2. Magnitude of filtered angular velocity
        let magnitude = (fx*fx + fy*fy + fz*fz).sqrt();
        
        // 3. Optimized PID Logic
        let error = target - magnitude;
        self.integral += error * dt;
        let derivative = (error - self.last_error) / dt;
        self.last_error = error;
        
        // Tuned for Rust-level responsiveness
        let p_term = 1.5 * error;
        let i_term = 0.5 * self.integral;
        let d_term = 0.2 * derivative;
        
        let output = p_term + i_term + d_term;
        
        // 4. Mastery Calculation
        let stability = 1.0 - (output.abs() / target).clamp(0.0, 1.0);
        if stability > 0.9 {
            self.mastery = (self.mastery + dt * 0.8).min(1.0);
        } else {
            self.mastery = (self.mastery - dt * 0.4).max(0.0);
        }
        
        magnitude
    }
}

// --- C-ABI Bridge ---

#[no_mangle]
pub extern "C" fn tako_init() -> *mut RhythmEngine {
    Box::into_raw(Box::new(RhythmEngine::new()))
}

#[no_mangle]
pub extern "C" fn tako_update(
    engine: *mut RhythmEngine, 
    gx: f32, gy: f32, gz: f32, 
    dt: f32, target: f32
) -> f32 {
    let engine = unsafe {
        assert!(!engine.is_null());
        &mut *engine
    };
    engine.update(gx, gy, gz, dt, target)
}

#[no_mangle]
pub extern "C" fn tako_get_mastery(engine: *mut RhythmEngine) -> f32 {
    let engine = unsafe {
        assert!(!engine.is_null());
        &mut *engine
    };
    engine.mastery
}

#[no_mangle]
pub extern "C" fn tako_free(engine: *mut RhythmEngine) {
    if !engine.is_null() {
        unsafe {
            let _ = Box::from_raw(engine);
        }
    }
}
