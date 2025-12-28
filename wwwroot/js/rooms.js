/**
 * ROOM & OCCUPANCY MODULE
 * Handles visual mapping, grouping by block, and room management.
 */

// --- GLOBAL STATE ---
let _cachedGroupedRooms = {};
let _currentActiveBlock = null;
let _allBlocks = [];

async function loadRoomMapping() {
    const container = document.getElementById('roomGridContainer');
    if (!container) return;

    // 1. Show Loading State
    container.innerHTML = '<p style="color:#64748b; text-align:center; width:100%; padding:20px;">Loading room map...</p>';

    try {
        const [roomsRes, blocksRes] = await Promise.all([
            fetch('/api/Room/all'),
            fetch('/api/Room/blocks')
        ]);

        if (!roomsRes.ok) throw new Error("Failed to fetch rooms");
        if (!blocksRes.ok) throw new Error("Failed to fetch blocks");

        const rawRooms = await roomsRes.json();
        const rawBlocks = await blocksRes.json();

        const rawBlockArray = Array.isArray(rawBlocks) ? rawBlocks : (rawBlocks.value || []);

        _allBlocks = rawBlockArray.map(b => ({
            BlockID: b.blockID || b.BlockID || b.id || b.Id,
            BlockName: b.blockName || b.BlockName || b.name || b.Name || "Unnamed Block"
        }));

        const roomsData = Array.isArray(rawRooms) ? rawRooms : (rawRooms.value || []);

        if (roomsData.length === 0 && _allBlocks.length === 0) {
            renderEmptyState(container);
            return;
        }

        const rooms = roomsData.map(r => ({
            id: r.id || r.Id || r.roomID || 0,
            number: String(r.number || r.Number || r.roomNumber || r.RoomNumber || 'Unknown'),
            block: String(r.blockName || r.BlockName || r.block || r.Block || 'Unassigned'),
            blockId: r.blockID || r.BlockID || 0,
            capacity: parseInt(r.capacity || r.Capacity || 0),
            occupied: parseInt(r.occupied || r.Occupied || r.currentOccupancy || 0)
        }));

        _cachedGroupedRooms = {};
        _allBlocks.forEach(b => { if (b.BlockName) _cachedGroupedRooms[b.BlockName] = []; });
        _cachedGroupedRooms["Unassigned"] = [];

        rooms.forEach(room => {
            const bName = room.block || "Unassigned";
            if (!_cachedGroupedRooms[bName]) _cachedGroupedRooms[bName] = [];
            _cachedGroupedRooms[bName].push(room);
        });

        if (_cachedGroupedRooms["Unassigned"].length === 0) delete _cachedGroupedRooms["Unassigned"];

        const blockNames = Object.keys(_cachedGroupedRooms).sort();
        if (blockNames.length > 0) {
            if (!_currentActiveBlock || !_cachedGroupedRooms[_currentActiveBlock]) {
                _currentActiveBlock = blockNames[0];
            }
        }

        renderRoomInterface(container);

    } catch (err) {
        console.error("Room Load Critical Error:", err);
        container.innerHTML = `
            <div style="color:#ef4444; text-align:center; padding:20px; background:#fef2f2; border-radius:8px;">
                <strong>Failed to load room map.</strong><br>
                <small>${err.message}</small>
            </div>`;
    }
}

/**
 * Renders the Tabs and the Grid (SECURED)
 */
function renderRoomInterface(container) {
    if (!container) container = document.getElementById('roomGridContainer');

    const blockNames = Object.keys(_cachedGroupedRooms).sort();

    // --- RBAC CHECK ---
    const isAdmin = sessionStorage.getItem('userRole') === 'Administrator';

    // 1. Generate Tabs HTML (Conditionally render "New Block")
    let newBlockBtn = '';
    if (isAdmin) {
        newBlockBtn = `
            <button onclick="openAddBlockModal()" 
                    style="margin-left:auto; background:#65a30d; color:white; border:none; padding:10px 20px; border-radius:6px; font-size:14px; font-weight:600; cursor:pointer; display:flex; align-items:center; gap:5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                <i class="fas fa-plus"></i> New Block
            </button>`;
    }

    const tabsHtml = `
        <div style="display:flex; align-items:center; border-bottom: 2px solid #e2e8f0; margin-bottom: 25px; overflow-x:auto;">
            ${blockNames.map(block => {
        const isActive = block === _currentActiveBlock;
        const activeStyle = "border-bottom: 3px solid #65a30d; color: #65a30d; font-weight: 700;";
        const inactiveStyle = "color: #64748b; font-weight: 500; border-bottom: 3px solid transparent;";

        return `
                    <button onclick="switchRoomBlock('${block}')" 
                            style="padding: 10px 20px; background:none; border:none; cursor:pointer; font-size:14px; transition:all 0.2s; white-space:nowrap; ${isActive ? activeStyle : inactiveStyle}">
                        ${block}
                    </button>
                `;
    }).join('')}
            ${newBlockBtn}
        </div>
    `;

    // 2. Generate Content HTML
    const activeRooms = _cachedGroupedRooms[_currentActiveBlock] || [];
    let contentHtml = '';

    if (activeRooms.length === 0) {
        // Only show "Add Room" to this empty block if Admin
        const addBtn = isAdmin ? `<button class="btn-submit" onclick="openAddRoomModal()" style="font-size:12px; padding: 8px 16px;">+ Add Room to ${_currentActiveBlock}</button>` : '';

        contentHtml = `
            <div style="grid-column: 1/-1; text-align:center; padding: 40px; background:#f8fafc; border-radius:12px; border: 2px dashed #e2e8f0;">
                <div style="font-size: 30px; color: #cbd5e1; margin-bottom: 10px;"><i class="fas fa-cubes"></i></div>
                <h4 style="color: #64748b; margin:0;">No rooms in ${_currentActiveBlock}</h4>
                <p style="color: #94a3b8; font-size:13px; margin-top:5px; margin-bottom:20px;">This block is currently empty.</p>
                ${addBtn}
            </div>
        `;
    } else {
        contentHtml = activeRooms.map(room => {
            const isFull = room.occupied >= room.capacity;
            const percentage = room.capacity > 0 ? Math.min((room.occupied / room.capacity) * 100, 100) : 0;
            const borderColor = isFull ? '#ef4444' : '#10b981';
            const statusText = isFull ? 'Full' : `${room.capacity - room.occupied} Available`;
            const badgeBg = isFull ? '#fee2e2' : '#dcfce7';
            const badgeColor = isFull ? '#ef4444' : '#166534';

            return `
                <div class="room-card" style="border-left: 4px solid ${borderColor}; background:white; padding:15px; border-radius:8px; box-shadow:0 2px 4px rgba(0,0,0,0.05);">
                    <div class="flex-between" style="margin-bottom:10px;">
                        <h4 style="margin:0; font-size:16px; font-weight:700;">${room.number}</h4>
                        <span class="badge" style="background:${badgeBg}; color:${badgeColor}; font-size:11px; padding:2px 8px; border-radius:4px;">
                            ${room.occupied} / ${room.capacity}
                        </span>
                    </div>
                    <div class="progress-bar" style="height:6px; background:#f1f5f9; border-radius:3px; overflow:hidden; margin-bottom:8px;">
                        <div style="width:${percentage}%; background:${borderColor}; height:100%;"></div>
                    </div>
                    <div class="flex-between" style="font-size:12px;">
                        <span style="color:#64748b;">${statusText}</span>
                        <button class="btn-icon" onclick="viewRoomDetails(${room.id})" style="width:24px; height:24px; font-size:10px; border:1px solid #e2e8f0; cursor:pointer;">
                            <i class="fas fa-eye"></i>
                        </button>
                    </div>
                </div>
            `;
        }).join('');
    }

    container.innerHTML = `
        ${tabsHtml}
        <div style="display:grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 15px; animation: fadeIn 0.3s ease-in-out;">
            ${contentHtml}
        </div>
    `;
}

function switchRoomBlock(blockName) {
    _currentActiveBlock = blockName;
    renderRoomInterface();
}

function renderEmptyState(container) {
    const isAdmin = sessionStorage.getItem('userRole') === 'Administrator';
    const btn = isAdmin ? `<button class="btn-submit" onclick="openAddRoomModal()">+ Add Room</button>` : '';

    container.innerHTML = `
        <div style="grid-column: 1/-1; text-align:center; padding: 40px; background:#f8fafc; border-radius:12px;">
            <div style="font-size: 40px; color: #cbd5e1; margin-bottom: 15px;"><i class="fas fa-door-open"></i></div>
            <h3 style="color: #64748b">No Rooms Yet</h3>
            <p style="color: #94a3b8; margin-bottom: 20px;">Start by adding your first hostel block.</p>
            ${btn}
        </div>`;
}

// ... (Rest of the file: openAddRoomModal, saveNewRoom, etc., remains same) ...
// --- MODAL & SAVE LOGIC ---

function openAddRoomModal() {
    document.getElementById('addRoomModal').style.display = 'flex';

    // --- SURGICAL REPLACEMENT OF DROPDOWN ---
    let optionsHtml = `<option value="" selected>-- Select Block --</option>`;
    if (_allBlocks.length > 0) {
        optionsHtml += _allBlocks.map(b => `<option value="${b.BlockID}">${b.BlockName}</option>`).join('');
    } else {
        optionsHtml += `<option disabled>No blocks found. Create one first.</option>`;
    }

    const existingSelect = document.getElementById('addBlockSelect');
    const oldInput = document.getElementById('addBlockName');

    if (existingSelect) {
        // Option A: Update existing
        existingSelect.innerHTML = optionsHtml;
        existingSelect.value = "";
    }
    else if (oldInput) {
        // Option B: Replace Input
        const newSelect = document.createElement('select');
        newSelect.id = 'addBlockSelect';
        newSelect.style.cssText = "width:100%; padding:10px; border:1px solid #cbd5e1; border-radius:6px; background:white;";
        newSelect.innerHTML = optionsHtml;

        oldInput.parentNode.replaceChild(newSelect, oldInput);
    }
}

function closeAddRoomModal() {
    document.getElementById('addRoomModal').style.display = 'none';
    const form = document.getElementById('addRoomForm');
    if (form) form.reset();
}

async function saveNewRoom() {
    const roomNum = document.getElementById('addRoomNumber').value;
    const capacity = document.getElementById('addCapacity').value;
    const blockSelect = document.getElementById('addBlockSelect');

    if (!roomNum || !capacity || !blockSelect || !blockSelect.value) {
        alert("Please fill in Room Number, select a Block, and set Capacity.");
        return;
    }

    const payload = {
        RoomNumber: roomNum,
        BlockID: parseInt(blockSelect.value),
        Capacity: parseInt(capacity)
    };

    const btn = document.querySelector('#addRoomModal .btn-submit');
    const originalText = btn.innerText;
    btn.innerText = "Saving...";
    btn.disabled = true;

    try {
        const res = await fetch('/api/Room/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert("Room added successfully!");
            closeAddRoomModal();
            loadRoomMapping();
            if (typeof updateStats === 'function') updateStats();
        } else {
            const err = await res.json();
            alert("Error: " + (err.Message || "Failed to save"));
        }
    } catch (err) {
        alert("Network Error: Could not reach server.");
    } finally {
        btn.innerText = originalText;
        btn.disabled = false;
    }
}

// Helper for Registration Dropdown
async function loadRoomsForSelect() {
    const select = document.getElementById('regRoomSelect');
    if (!select) return;

    try {
        const res = await fetch('/api/Room/available');
        const data = await res.json();
        const rooms = Array.isArray(data) ? data : (data.value || []);

        select.innerHTML = '<option value="">-- Select Room --</option>' +
            rooms.map(r => {
                const num = r.number || r.RoomNumber || r.Number;
                return `<option value="${r.id || r.Id}">Room ${num} (${r.available || r.Available} beds left)</option>`;
            }).join('');
    } catch (err) {
        console.error("Dropdown Error:", err);
    }
}

// --- NEW BLOCK LOGIC ---

function openAddBlockModal() {
    let modal = document.getElementById('addBlockModal');
    if (!modal) {
        document.body.insertAdjacentHTML('beforeend', `
            <div id="addBlockModal" style="display:none; position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.5); z-index:1000; justify-content:center; align-items:center;">
                <div style="background:white; padding:25px; border-radius:8px; width:300px; box-shadow:0 10px 25px rgba(0,0,0,0.1);">
                    <h3 style="margin-top:0; color:#334155;">Add New Block</h3>
                    <div style="margin: 15px 0;">
                        <label style="display:block; margin-bottom:5px; font-size:12px; color:#64748b;">Block Name</label>
                        <input type="text" id="newBlockNameInput" placeholder="e.g. Khomas" style="width:100%; padding:8px; border:1px solid #cbd5e1; border-radius:4px;">
                    </div>
                    <div style="display:flex; justify-content:flex-end; gap:10px;">
                        <button onclick="document.getElementById('addBlockModal').style.display='none'" style="padding:8px 16px; background:none; border:1px solid #cbd5e1; border-radius:4px; cursor:pointer;">Cancel</button>
                        <button onclick="saveNewBlock()" style="padding:8px 16px; background:#65a30d; color:white; border:none; border-radius:4px; cursor:pointer;">Save</button>
                    </div>
                </div>
            </div>
        `);
        modal = document.getElementById('addBlockModal');
    }

    document.getElementById('newBlockNameInput').value = '';
    modal.style.display = 'flex';
}

async function saveNewBlock() {
    const name = document.getElementById('newBlockNameInput').value;
    if (!name) return alert("Please enter a block name");

    try {
        const res = await fetch('/api/Room/addBlock', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ BlockName: name })
        });

        if (res.ok) {
            alert("Block added!");
            document.getElementById('addBlockModal').style.display = 'none';
            loadRoomMapping();
        } else {
            const err = await res.json();
            alert("Error: " + err.Message);
        }
    } catch (e) {
        console.error(e);
        alert("Failed to connect to server.");
    }
}

// --- VIEW DETAILS LOGIC (Fixed for Case Sensitivity & Data Structure) ---

async function viewRoomDetails(roomId) {
    // 1. Find Room Number
    let roomNum = "Unknown";
    for (const block in _cachedGroupedRooms) {
        const found = _cachedGroupedRooms[block].find(r => r.id === roomId);
        if (found) {
            roomNum = found.number;
            break;
        }
    }

    // 2. Open Modal
    openDetailsModal(roomNum);
    const contentDiv = document.getElementById('roomDetailsContent');
    contentDiv.innerHTML = '<p style="color:#64748b; text-align:center;">Loading occupants...</p>';

    // 3. Fetch Data
    try {
        const res = await fetch(`/api/Room/details/${roomId}`);

        // Error Check
        if (!res.ok) {
            const errData = await res.json();
            throw new Error(errData.Message || errData.message || "Server Error");
        }

        const learners = await res.json();

        // 4. Render Table
        if (learners.length === 0) {
            contentDiv.innerHTML = `
                <div style="text-align:center; padding:20px; color:#94a3b8;">
                    <i class="fas fa-bed" style="font-size:24px; margin-bottom:10px;"></i>
                    <p>This room is currently empty.</p>
                </div>`;
        } else {
            const rows = learners.map(l => {
                const firstName = l.name || l.Name || "";
                const lastName = l.surname || l.Surname || "";

                let displayName = "Unknown Student";

                if (firstName || lastName) {
                    displayName = `${firstName} ${lastName}`;
                } else if (l.fullName || l.FullName) {
                    displayName = l.fullName || l.FullName;
                }

                const grade = l.grade || l.Grade || "-";

                return `
                    <tr style="border-bottom:1px solid #f1f5f9;">
                        <td style="padding:10px;">${displayName}</td>
                        <td style="padding:10px; color:#64748b;">${grade}</td>
                    </tr>
                `;
            }).join('');

            contentDiv.innerHTML = `
                <table style="width:100%; border-collapse:collapse; font-size:14px;">
                    <thead>
                        <tr style="text-align:left; background:#f8fafc; color:#475569;">
                            <th style="padding:10px; font-weight:600;">Student Name</th>
                            <th style="padding:10px; font-weight:600;">Grade</th>
                        </tr>
                    </thead>
                    <tbody>${rows}</tbody>
                </table>
            `;
        }
    } catch (err) {
        console.error(err);
        contentDiv.innerHTML = `
            <div style="text-align:center; padding:10px;">
                <p style="color:#ef4444; font-weight:bold; margin-bottom:5px;">System Error</p>
                <code style="display:block; background:#f1f5f9; padding:10px; border-radius:4px; color:#334155; font-size:11px; text-align:left;">
                    ${err.message || err}
                </code>
            </div>`;
    }
}

// Helper: Creates the modal HTML if it doesn't exist yet
function openDetailsModal(roomNumber) {
    let modal = document.getElementById('roomDetailsModal');

    if (!modal) {
        document.body.insertAdjacentHTML('beforeend', `
            <div id="roomDetailsModal" style="display:none; position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.5); z-index:1100; justify-content:center; align-items:center;">
                <div style="background:white; width:400px; max-width:90%; border-radius:10px; box-shadow:0 10px 25px rgba(0,0,0,0.15); overflow:hidden; animation: fadeIn 0.2s;">
                    
                    <div style="background:#f8fafc; padding:15px 20px; border-bottom:1px solid #e2e8f0; display:flex; justify-content:space-between; align-items:center;">
                        <h3 id="roomDetailsTitle" style="margin:0; color:#334155; font-size:16px;">Room Details</h3>
                        <button onclick="document.getElementById('roomDetailsModal').style.display='none'" style="background:none; border:none; font-size:18px; color:#94a3b8; cursor:pointer;">&times;</button>
                    </div>
                    
                    <div id="roomDetailsContent" style="padding:20px; max-height:300px; overflow-y:auto;">
                        </div>

                    <div style="padding:15px 20px; border-top:1px solid #e2e8f0; text-align:right;">
                        <button onclick="document.getElementById('roomDetailsModal').style.display='none'" style="padding:8px 16px; background:#e2e8f0; color:#475569; border:none; border-radius:6px; cursor:pointer; font-weight:600;">Close</button>
                    </div>
                </div>
            </div>
        `);
        modal = document.getElementById('roomDetailsModal');
    }

    // Update Title & Show
    document.getElementById('roomDetailsTitle').innerText = `Occupants - Room ${roomNumber}`;
    modal.style.display = 'flex';
}