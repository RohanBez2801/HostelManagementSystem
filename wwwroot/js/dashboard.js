/**
 * CORE DASHBOARD & NAVIGATION MODULE
 */

// 1. Navigation Logic
function showView(viewId, element) {
    // Hide all sections
    document.querySelectorAll('.view-section').forEach(v => v.style.display = 'none');

    // Show target section
    const target = document.getElementById('view-' + viewId);
    if (target) target.style.display = 'block';

    // Update Title
    document.getElementById('viewTitle').innerText = viewId.replace(/-/g, ' ').toUpperCase();

    // Update Sidebar Active State
    if (element) {
        document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
        element.classList.add('active');
    }

    // Module Auto-Loading
    // This triggers functions found in your other JS files
    switch (viewId) {
        case 'overview': updateStats(); break;
        case 'rooms': if (typeof loadRoomMapping === 'function') loadRoomMapping(); break;
        case 'learners': if (typeof loadLearners === 'function') { loadRooms(); loadLearners(); } break;
        case 'financials': if (typeof loadPaymentLedger === 'function') loadPaymentLedger(); break;
    }
}

// 2. Dashboard Stats (The Top Cards)
async function updateStats() {
    try {
        // We fetch room data to calculate occupancy
        const res = await fetch('/api/Room/all');
        const rooms = await res.json();

        let totalCap = 0;
        let totalOcc = 0;

        rooms.forEach(r => {
            totalCap += r.capacity;
            totalOcc += r.occupied;
        });

        // Update UI Cards
        document.getElementById('statCap').innerText = totalCap;
        document.getElementById('statAvail').innerText = totalCap - totalOcc;

        // Visual warning if hostel is almost full
        const availElement = document.getElementById('statAvail');
        availElement.style.color = (totalCap - totalOcc) <= 5 ? "#ef4444" : "#10b981";

    } catch (err) {
        console.error("Dashboard Stats Error:", err);
    }
}

// 3. Global Search Logic
function globalSearch(term) {
    if (term.length < 2) return;
    // Example: This can be expanded to jump to a specific student profile
    console.log("Global searching for:", term);
}

// 4. Utilities (Clock & Logout)
function startClock() {
    setInterval(() => {
        const clockEl = document.getElementById('liveClock');
        if (clockEl) clockEl.innerText = new Date().toLocaleTimeString();
    }, 1000);
}

function logout() {
    if (confirm("Are you sure you want to log out?")) {
        sessionStorage.clear();
        window.location.href = 'login.html';
    }
}

// Initialize components that aren't data-dependent
startClock();