use std::f32::consts::PI;

#[repr(C)]
pub struct Vec3 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

#[repr(C)]
pub struct VertexState {
    pub position: Vec3,
    pub velocity: Vec3,
    pub original_pos: Vec3,
}

#[repr(C)]
pub struct PhysicsParams {
    pub stiffness: f32,
    pub damping: f32,
    pub mass: f32,
    pub gravity_influence: f32,
}

struct KalmanFilter {
    q: f32, r: f32, x: f32, p: f32, k: f32,
}

impl KalmanFilter {
    fn new(init_x: f32) -> Self {
        Self { q: 0.01, r: 0.1, x: init_x, p: 1.0, k: 0.0 }
    }
    fn update(&mut self, m: f32) -> f32 {
        self.p += self.q;
        self.k = self.p / (self.p + self.r);
        self.x += self.k * (m - self.x);
        self.p *= 1.0 - self.k;
        self.x
    }
}

#[repr(C)]
pub enum GamePhase {
    Raw = 0,
    Cooking = 1,
    Turned = 2,
    Finished = 3,
}

pub struct RhythmEngine {
    kf_x: KalmanFilter,
    kf_y: KalmanFilter,
    kf_z: KalmanFilter,
    last_error: f32,
    integral: f32,
    pub mastery: f32,
    pub shaping_progress: f32,
    pub batter_level: f32,
    pub game_phase: GamePhase,
    pub combo_count: i32,
    pub stability_timer: f32,
    pub p_term: f32,
    pub i_term: f32,
    pub d_term: f32,
}

impl RhythmEngine {
    fn new() -> Self {
        Self {
            kf_x: KalmanFilter::new(0.0), kf_y: KalmanFilter::new(0.0), kf_z: KalmanFilter::new(0.0),
            last_error: 0.0, integral: 0.0, mastery: 0.0,
            shaping_progress: 1.0, batter_level: 0.0, game_phase: GamePhase::Raw,
            combo_count: 0, stability_timer: 0.0,
            p_term: 0.0, i_term: 0.0, d_term: 0.0,
        }
    }

    fn update(&mut self, x: f32, y: f32, z: f32, dt: f32, target: f32) -> f32 {
        let fx = self.kf_x.update(x);
        let fy = self.kf_y.update(y);
        let fz = self.kf_z.update(z);
        let mag = (fx*fx + fy*fy + fz*fz).sqrt();
        
        let error = target - mag;
        self.integral += error * dt;
        let der = (error - self.last_error) / dt;
        self.last_error = error;
        
        self.p_term = 1.5 * error;
        self.i_term = 0.5 * self.integral;
        self.d_term = 0.2 * der;
        
        if mag > 2.0 {
            let harmony = (1.0 - (output.abs() / target)).clamp(0.0, 1.0);
            let is_perfect = harmony > 0.85;

            if is_perfect {
                self.stability_timer += dt;
                if self.stability_timer > 0.5 {
                    self.combo_count += 1;
                    self.stability_timer = 0.0;
                }
                self.mastery = (self.mastery + dt * 0.5).min(1.0);
            } else {
                self.stability_timer = 0.0;
                if harmony < 0.5 { self.combo_count = 0; }
                self.mastery = (self.mastery - dt * 0.2).max(0.0);
            }

            // Target roughly 20 seconds for full completion at perfect harmony
            let pressure = harmony * (1.0 + self.mastery * 1.5);
            let shaping_speed = 0.05; 
            self.shaping_progress = (self.shaping_progress - pressure * shaping_speed * dt).max(0.0);
        } else {
            self.combo_count = 0;
            self.mastery = (self.mastery - dt * 1.0).max(0.0);
            self.shaping_progress = (self.shaping_progress + dt * 0.02).min(1.0);
        }

        // Automatic Phase Transitions
        match self.game_phase {
            GamePhase::Raw => {
                self.batter_level = (self.batter_level + dt * 0.5).min(1.0);
                if self.batter_level >= 1.0 { self.game_phase = GamePhase::Cooking; }
            },
            GamePhase::Cooking => {
                if self.shaping_progress < 0.5 { self.game_phase = GamePhase::Turned; }
            },
            GamePhase::Turned => {
                if self.shaping_progress <= 0.0 { self.game_phase = GamePhase::Finished; }
            },
            GamePhase::Finished => {},
        }

        mag
    }

    fn step_physics(&self, states: &mut [VertexState], params: &PhysicsParams, gravity: &Vec3, accel: &Vec3, dt: f32) {
        let dt_ratio = dt * 60.0;
        let d_factor = (1.0 - params.damping).powf(dt_ratio);
        for v in states.iter_mut() {
            let dx = v.position.x - v.original_pos.x;
            let dy = v.position.y - v.original_pos.y;
            let dz = v.position.z - v.original_pos.z;
            let mut fx = -dx * params.stiffness;
            let mut fy = -dy * params.stiffness;
            let mut fz = -dz * params.stiffness;
            fx += gravity.x * params.gravity_influence - accel.x * params.mass;
            fy += gravity.y * params.gravity_influence - accel.y * params.mass;
            fz += gravity.z * params.gravity_influence - accel.z * params.mass;
            v.velocity.x += (fx / params.mass) * dt;
            v.velocity.y += (fy / params.mass) * dt;
            v.velocity.z += (fz / params.mass) * dt;
            v.velocity.x *= d_factor; v.velocity.y *= d_factor; v.velocity.z *= d_factor;
            v.position.x += v.velocity.x * dt;
            v.position.y += v.velocity.y * dt;
            v.position.z += v.velocity.z * dt;
            let cur_dx = v.position.x - v.original_pos.x;
            let cur_dy = v.position.y - v.original_pos.y;
            let cur_dz = v.position.z - v.original_pos.z;
            let dist_sq = cur_dx*cur_dx + cur_dy*cur_dy + cur_dz*cur_dz;
            if dist_sq > 0.09 {
                let d = dist_sq.sqrt();
                v.position.x = v.original_pos.x + (cur_dx / d) * 0.3;
                v.position.y = v.original_pos.y + (cur_dy / d) * 0.3;
                v.position.z = v.original_pos.z + (cur_dz / d) * 0.3;
                v.velocity.x *= 0.1; v.velocity.y *= 0.1; v.velocity.z *= 0.1;
            }
        }
    }
}

// --- Bridge ---

#[no_mangle]
pub extern "C" fn tako_init() -> *mut RhythmEngine {
    Box::into_raw(Box::new(RhythmEngine::new()))
}

#[no_mangle]
pub extern "C" fn tako_update(e: *mut RhythmEngine, gx: f32, gy: f32, gz: f32, dt: f32, target: f32) -> f32 {
    let e = unsafe { &mut *e };
    e.update(gx, gy, gz, dt, target)
}

#[no_mangle]
pub extern "C" fn tako_get_pid_terms(e: *mut RhythmEngine, p: *mut f32, i: *mut f32, d: *mut f32) {
    let e = unsafe { &*e };
    unsafe {
        *p = e.p_term;
        *i = e.i_term;
        *d = e.d_term;
    }
}

#[no_mangle]
pub extern "C" fn tako_step_physics(e: *mut RhythmEngine, states: *mut VertexState, count: i32, params: *const PhysicsParams, g: *const Vec3, a: *const Vec3, dt: f32) {
    let e = unsafe { &mut *e };
    let s_slice = unsafe { std::slice::from_raw_parts_mut(states, count as usize) };
    let p = unsafe { &*params };
    let g_vec = unsafe { &*g };
    let a_vec = unsafe { &*a };
    e.step_physics(s_slice, p, g_vec, a_vec, dt);
}

#[no_mangle]
pub extern "C" fn tako_get_mastery(e: *mut RhythmEngine) -> f32 {
    let e = unsafe { &*e }; e.mastery
}

#[no_mangle]
pub extern "C" fn tako_get_progress(e: *mut RhythmEngine) -> f32 {
    let e = unsafe { &*e }; e.shaping_progress
}

#[no_mangle]
pub extern "C" fn tako_get_combo(e: *mut RhythmEngine) -> i32 {
    let e = unsafe { &*e }; e.combo_count
}

#[no_mangle]
pub extern "C" fn tako_free(e: *mut RhythmEngine) {
    if !e.is_null() { unsafe { let _ = Box::from_raw(e); } }
}
