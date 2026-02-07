// Takoyaki Soul: Zen Mastery Edition - Web Version
// Game State Management

class TakoyakiGame {
    constructor() {
        this.canvas = document.getElementById('game-canvas');
        this.ctx = this.canvas.getContext('2d');
        
        // Game state
        this.state = 'title'; // title, playing, result
        this.shapeProgress = 100; // Start at 100, decrease to 0
        this.cookLevel = 0; // 0 to 100
        this.mastery = 0; // 0 to 100
        
        // Physics
        this.takoyakiX = 0;
        this.takoyakiY = 0;
        this.takoyakiRadius = 40;
        this.rotation = 0;
        this.rotationSpeed = 0;
        this.deformAmount = 1.0; // 1.0 = liquid, 0.0 = perfect sphere
        
        // Input
        this.mouseDown = false;
        this.lastMouseX = 0;
        this.lastMouseY = 0;
        this.currentRotationRate = 0; // Current rotation speed
        
        // Toppings
        this.toppings = [];
        this.hasSauce = false;
        this.hasMayo = false;
        this.hasAonori = false;
        this.hasKatsuobushi = false;
        
        // Timing
        this.lastTime = Date.now();
        this.targetRotationSpeed = 6.0; // Target rotation (similar to Unity version)
        
        this.initCanvas();
        this.setupEventListeners();
        this.gameLoop();
    }
    
    initCanvas() {
        this.resizeCanvas = () => {
            const container = this.canvas.parentElement;
            this.canvas.width = container.clientWidth;
            this.canvas.height = container.clientHeight;
            this.takoyakiX = this.canvas.width / 2;
            this.takoyakiY = this.canvas.height / 2;
        };
        // Don't call resizeCanvas() here - will be called when game starts
        window.addEventListener('resize', this.resizeCanvas);
    }
    
    setupEventListeners() {
        // Title screen
        document.getElementById('start-button').addEventListener('click', () => {
            this.startGame();
        });
        
        // Game controls
        this.canvas.addEventListener('mousedown', (e) => {
            this.mouseDown = true;
            this.lastMouseX = e.clientX;
            this.lastMouseY = e.clientY;
        });
        
        this.canvas.addEventListener('mousemove', (e) => {
            if (this.mouseDown && this.state === 'playing') {
                const deltaX = e.clientX - this.lastMouseX;
                const deltaY = e.clientY - this.lastMouseY;
                
                // Calculate rotation speed based on mouse movement
                this.currentRotationRate = Math.sqrt(deltaX * deltaX + deltaY * deltaY) * 0.1;
                this.rotationSpeed = deltaX * 0.02;
                
                this.lastMouseX = e.clientX;
                this.lastMouseY = e.clientY;
            }
        });
        
        this.canvas.addEventListener('mouseup', () => {
            this.mouseDown = false;
            this.currentRotationRate *= 0.5; // Decay
        });
        
        this.canvas.addEventListener('mouseleave', () => {
            this.mouseDown = false;
        });
        
        // Touch support
        this.canvas.addEventListener('touchstart', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            this.mouseDown = true;
            this.lastMouseX = touch.clientX;
            this.lastMouseY = touch.clientY;
        });
        
        this.canvas.addEventListener('touchmove', (e) => {
            e.preventDefault();
            if (this.mouseDown && this.state === 'playing') {
                const touch = e.touches[0];
                const deltaX = touch.clientX - this.lastMouseX;
                const deltaY = touch.clientY - this.lastMouseY;
                
                this.currentRotationRate = Math.sqrt(deltaX * deltaX + deltaY * deltaY) * 0.1;
                this.rotationSpeed = deltaX * 0.02;
                
                this.lastMouseX = touch.clientX;
                this.lastMouseY = touch.clientY;
            }
        });
        
        this.canvas.addEventListener('touchend', () => {
            this.mouseDown = false;
            this.currentRotationRate *= 0.5;
        });
        
        // Shake/Toppings button
        document.getElementById('shake-button').addEventListener('click', () => {
            this.addToppings();
        });
        
        // Restart
        document.getElementById('restart-button').addEventListener('click', () => {
            this.resetGame();
            this.startGame();
        });
    }
    
    startGame() {
        this.state = 'playing';
        this.showScreen('game-screen');
        // Initialize canvas size now that the game screen is visible
        this.resizeCanvas();
    }
    
    resetGame() {
        this.shapeProgress = 100;
        this.cookLevel = 0;
        this.mastery = 0;
        this.rotation = 0;
        this.rotationSpeed = 0;
        this.deformAmount = 1.0;
        this.toppings = [];
        this.hasSauce = false;
        this.hasMayo = false;
        this.hasAonori = false;
        this.hasKatsuobushi = false;
    }
    
    showScreen(screenId) {
        document.querySelectorAll('.screen').forEach(s => s.classList.remove('active'));
        document.getElementById(screenId).classList.add('active');
    }
    
    addToppings() {
        // Add random toppings based on progress
        const types = [];
        
        if (!this.hasSauce && this.shapeProgress < 80) {
            types.push('sauce');
        }
        if (!this.hasMayo && this.shapeProgress < 60) {
            types.push('mayo');
        }
        if (!this.hasAonori && this.shapeProgress < 40) {
            types.push('aonori');
        }
        if (!this.hasKatsuobushi && this.shapeProgress < 20) {
            types.push('katsuobushi');
        }
        
        if (types.length > 0) {
            const type = types[0]; // Add in order
            
            // Create topping particles
            for (let i = 0; i < 20; i++) {
                this.toppings.push({
                    x: Math.random() * this.canvas.width,
                    y: -20,
                    vx: (Math.random() - 0.5) * 2,
                    vy: Math.random() * 3 + 2,
                    type: type,
                    alpha: 1.0,
                    size: Math.random() * 3 + 2
                });
            }
            
            // Mark as added
            if (type === 'sauce') this.hasSauce = true;
            if (type === 'mayo') this.hasMayo = true;
            if (type === 'aonori') this.hasAonori = true;
            if (type === 'katsuobushi') this.hasKatsuobushi = true;
        }
    }
    
    update(deltaTime) {
        if (this.state !== 'playing') return;
        
        // Update rotation
        this.rotation += this.rotationSpeed;
        this.rotationSpeed *= 0.95; // Friction
        
        // Decay current rotation rate
        this.currentRotationRate *= 0.9;
        
        // Check if rotation is near target
        const rotationDiff = Math.abs(this.currentRotationRate - this.targetRotationSpeed);
        const isGoodRotation = rotationDiff < 3.0 && this.currentRotationRate > 2.0;
        
        // Update shaping progress
        if (isGoodRotation) {
            this.shapeProgress = Math.max(0, this.shapeProgress - deltaTime * 8); // Faster shaping
            this.mastery = Math.min(100, this.mastery + deltaTime * 20);
        } else {
            this.mastery = Math.max(0, this.mastery - deltaTime * 10);
        }
        
        // Update cook level (always cooking)
        this.cookLevel = Math.min(100, this.cookLevel + deltaTime * 3);
        
        // Update deform amount based on shape progress
        this.deformAmount = this.shapeProgress / 100;
        
        // Update toppings
        for (let i = this.toppings.length - 1; i >= 0; i--) {
            const t = this.toppings[i];
            t.x += t.vx;
            t.y += t.vy;
            t.vy += 0.1; // Gravity
            
            // Remove if off screen
            if (t.y > this.canvas.height + 50) {
                this.toppings.splice(i, 1);
            }
        }
        
        // Update feedback text
        const feedback = document.getElementById('rotation-feedback');
        if (isGoodRotation) {
            feedback.textContent = '✓ Perfect Rotation!';
            feedback.style.color = '#4ade80';
        } else if (this.currentRotationRate < 2.0) {
            feedback.textContent = '⚠ Rotate faster...';
            feedback.style.color = '#fbbf24';
        } else {
            feedback.textContent = '⚠ Too fast! Slow down...';
            feedback.style.color = '#f87171';
        }
        
        // Update HUD
        document.getElementById('shaping-bar').style.width = this.shapeProgress + '%';
        document.getElementById('cooking-bar').style.width = this.cookLevel + '%';
        document.getElementById('mastery-bar').style.width = this.mastery + '%';
        
        // Check for completion
        if (this.shapeProgress <= 0 && this.cookLevel > 30) {
            this.endGame();
        }
    }
    
    render() {
        // Clear canvas
        this.ctx.fillStyle = '#0f1419';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        if (this.state !== 'playing') return;
        
        // Draw pan (background)
        this.ctx.fillStyle = '#2d2d2d';
        this.ctx.beginPath();
        this.ctx.arc(this.takoyakiX, this.takoyakiY, this.takoyakiRadius + 20, 0, Math.PI * 2);
        this.ctx.fill();
        
        // Draw takoyaki with deformation
        this.ctx.save();
        this.ctx.translate(this.takoyakiX, this.takoyakiY);
        this.ctx.rotate(this.rotation);
        
        // Deformed ellipse
        const scaleX = 1 + this.deformAmount * 0.3;
        const scaleY = 1 - this.deformAmount * 0.3;
        
        this.ctx.scale(scaleX, scaleY);
        
        // Color based on cook level
        const cookRatio = this.cookLevel / 100;
        const r = Math.floor(139 + cookRatio * 100);
        const g = Math.floor(69 + cookRatio * 100);
        const b = 19;
        
        // Gradient for 3D sphere effect - offset highlight to top-left
        const highlightOffsetX = -this.takoyakiRadius * 0.3;
        const highlightOffsetY = -this.takoyakiRadius * 0.3;
        const gradient = this.ctx.createRadialGradient(
            highlightOffsetX, highlightOffsetY, this.takoyakiRadius * 0.1,
            0, 0, this.takoyakiRadius
        );
        gradient.addColorStop(0, `rgb(${Math.min(255, r + 50)}, ${Math.min(255, g + 50)}, ${Math.min(255, b + 30)})`);
        gradient.addColorStop(0.4, `rgb(${Math.min(255, r + 20)}, ${Math.min(255, g + 20)}, ${b})`);
        gradient.addColorStop(1, `rgb(${Math.max(0, r - 30)}, ${Math.max(0, g - 30)}, ${Math.max(0, b - 10)})`);
        
        this.ctx.fillStyle = gradient;
        this.ctx.beginPath();
        this.ctx.arc(0, 0, this.takoyakiRadius, 0, Math.PI * 2);
        this.ctx.fill();
        
        // Zen glow when mastery is high
        if (this.mastery > 70) {
            this.ctx.strokeStyle = `rgba(255, 215, 0, ${(this.mastery - 70) / 30})`;
            this.ctx.lineWidth = 3;
            this.ctx.beginPath();
            this.ctx.arc(0, 0, this.takoyakiRadius + 5, 0, Math.PI * 2);
            this.ctx.stroke();
        }
        
        this.ctx.restore();
        
        // Draw toppings
        for (const t of this.toppings) {
            this.ctx.save();
            this.ctx.globalAlpha = t.alpha;
            
            if (t.type === 'sauce') {
                this.ctx.fillStyle = '#8B4513';
            } else if (t.type === 'mayo') {
                this.ctx.fillStyle = '#FFF8DC';
            } else if (t.type === 'aonori') {
                this.ctx.fillStyle = '#228B22';
            } else if (t.type === 'katsuobushi') {
                this.ctx.fillStyle = '#DEB887';
            }
            
            this.ctx.fillRect(t.x, t.y, t.size, t.size * 2);
            this.ctx.restore();
        }
        
        // Screen edge glow when mastery is high
        if (this.mastery > 80) {
            const glowAlpha = (this.mastery - 80) / 20 * 0.3;
            this.ctx.fillStyle = `rgba(255, 215, 0, ${glowAlpha})`;
            this.ctx.fillRect(0, 0, this.canvas.width, 10);
            this.ctx.fillRect(0, this.canvas.height - 10, this.canvas.width, 10);
            this.ctx.fillRect(0, 0, 10, this.canvas.height);
            this.ctx.fillRect(this.canvas.width - 10, 0, 10, this.canvas.height);
        }
    }
    
    endGame() {
        this.state = 'result';
        this.showScreen('result-screen');
        
        // Calculate scores
        const shapeScore = Math.max(0, 100 - this.shapeProgress);
        const cookScore = Math.min(100, this.cookLevel);
        const masteryBonus = this.mastery;
        
        const totalScore = Math.floor((shapeScore * 0.4 + cookScore * 0.3 + masteryBonus * 0.3));
        
        // Update result display
        document.getElementById('total-score').textContent = totalScore;
        document.getElementById('shape-score').textContent = Math.floor(shapeScore) + '%';
        document.getElementById('cook-score').textContent = Math.floor(cookScore) + '%';
        
        // Generate comment
        let comment = '';
        if (totalScore >= 90) {
            comment = '🦀 究極の職人技！ / Ultimate Mastery!';
        } else if (totalScore >= 75) {
            comment = '🔥 素晴らしい調和！ / Excellent Harmony!';
        } else if (totalScore >= 60) {
            comment = '👍 良い出来！ / Good Work!';
        } else if (totalScore >= 40) {
            comment = '💪 もう少し！ / Keep Practicing!';
        } else {
            comment = '😅 修行が必要... / More Training Needed...';
        }
        
        document.getElementById('comment').textContent = comment;
    }
    
    gameLoop() {
        const now = Date.now();
        const deltaTime = (now - this.lastTime) / 1000;
        this.lastTime = now;
        
        this.update(deltaTime);
        this.render();
        
        requestAnimationFrame(() => this.gameLoop());
    }
}

// Initialize game when page loads
window.addEventListener('DOMContentLoaded', () => {
    new TakoyakiGame();
});
