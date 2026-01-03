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
            const restrictedNavs = ['financialsLink', 'settingsLink', 'reportsLink', 'lbl-admin', 'staffLink'];
            restrictedNavs.forEach(id => {
                const el = document.getElementById(id);
                if (el) el.style.display = 'none';
            });

            const finCard = document.getElementById('statTotalCollected');
            if (finCard) {
                finCard.closest('.stat-card').style.display = 'none';
            }

            const restrictedButtons = [
                'action-payment', 'btn-add-room', 'btn-add-staff', 'btn-quick-learner', 'btn-reg-learner'
            ];
            restrictedButtons.forEach(id => {
                const el = document.getElementById(id);
                if (el) el.style.display = 'none';
            });

            const style = document.createElement('style');
            style.innerHTML = `
                button[onclick="openAddBlockModal()"] { display: none !important; }
                button[onclick="openAddRoomModal()"] { display: none !important; }
            `;
            document.head.appendChild(style);
        }
    });
})();

// Helper: Check if user is Admin
function isAdmin() {
    return sessionStorage.getItem('userRole') === 'Administrator';
}

// --- 2. NAVIGATION LOGIC ---
function showView(viewId, element) {
    document.querySelectorAll('.view-section').forEach(v => {
        v.style.display = 'none';
    });

    const target = document.getElementById('view-' + viewId);
    if (target) {
        target.style.display = 'block';
    } else {
        console.error("Target section NOT found:", 'view-' + viewId);
    }

    const titleEl = document.getElementById('viewTitle');
    if (titleEl) titleEl.innerText = viewId.replace(/-/g, ' ').toUpperCase();

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
        case 'dining':
            if (typeof loadDiningLog === 'function') loadDiningLog();
            break;
        case 'communication':
            if (typeof loadCommunicationLog === 'function') loadCommunicationLog();
            break;
        case 'events':
            if (typeof loadEvents === 'function') loadEvents();
            break;
    }
}

// --- 3. DASHBOARD STATS ---
async function updateStats() {
    try {
        const res = await fetch('/api/Dashboard/stats');
        
        // Robustness: Check if response is valid JSON
        const contentType = res.headers.get("content-type");
        if (!res.ok || !contentType || !contentType.includes("application/json")) {
            throw new Error("Invalid API Response");
        }

        const stats = await res.json();

        // 1. Capacity
        const elCap = document.getElementById('statCap');
        const elAvail = document.getElementById('statAvail');
        if (elCap) elCap.innerText = stats.totalCapacity || 0;
        if (elAvail) elAvail.innerText = (stats.totalCapacity || 0) - (stats.totalStudents || 0);

        // 2. Maintenance
        const elIssues = document.getElementById('openIssuesCount');
        if (elIssues) elIssues.innerText = stats.pendingMaintenance || 0;

        // 3. Low Stock
        const elStock = document.getElementById('statLowStock');
        if (elStock) elStock.innerText = stats.lowStockItems || 0;
        
        // 4. License Status (New Tile)
        const elLic = document.getElementById('statLicense');
        if (elLic) {
            const status = stats.licenseStatus || "Unknown";
            const days = stats.licenseDaysLeft || 0;
            if (status === 'Active') {
                elLic.innerHTML = `<span style="color:#10b981; font-weight:bold;"><i class="fas fa-check-circle"></i> Active (${days} days)</span>`;
            } else {
                elLic.innerHTML = `<span style="color:#ef4444; font-weight:bold;"><i class="fas fa-exclamation-triangle"></i> ${status}</span>`;
            }
        }

        // 5. Financials (Admin Only)
        if (isAdmin()) {
            try {
                const finRes = await fetch('/api/Financials/summary');
                if (finRes.ok) {
                    const finData = await finRes.json();
                    const currency = typeof getCurrency === 'function' ? getCurrency() : "N$";
                    const total = finData.total || finData.TotalIncome || 0;
                    const elFin = document.getElementById('statTotalCollected');
                    if (elFin) elFin.innerText = `${currency} ${total.toLocaleString('en-US', { minimumFractionDigits: 2 })}`;
                }
            } catch (e) { console.warn("Financial stats failed", e); }
        }

    } catch (err) { console.error("Stats Error:", err); }
}

// --- 4. GLOBAL SEARCH ---
let searchCache = { data: null, timestamp: 0 };
let searchDebounceTimer;

async function globalSearch(term) {
    clearTimeout(searchDebounceTimer);

    if (term.length < 2) {
        document.getElementById('search-results-dropdown')?.remove();
        return;
    }

    // Debounce: Wait 300ms before processing
    searchDebounceTimer = setTimeout(async () => {
        try {
            console.time("Search Duration");
            let learners = [];

            // Cache Strategy: Use cache if < 60 seconds old
            const now = Date.now();
            if (searchCache.data && (now - searchCache.timestamp < 60000)) {
                learners = searchCache.data;
                console.log("Search: Cache Hit");
            } else {
                console.log("Search: Fetching from Server...");
                const res = await fetch('/api/learner/list-all');
                if(!res.ok) return;

                const data = await res.json();
                learners = Array.isArray(data) ? data : (data.value || []);

                // Update Cache
                searchCache = { data: learners, timestamp: now };
            }

            const filtered = learners.filter(s =>
                (s.name || '').toLowerCase().includes(term.toLowerCase()) ||
                (s.adNo || '').toLowerCase().includes(term.toLowerCase())
            );

            displaySearchResults(filtered);
            console.timeEnd("Search Duration");
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

// Global Modal Helper (Used by Reports etc)
function openModalContent(html) {
    let existing = document.getElementById('dynamicModal');
    if (existing) existing.remove();

    const modal = document.createElement('div');
    modal.id = 'dynamicModal';
    modal.className = 'modal-overlay';
    modal.style.display = 'flex';
    modal.innerHTML = `
        <div class="modal-content" style="max-width:800px; width:90%; max-height:90vh; overflow-y:auto;">
            ${html}
        </div>
    `;
    document.body.appendChild(modal);
    
    // Close on click outside
    modal.addEventListener('click', (e) => {
        if (e.target === modal) modal.remove();
    });
}

startClock();
