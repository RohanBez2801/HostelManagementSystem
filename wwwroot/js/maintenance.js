/**
 * MAINTENANCE & REPAIRS MODULE
 */

async function loadMaintenance() {
    const tbody = document.getElementById('maintenanceTableBody');
    try {
        const res = await fetch('/api/maintenance/all');
        const data = await res.json();

        document.getElementById('openIssuesCount').innerText = data.filter(i => i.Status === 'Pending').length;

        tbody.innerHTML = data.map(item => `
            <tr>
                <td><strong>Room ${item.RoomNumber}</strong></td>
                <td>${item.IssueDescription}</td>
                <td><span class="priority-${item.Priority.toLowerCase()}">${item.Priority}</span></td>
                <td>
                    <select onchange="updateMaintenanceStatus(${item.Id}, this.value)" style="padding:4px; border-radius:4px;">
                        <option value="Pending" ${item.Status === 'Pending' ? 'selected' : ''}>Pending</option>
                        <option value="Fixed" ${item.Status === 'Fixed' ? 'selected' : ''}>Fixed</option>
                    </select>
                </td>
                <td>${new Date(item.ReportedDate).toLocaleDateString()}</td>
                <td>
                    <button class="btn-icon" onclick="deleteIssue(${item.Id})"><i class="fas fa-trash"></i></button>
                </td>
            </tr>
        `).join('');
    } catch (err) { console.error("Maintenance load error", err); }
}

async function updateMaintenanceStatus(id, newStatus) {
    await fetch(`/api/maintenance/update-status/${id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(newStatus)
    });
    loadMaintenance(); // Refresh UI
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
            rooms.map(r => `<option value="${r.id}">Room ${r.roomNumber}</option>`).join('');
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