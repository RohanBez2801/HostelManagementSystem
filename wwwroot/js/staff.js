/**
 * STAFF MANAGEMENT MODULE
 */

async function loadStaff() {
    const tbody = document.getElementById('staffTableBody');
    if (!tbody) return;

    try {
        const res = await fetch('/api/staff/list');
        const data = await res.json();
        
        if (!res.ok) throw new Error(data.Message || data.message || "Failed to load staff");
        const list = Array.isArray(data) ? data : (data.value || []);

        tbody.innerHTML = list.map(s => `
            <tr>
                <td><strong>${s.name}</strong></td>
                <td>${s.title}</td>
                <td><span class="badge" style="background:#e2e8f0; color:#475569">${s.shift}</span></td>
                <td>${s.phone}</td>
                <td>
                    <button class="btn-icon" onclick="deleteStaff(${s.id})"><i class="fas fa-trash" style="color:#ef4444"></i></button>
                </td>
            </tr>
        `).join('');
    } catch (err) {
        console.error("Failed to load staff:", err);
    }
}

function openStaffModal() {
    document.getElementById('staffModal').style.display = 'flex';
}

function closeStaffModal() {
    document.getElementById('staffModal').style.display = 'none';
    document.getElementById('staffForm').reset();
}

async function saveStaff() {
    const payload = {
        FullName: document.getElementById('staffName').value,
        IDNumber: document.getElementById('staffIDNumber').value,
        JobTitle: document.getElementById('staffTitle').value,
        Shift: document.getElementById('staffShift').value,
        ContactNo: document.getElementById('staffPhone').value
    };

    if (!payload.FullName || !payload.IDNumber) {
        alert("Please fill in required fields.");
        return;
    }

    try {
        const res = await fetch('/api/staff/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            alert("Staff member added successfully!");
            closeStaffModal();
            loadStaff();
        }
    } catch (err) {
        alert("Failed to save staff.");
    }
}

async function deleteStaff(id) {
    if (!confirm("Remove this staff member?")) return;
    try {
        const res = await fetch(`/api/staff/delete/${id}`, { method: 'DELETE' });
        if (res.ok) loadStaff();
    } catch (err) {
        console.error("Delete staff failed:", err);
    }
}
