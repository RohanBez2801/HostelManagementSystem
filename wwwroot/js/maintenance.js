/**
 * MAINTENANCE & REPAIRS MODULE
 */

async function loadMaintenance() {
    const tbody = document.getElementById('maintenanceTableBody');
    if (!tbody) return;

    try {
        const res = await fetch('/api/maintenance/all');
        const data = await res.json();

        // RBAC CHECK
        const role = sessionStorage.getItem('userRole');
        const canFix = (role === 'Maintenance' || role === 'Administrator');

        tbody.innerHTML = data.map(m => {
            let actionBtn = '';
            if (m.status === 'Pending' && canFix) {
                actionBtn = `<button onclick="markFixed(${m.id})" class="btn-sm" style="background:#22c55e; color:white; border:none; border-radius:4px; padding:4px 8px;">Mark Done</button>`;
            } else if (m.status === 'Fixed') {
                actionBtn = `<span style="color:#22c55e;"><i class="fas fa-check"></i> Fixed</span>`;
            } else {
                actionBtn = `<span style="color:#94a3b8;">Pending</span>`; // Other roles just see text
            }

            return `<tr>
                <td>${m.room}</td>
                <td>${m.description}</td>
                <td>${m.priority}</td>
                <td>${m.status}</td>
                <td>${m.date}</td>
                <td>${actionBtn}</td>
            </tr>`;
        }).join('');
    } catch (err) { console.error(err); }
}

async function markFixed(id) {
    if (!confirm("Mark this issue as fixed?")) return;
    await fetch(`/api/maintenance/resolve/${id}`, { method: 'POST' });
    loadMaintenance();
}

async function updateMaintenanceStatus(id, newStatus) {
    try {
        const res = await fetch(`/api/maintenance/update-status/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newStatus)
        });
        if (res.ok) {
            loadMaintenance(); // Refresh UI
        } else {
            alert("Failed to update status.");
        }
    } catch (err) {
        console.error("Update status error:", err);
    }
}

// Open Modal and Populate Rooms
async function openMaintenanceModal() {
    const modal = document.getElementById('maintenanceModal');
    const select = document.getElementById('maintRoomSelect');
    document.getElementById('maintDate').valueAsDate = new Date();

    modal.style.display = 'flex';

    try {
        const res = await fetch('/api/Room/all');
        const rooms = await res.json();
        select.innerHTML = '<option value="">-- Choose Room --</option>' +
            rooms.map(r => `<option value="${r.id}">Room ${r.number}</option>`).join('');
    } catch (err) { console.error("Could not load rooms", err); }
}

// Save to Database
async function submitMaintenanceIssue() {
    const payload = {
        RoomID: parseInt(document.getElementById('maintRoomSelect').value),
        IssueDescription: document.getElementById('maintDesc').value,
        Priority: document.getElementById('maintPriority').value,
        ReportedDate: document.getElementById('maintDate').value,
        Status: "Pending" // Default status
    };

    if (!payload.RoomID || !payload.IssueDescription) {
        alert("Please select a room and describe the issue.");
        return;
    }

    try {
        const res = await fetch('/api/maintenance/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert("Issue reported successfully.");
            closeMaintenanceModal();
            loadMaintenance(); // Refresh the list
        }
    } catch (err) { alert("Failed to save. Check server connection."); }
}

function closeMaintenanceModal() {
    document.getElementById('maintenanceModal').style.display = 'none';
    document.getElementById('maintenanceForm').reset();
}
async function deleteIssue(id) {
    if (!confirm("Remove this issue from the log?")) return;
    try {
        const res = await fetch(`/api/maintenance/delete/${id}`, { method: 'DELETE' });
        if (res.ok) loadMaintenance();
    } catch (err) {
        console.error("Delete maintenance failed:", err);
    }
}