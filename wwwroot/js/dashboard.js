/**
 * CORE DASHBOARD & NAVIGATION MODULE
 * Features: Navigation, Stats, Search, RBAC Security
 */

// --- 1. SECURITY & RBAC INITIALIZATION ---
(function initSecurity() {
    // A. Check Login Status
    if (sessionStorage.getItem('isLoggedIn') !== 'true') {
        window.location.href = 'index.html'; // Redirect to login
        return;
    }

    // B. Apply User Roles
    const role = sessionStorage.getItem('userRole') || 'Staff';
    const userName = sessionStorage.getItem('userName') || 'User';

    // Update UI Names when DOM is ready
    document.addEventListener("DOMContentLoaded", () => {
        const nameEl = document.getElementById('userName');
        const roleEl = document.getElementById('userRole');
        if (nameEl) nameEl.innerText = userName;
        if (roleEl) roleEl.innerText = role;

        // --- RBAC ENFORCEMENT ---
        // If user is NOT an Administrator, hide sensitive sections
        if (role !== 'Administrator') {

            // 1. Hide Sidebar Links
            const restrictedNavs = ['financialsLink', 'settingsLink', 'reportsLink', 'lbl-admin', 'staffLink'];
            restrictedNavs.forEach(id => {
                const el = document.getElementById(id);
                if (el) el.style.display = 'none';
            });

            // 2. Hide Dashboard Financial Card (TARGET PARENT ELEMENT)
            // We use a CSS trick to hide the 4th stat card specifically if it's the financial one
            const finCard = document.getElementById('statTotalCollected');
            if (finCard) {
                // Traverse up to find the .stat-card wrapper and hide it
                // structure: .stat-card > .stat-value(id)
                finCard.closest('.stat-card').style.display = 'none';
            }

            // 3. Hide Sensitive Action Buttons (Static HTML)
            const restrictedButtons = [
                'action-payment',       // Post Payment
                'btn-add-room',         // Add New Room (Header)
                'btn-add-staff',        // Add Staff
                'btn-quick-learner',    // Quick: New Learner
                'btn-reg-learner'       // Learners View: Register
            ];
            restrictedButtons.forEach(id => {
                const el = document.getElementById(id);
                if (el) el.style.display = 'none';
            });

            // 4. Force Hide Dynamic Elements via CSS Injection
            // This catches elements created by other JS files (like rooms.js New Block button)
            const style = document.createElement('style');
            style.innerHTML = `
                button[onclick="openAddBlockModal()"] { display: none !important; }
                button[onclick="openAddRoomModal()"] { display: none !important; }
            `;
            document.head.appendChild(style);
        }
    });
})();

// Helper: Check if user is Admin (used by other scripts)
function isAdmin() {
    return sessionStorage.getItem('userRole') === 'Administrator';
}

// --- 2. NAVIGATION LOGIC ---
function showView(viewId, element) {
    // Hide all sections
    document.querySelectorAll('.view-section').forEach(v => {
        v.style.display = 'none';
    });

    // Show target section
    const target = document.getElementById('view-' + viewId);
    if (target) {
        target.style.display = 'block';
    } else {
        console.error("Target section NOT found:", 'view-' + viewId);
    }

    // Update Title
    const titleEl = document.getElementById('viewTitle');
    if (titleEl) titleEl.innerText = viewId.replace(/-/g, ' ').toUpperCase();

    // Update Sidebar Active State
    if (element) {
        document.querySelectorAll('.nav-item').forEach(i => i.classList.remove('active'));
        element.classList.add('active');
    }

    // Module Auto-Loading
    switch (viewId) {
        case 'overview':
            if (typeof updateStats === 'function') updateStats();
            if (typeof loadNotices === 'function') loadNotices();
            break;
        case 'rooms':
            if (typeof loadRoomMapping === 'function') loadRoomMapping();
            break;
        case 'learners':
            if (typeof loadLearners === 'function') {
                if (typeof loadRoomsForSelect === 'function') loadRoomsForSelect();
                loadLearners();
            }
            break;
        case 'financials':
            if (typeof loadPaymentLedger === 'function') loadPaymentLedger();
            break;
        case 'staff':
            if (typeof loadStaff === 'function') loadStaff();
            break;
        case 'discipline':
            if (typeof loadDisciplineLearners === 'function') loadDisciplineLearners();
            break;
        case 'leave':
            if (typeof loadAttendance === 'function') loadAttendance();
            break;
        case 'settings':
            if (typeof initSettings === 'function') initSettings();
            break;
        case 'parents':
            if (typeof loadParents === 'function') loadParents();
            break;
        case 'inventory':
            if (typeof loadInventory === 'function') loadInventory();
            break;
    }
}

// --- 3. DASHBOARD STATS ---
async function updateStats() {
    try {
        const res = await fetch('/api/Dashboard/stats');
        const stats = await res.json();

        // 1. Capacity & Availability
        const elCap = document.getElementById('statCap');
        const elAvail = document.getElementById('statAvail');
        if (elCap) elCap.innerText = stats.totalCapacity;
        if (elAvail) elAvail.innerText = stats.totalCapacity - (Math.round(stats.totalCapacity * (stats.occupancyRate / 100)) || 0); // Approx based on rate if needed, or calc exact if API sends occupied count.
        // Actually API sends TotalCapacity and OccupancyRate.
        // Let's rely on what we have or better yet, let's keep it simple.
        // The API returns TotalCapacity. We can calculate available if we had occupied.
        // Wait, the API returns OccupancyRate, TotalStudents.
        // Available = TotalCapacity - (OccupancyRate/100 * TotalCapacity)? No, TotalStudents is simpler.
        if (elAvail) elAvail.innerText = stats.totalCapacity - stats.totalStudents; // Assuming 1 student = 1 bed


        // 2. Maintenance
        const elIssues = document.getElementById('openIssuesCount');
        if (elIssues) elIssues.innerText = stats.pendingMaintenance;

        // 3. New Stats
        const elStock = document.getElementById('statLowStock');
        if (elStock) elStock.innerText = stats.lowStockItems;

        // 4. Financials (Still fetch separately as it might have specific security/logic)
        if (isAdmin()) {
            try {
                const finRes = await fetch('/api/Financials/summary');
                const finData = await finRes.json();

                const currency = typeof getCurrency === 'function' ? getCurrency() : "N$";
                const total = finData.total || finData.TotalIncome || 0;

                const elFin = document.getElementById('statTotalCollected');
                if (elFin) elFin.innerText = `${currency} ${total.toLocaleString('en-US', { minimumFractionDigits: 2 })}`;
            } catch (e) { console.warn("Financial stats failed", e); }
        } else {
            const elFin = document.getElementById('statTotalCollected');
            if (elFin && elFin.closest('.stat-card')) {
                elFin.closest('.stat-card').style.display = 'none';
            }
        }

    } catch (err) { console.error("Stats Error:", err); }
}

// --- 4. GLOBAL SEARCH ---
let searchTimeout;
let searchCache = null;
let searchCacheTime = 0;
const CACHE_DURATION = 60000; // 60 seconds

async function globalSearch(term) {
    if (searchTimeout) clearTimeout(searchTimeout);

    if (term.length < 2) {
        const existing = document.getElementById('search-results-dropdown');
        if (existing) existing.remove();
        return;
    }

    // Debounce: 300ms delay before fetching
    searchTimeout = setTimeout(async () => {
        try {
            let learners = [];
            const now = Date.now();

            // Optimization: Use Client-Side Cache
            if (searchCache && (now - searchCacheTime < CACHE_DURATION)) {
                learners = searchCache;
            } else {
                const res = await fetch('/api/learner/list-all');
                const data = await res.json();
                learners = Array.isArray(data) ? data : (data.value || []);

                // Update Cache
                searchCache = learners;
                searchCacheTime = now;
            }

            const filtered = learners.filter(s =>
                (s.name || '').toLowerCase().includes(term.toLowerCase()) ||
                (s.adNo || '').toLowerCase().includes(term.toLowerCase())
            );

            displaySearchResults(filtered);
        } catch (err) { console.error("Search Error:", err); }
    }, 300);
}

function displaySearchResults(results) {
    let existing = document.getElementById('search-results-dropdown');
    if (!existing) {
        existing = document.createElement('div');
        existing.id = 'search-results-dropdown';
        existing.style.cssText = "position:absolute; top:100%; left:0; right:0; background:white; border:1px solid #ddd; border-radius:8px; box-shadow:0 4px 12px rgba(0,0,0,0.1); z-index:1000; max-height:300px; overflow-y:auto;";
        document.querySelector('.search-box').appendChild(existing);
    }

    if (results.length === 0) {
        existing.innerHTML = '<div style="padding:15px; color:#64748b; font-size:14px;">No learners found.</div>';
    } else {
        existing.innerHTML = results.map(s => `
            <div onclick="jumpToLearner(${s.id})" style="padding:12px 15px; border-bottom:1px solid #f1f5f9; cursor:pointer; transition:background 0.2s;" onmouseover="this.style.background='#f8fafc'" onmouseout="this.style.background='white'">
                <div style="font-weight:700; font-size:14px;">${s.name}</div>
                <div style="font-size:12px; color:#64748b;">AdNo: ${s.adNo} | Room: ${s.room}</div>
            </div>
        `).join('');
    }
}

function jumpToLearner(id) {
    document.getElementById('search-results-dropdown')?.remove();
    document.querySelector('.search-box input').value = "";

    const learnersNav = document.querySelector('[onclick*="showView(\'learners\'"]');
    if (learnersNav) showView('learners', learnersNav);
}

// --- 5. UTILITIES ---
function startClock() {
    setInterval(() => {
        const clockEl = document.getElementById('liveClock');
        if (clockEl) clockEl.innerText = new Date().toLocaleTimeString();
    }, 1000);
}

function logout() {
    if (confirm("Are you sure you want to log out?")) {
        sessionStorage.clear();
        window.location.href = 'index.html';
    }
}

// --- LICENSE MANAGEMENT ---
async function activateLicense() {
    const input = document.getElementById('licenseKeyInput');
    const key = input.value.trim();

    if (!key) return alert("Please enter a license key.");

    const btn = document.querySelector('button[onclick="activateLicense()"]');
    const originalText = btn.innerText;
    btn.innerText = "Verifying...";
    btn.disabled = true;

    try {
        const res = await fetch('/api/License/activate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Key: key })
        });

        const data = await res.json();

        if (res.ok) {
            alert("Success! " + data.message + "\nType: " + data.type);
            input.value = "";
            if (typeof updateStats === 'function') updateStats(); // Refresh dashboard badge
        } else {
            alert("Activation Failed: " + (data.message || "Invalid Key"));
        }
    } catch (err) {
        alert("Connection Error. Please try again.");
    } finally {
        btn.innerText = originalText;
        btn.disabled = false;
    }
}


// Enable keyboard navigation for role="button" elements
document.addEventListener('keydown', function(e) {
    if (e.target.getAttribute('role') === 'button' && (e.key === 'Enter' || e.key === ' ')) {
        e.preventDefault();
        e.target.click();
    }
});

startClock();